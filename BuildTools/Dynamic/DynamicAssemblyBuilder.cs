using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Reflection.Emit;
using BuildTools.Cmdlets;

namespace BuildTools.Dynamic
{
    class DynamicAssemblyBuilder
    {
        private ProjectConfig config;

        public Type EnvironmentId { get; private set; }

        public List<Type> CmdletTypes { get; } = new List<Type>();

        private static Type[] baseCmdletTypes;

        static DynamicAssemblyBuilder()
        {
            baseCmdletTypes = typeof(BuildCommand).Assembly.GetTypes().Where(t =>
                t.IsGenericTypeDefinition &&
                t.GetCustomAttribute<BuildCommandAttribute>() != null
                && t != typeof(StartModule<>)
            ).ToArray();
        }

        public DynamicAssemblyBuilder(ProjectConfig config)
        {
            this.config = config;
        }

        public void BuildCmdlets(bool singleton)
        {
            if (singleton)
                EnvironmentId = typeof(SingletonEnvironment);
            else
                EnvironmentId = DynamicAssembly.Instance.DefineEnvironment(config.Name);

            foreach (var type in baseCmdletTypes)
                DefineCmdlet(type);

            DefineCmdlet(typeof(StartModule<>), config.Name);
        }

        private void DefineCmdlet(Type baseType, string noun = null)
        {
            if (!baseType.IsGenericTypeDefinition)
                throw new ArgumentException($"Cannot define cmdlet proxy for type '{baseType.Name}': type is not a generic type definition.");

            var genericArgs = baseType.GetGenericArguments();

            if (genericArgs.Length != 1)
                throw new InvalidOperationException($"Cannot define cmdlet proxy for type '{baseType.Name}'. Expected Generic Args: 1. Actual: {genericArgs.Length}.");

            var kind = baseType.GetCustomAttribute<BuildCommandAttribute>().Kind;

            if (config.ExcludedCommands != null && config.ExcludedCommands.Contains(kind))
                return;

            var genericBaseType = baseType.MakeGenericType(EnvironmentId);

            var typeBuilder = DynamicAssembly.Instance.DefineCmdlet(config.CmdletPrefix, genericBaseType);

            SetCmdletAttribute(genericBaseType, typeBuilder, noun);
            SetNameAttribute(genericBaseType, typeBuilder, noun);

            typeBuilder.SetCustomAttribute(CloneAttribute<BuildCommandAttribute>(genericBaseType));

            CloneParameters(typeBuilder, genericBaseType);

            CmdletTypes.Add(typeBuilder.CreateType());
        }

        private void SetCmdletAttribute(Type genericBaseType, TypeBuilder typeBuilder, string noun)
        {
            var cmdletAttrib = genericBaseType.GetCustomAttribute<CmdletAttribute>();

            var attributeBuilder = CloneAttribute<CmdletAttribute>(
                genericBaseType,
                new object[] { cmdletAttrib.VerbName, noun ?? $"{config.CmdletPrefix}{cmdletAttrib.NounName}" }
            );

            typeBuilder.SetCustomAttribute(attributeBuilder);
        }

        private void SetNameAttribute(Type genericBaseType, TypeBuilder typeBuilder, string noun)
        {
            var attrib = genericBaseType.GetCustomAttribute<NameAttribute>();

            if (attrib != null)
            {
                string GetName(string value)
                {
                    var split = value.Split('-');

                    if (split.Length == 2)
                        return $"{split[0]}-{config.CmdletPrefix}{noun ?? split[1]}";

                    return value;
                }

                var attributeBuilder = CloneAttribute<NameAttribute>(
                    genericBaseType,
                    new object[]
                    {
                        GetName(attrib.Name)
                    }
                );

                typeBuilder.SetCustomAttribute(attributeBuilder);
            }
        }

        private void CloneParameters(TypeBuilder typeBuilder, Type baseType)
        {
            var properties = baseType.GetProperties().Where(p => p.GetCustomAttributes<ParameterAttribute>().Any()).ToArray();

            foreach (var propertyInfo in properties)
            {
                var propertyBuilder = typeBuilder.DefineProperty(propertyInfo.Name, PropertyAttributes.None, propertyInfo.PropertyType, null);

                //Getter
                var getter = typeBuilder.DefineMethod(
                    propertyInfo.GetGetMethod().Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                    propertyInfo.PropertyType,
                    null
                );

                var getIL = getter.GetILGenerator();
                getIL.Emit(OpCodes.Ldarg_0);
                getIL.Emit(OpCodes.Call, propertyInfo.GetGetMethod());
                getIL.Emit(OpCodes.Ret);

                //Setter
                var setter = typeBuilder.DefineMethod(
                    propertyInfo.GetSetMethod().Name,
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName,
                    null,
                    new[] {propertyInfo.PropertyType}
                );

                var setIL = setter.GetILGenerator();
                setIL.Emit(OpCodes.Ldarg_0);
                setIL.Emit(OpCodes.Ldarg_1);
                setIL.Emit(OpCodes.Call, propertyInfo.GetSetMethod());
                setIL.Emit(OpCodes.Ret);

                propertyBuilder.SetGetMethod(getter);
                propertyBuilder.SetSetMethod(setter);

                var attribData = propertyInfo.GetCustomAttributesData();

                foreach (var item in attribData)
                {
                    var attribBuilder = CloneAttribute(item);

                    propertyBuilder.SetCustomAttribute(attribBuilder);
                }
            }
        }

        private CustomAttributeBuilder CloneAttribute<TAttribute>(Type originalType, object[] newCtorArgs = null)
        {
            var items = originalType.GetCustomAttributesData();

            var matches = items.Where(i => i.AttributeType == typeof(TAttribute)).ToArray();

            if (matches.Length == 0)
                throw new InvalidOperationException($"Cannot find required attribute '{typeof(TAttribute).Name}' on type '{originalType.Name}'.");

            if (matches.Length > 1)
                throw new InvalidOperationException($"Found more than one '{nameof(TAttribute)}' on type '{originalType.Name}'.");

            return CloneAttribute(matches[0], newCtorArgs);
        }

        private CustomAttributeBuilder CloneAttribute(CustomAttributeData data, object[] newCtorArgs = null)
        {
            var propertyNames = new List<PropertyInfo>();
            var propertyValues = new List<object>();

            if (data.NamedArguments != null)
            {
                foreach (var prop in data.NamedArguments)
                {
                    propertyNames.Add((PropertyInfo)prop.MemberInfo);
                    propertyValues.Add(prop.TypedValue.Value);
                }
            }

            if (newCtorArgs == null)
            {
                newCtorArgs = data.ConstructorArguments.Select(c =>
                {
                    var value = c.Value;

                    if (value is Type t && t.IsGenericTypeDefinition)
                        return t.MakeGenericType(EnvironmentId);

                    if (value is ReadOnlyCollection<CustomAttributeTypedArgument> r)
                        return r.Select(v => v.Value).ToArray();

                    return value;
                }).ToArray();
            }

            var attribBuilder = new CustomAttributeBuilder(
                data.Constructor,
                newCtorArgs,
                propertyNames.ToArray(),
                propertyValues.ToArray()
            );

            return attribBuilder;
        }
    }
}
