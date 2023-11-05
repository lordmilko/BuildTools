using System;
using System.Collections;
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
