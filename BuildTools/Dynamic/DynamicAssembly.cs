﻿using System;
using System.Diagnostics;
using System.Management.Automation;
using System.Reflection;
using System.Reflection.Emit;

namespace BuildTools.Dynamic
{
    public class DynamicAssembly
    {
        private static object lockObj = new object();

        public static readonly DynamicAssembly Instance = new DynamicAssembly("BuildTools.GeneratedCode");

        public string Name { get; }

        public DynamicAssembly(string name)
        {
            Name = name;
        }

        #region AssemblyBuilder

        AssemblyBuilder assemblyBuilder;
        internal AssemblyBuilder AssemblyBuilder
        {
            get
            {
                lock (lockObj)
                {
                    if (assemblyBuilder == null)
                    {
                        InitAssembly();
                        InitModule();
                    }
                }

                return assemblyBuilder;
            }
        }

        void InitAssembly()
        {
            var assemblyName = new AssemblyName(Name);

            //RunAndSave must be specified to allow debugging in memory (even if we don't actually save it)
#if NETFRAMEWORK
            assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndSave);
#else
            assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
#endif

#if DEBUG
            //Specify a DebuggableAttribute to enable debugging
            var attribute = typeof(DebuggableAttribute);
            var ctor = attribute.GetConstructor(new Type[] { typeof(DebuggableAttribute.DebuggingModes) });

            var attributeBuilder = new CustomAttributeBuilder(ctor, new object[]
            {
                DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.Default
            });

            assemblyBuilder.SetCustomAttribute(attributeBuilder);
#endif
        }

        #endregion
        #region ModuleBuilder

        ModuleBuilder moduleBuilder;
        ModuleBuilder ModuleBuilder
        {
            get
            {
                lock (lockObj)
                {
                    if (moduleBuilder == null)
                    {
                        InitAssembly();
                        InitModule();
                    }
                }

                return moduleBuilder;
            }
        }

        void InitModule()
        {
            //All assemblies contain at least one module. This implementation detail is typically invisible
            moduleBuilder = assemblyBuilder.DefineDynamicModule(
                Name
#if NETFRAMEWORK
                , Name + ".dll"
                , true
#endif
            );
        }

        #endregion

        public Type DefineEnvironment(string name)
        {
            var typeBuilder = ModuleBuilder.DefineType($"{AssemblyBuilder.GetName().Name}.{name}Environment", TypeAttributes.Public);

            typeBuilder.AddInterfaceImplementation(typeof(IEnvironmentIdentifier));

            var type = typeBuilder.CreateType();

            return type;
        }

        public TypeBuilder DefineCmdlet(string cmdletPrefix, Type baseType)
        {
            //assert the basetype is a buildcmdlet and that its got a cmdlet descriptor attribute
            if (!typeof(PSCmdlet).IsAssignableFrom(baseType))
                throw new ArgumentException($"Cannot define cmdlet proxy for type '{baseType.Name}': type is not a {nameof(PSCmdlet)}");

            var cmdletAttrib = baseType.GetCustomAttribute<CmdletAttribute>();

            if (cmdletAttrib == null)
                throw new InvalidOperationException($"Cannot define cmdlet proxy for type '{baseType.Name}': type is missing a '{nameof(CmdletAttribute)}'.");

            string typeName = baseType.Name;

            var index = typeName.IndexOf('`');

            if (index != -1)
                typeName = typeName.Substring(0, index);

            string verb = cmdletAttrib.VerbName;

            if (typeName.EndsWith(cmdletAttrib.NounName))
            {
                var prefix = typeName.Substring(0, typeName.Length - cmdletAttrib.NounName.Length);

                if (cmdletAttrib.VerbName == "Test" && prefix == "Simulate")
                    verb = prefix;
            }

            var typeBuilder = ModuleBuilder.DefineType($"{AssemblyBuilder.GetName().Name}.{verb}{cmdletPrefix}{cmdletAttrib.NounName}", TypeAttributes.Public, baseType);

            return typeBuilder;
        }

#if NETFRAMEWORK
        public void Save()
        {
            AssemblyBuilder.Save(Name + ".dll");
        }
#endif
    }
}
