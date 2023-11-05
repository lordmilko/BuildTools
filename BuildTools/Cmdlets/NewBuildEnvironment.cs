using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace BuildTools.Cmdlets
{
    [Alias("New-BuildManifest")]
    [Cmdlet(VerbsCommon.New, "BuildEnvironment")]
    public class NewBuildEnvironment : GlobalBuildCmdlet, IDynamicParameters
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Path { get; set; } = ".";

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Simple { get; set; }

        protected override void ProcessRecordEx()
        {
            var service = GetService<NewBuildEnvironmentService>();

            foreach (var item in service.Execute(Path, Force, new HashTableConfigSettingValueProvider(GetBoundParameters())))
                WriteObject(new FileInfo(item));
        }

        private Hashtable GetBoundParameters()
        {
            var realParameters = GetType().GetProperties().Where(p => p.GetCustomAttribute<ParameterAttribute>() != null).Select(p => p.Name).ToArray();

            var boundParameters = MyInvocation.BoundParameters.ToDictionary(kv => kv.Key, kv => kv.Value);

            foreach (var item in realParameters)
                boundParameters.Remove(item);

            if (Simple)
            {
                if (!boundParameters.ContainsKey(nameof(ProjectConfig.Features)))
                {
                    var properties = typeof(ProjectConfig).GetProperties();

                    var bad = new List<Feature>();

                    foreach (var property in properties)
                    {
                        var requiredWithAttrib = property.GetCustomAttribute<RequiredWithAttribute>();

                        if (requiredWithAttrib != null)
                        {
                            //If we didn't specify this required value, disable the feature that requires it

                            if (!boundParameters.ContainsKey(property.Name))
                                bad.Add(requiredWithAttrib.Feature);
                        }
                    }

                    if (bad.Count > 0)
                        boundParameters[nameof(ProjectConfig.Features)] = bad.Distinct().Select(v => $"~{v}").OrderBy(v => v.Length).ToArray();
                }
            }

            var hashtable = new Hashtable();

            foreach (var item in boundParameters)
                hashtable[item.Key] = item.Value;

            return hashtable;
        }

        public override object GetDynamicParameters()
        {
            var dict = new RuntimeDefinedParameterDictionary();

            var properties = typeof(ProjectConfig).GetProperties();

            foreach (var property in properties)
            {
                var attributes = new Collection<Attribute>
                {
                    new ParameterAttribute
                    {
                        Mandatory = false
                    }
                };

                var type = GetParameterType(property.PropertyType);

                var valueConverterAttrib = property.GetCustomAttribute<ValueConverterAttribute>();

                if (valueConverterAttrib != null)
                {
                    if (valueConverterAttrib.Type.IsGenericType && valueConverterAttrib.Type.GetGenericTypeDefinition() == typeof(NegatableEnumValueConverter<>))
                    {
                        var elmType = valueConverterAttrib.Type.GetGenericArguments()[0];

                        attributes.Add(new ArgumentCompleterAttribute(typeof(NegatedEnumValueCompleter<>).MakeGenericType(elmType)));
                        attributes.Add(new ValidateSetExAttribute(typeof(NegatedEnumValueValidator<>).MakeGenericType(elmType)));
                        attributes.Add(new NegatedEnumValueTransformationAttribute(elmType));

                        type = type.IsArray ? typeof(string[]) : typeof(string);
                    }
                }                

                dict.Add(property.Name, new RuntimeDefinedParameter(property.Name, type, attributes));
            }

            return dict;
        }

        private Type GetParameterType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;

            var safeTypes = new[]
            {
                typeof(string),
                typeof(bool),
                typeof(double),
                typeof(string[]),
                typeof(CommandKind[]),
                typeof(Feature[]),
                typeof(PackageType[]),
                typeof(TestType[])
            };

            if (safeTypes.Contains(underlying))
                return underlying;

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(Func<,>))
                    return typeof(ScriptBlock);
            }

            if (type == typeof(PackageTests) || type == typeof(PackageFiles))
                return typeof(Hashtable);

            throw new NotImplementedException($"Don't know how to handle type '{underlying.Name}'.");
        }
    }
}
