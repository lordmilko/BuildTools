using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using BuildTools.Reflection;

namespace BuildTools
{
    class HelpMetadataHolder : IDisposable
    {
        private static PropertyInfo CmdletInfo_CommandMetadata;

        private static PropertyInfo CommandMetadata_ImplementsDynamicParameters;
        private static PropertyInfo CommandMetadata_StaticCommandParameterMetadata_Property;
        private static FieldInfo CommandMetadata_staticCommandParameterMetadata_Field;

        private static MethodInfo MergedCommandParameterMetadata_MakeReadOnly;
        private static MethodInfo MergedCommandParameterMetadata_ResetReadOnly;
        private static MethodInfo MergedCommandParameterMetadata_AddMetadataForBinder;
        private static MethodInfo MergedCommandParameterMetadata_GenerateParameterSetMappingFromMetadata;
        private static FieldInfo MergedCommandParameterMetadata_parameterSetMap;

        private static object ParameterBinderAssociation_DynamicParameters;

        private static MethodInfo InternalParameterMetadata_Get;

        static HelpMetadataHolder()
        {
            CmdletInfo_CommandMetadata = typeof(CmdletInfo).GetInternalPropertyInfo("CommandMetadata");

            CommandMetadata_ImplementsDynamicParameters = typeof(CommandMetadata).GetInternalPropertyInfo("ImplementsDynamicParameters");
            CommandMetadata_StaticCommandParameterMetadata_Property = typeof(CommandMetadata).GetInternalPropertyInfo("StaticCommandParameterMetadata");
            CommandMetadata_staticCommandParameterMetadata_Field = typeof(CommandMetadata).GetInternalFieldInfo("staticCommandParameterMetadata");

            InitializeMergedCommandParameterMetadata();
            InitializeInternalParameterMetadata();
            InitializeParameterBinderAssociation();
        }

        private static void InitializeMergedCommandParameterMetadata()
        {
            var type = CommandMetadata_StaticCommandParameterMetadata_Property.PropertyType;

            MergedCommandParameterMetadata_MakeReadOnly = type.GetInternalMethod("MakeReadOnly");
            MergedCommandParameterMetadata_ResetReadOnly = type.GetInternalMethod("ResetReadOnly");
            MergedCommandParameterMetadata_AddMetadataForBinder = type.GetInternalMethod("AddMetadataForBinder");
            MergedCommandParameterMetadata_GenerateParameterSetMappingFromMetadata = type.GetInternalMethod("GenerateParameterSetMappingFromMetadata");
            MergedCommandParameterMetadata_parameterSetMap = type.GetInternalFieldInfo("parameterSetMap");
        }

        private static void InitializeInternalParameterMetadata()
        {
            var typeName = "System.Management.Automation.InternalParameterMetadata";

            var internalParameterMetadataType = typeof(PSCmdlet).Assembly.GetType(typeName);

            if (internalParameterMetadataType == null)
                throw new InvalidOperationException($"Failed to find type '{typeName}'.");

            InternalParameterMetadata_Get = internalParameterMetadataType.GetMethods(BindingFlags.Static | BindingFlags.NonPublic).SingleOrDefault(m => m.Name == "Get" && m.GetParameters()[0].ParameterType == typeof(RuntimeDefinedParameterDictionary));

            if (InternalParameterMetadata_Get == null)
                throw new MissingMemberException(internalParameterMetadataType.Name, "Get");
        }

        private static void InitializeParameterBinderAssociation()
        {
            var type = typeof(PSCmdlet).Assembly.GetType("System.Management.Automation.ParameterBinderAssociation");

            if (type == null)
                throw new NotImplementedException();

            ParameterBinderAssociation_DynamicParameters = Enum.GetValues(type).Cast<object>().Single(v => v.ToString() == "DynamicParameters");
        }

        private object originalMetadata;
        private object originalStaticMetadata;
        private bool needRevert;

        public HelpMetadataHolder(CmdletInfo cmdletInfo)
        {
            originalMetadata = (CommandMetadata)CmdletInfo_CommandMetadata.GetValue(cmdletInfo);
            var implementsDynamicParameters = (bool)CommandMetadata_ImplementsDynamicParameters.GetValue(originalMetadata);

            if (!implementsDynamicParameters)
                return;

            //The help system will get upset if you return a different CmdletInfo object. We don't want to risk causing any issues with the normal cmdlet binding system,
            //so we just create a temporary StaticCommandParameterMetadata definition that we add our dynamic parameters to, generate the help using it, and then restore
            //the original StaticCommandParameterMetadata value.

            var newCmdletInfo = new CmdletInfo(cmdletInfo.Name, cmdletInfo.ImplementingType);
            var newMetadata = (CommandMetadata) CmdletInfo_CommandMetadata.GetValue(newCmdletInfo);
            var newStaticMetadata = CommandMetadata_StaticCommandParameterMetadata_Property.GetValue(newMetadata);

            originalStaticMetadata = CommandMetadata_StaticCommandParameterMetadata_Property.GetValue(originalMetadata);
            CommandMetadata_staticCommandParameterMetadata_Field.SetValue(originalMetadata, newStaticMetadata);
            needRevert = true;

            MergedCommandParameterMetadata_ResetReadOnly.Invoke(newStaticMetadata, new object[0]);

            var internalParameterMetadata = GetInternalParameterMetadata(cmdletInfo.ImplementingType);

            MergedCommandParameterMetadata_AddMetadataForBinder.Invoke(newStaticMetadata, new[] { internalParameterMetadata, ParameterBinderAssociation_DynamicParameters });

            MergedCommandParameterMetadata_parameterSetMap.SetValue(newStaticMetadata, new List<string>());
            MergedCommandParameterMetadata_GenerateParameterSetMappingFromMetadata.Invoke(newStaticMetadata, new object[] { newMetadata.DefaultParameterSetName });
        }

        private object GetInternalParameterMetadata(Type cmdletType)
        {
            var cmdletInstance = (IDynamicParameters)Activator.CreateInstance(cmdletType);
            var dynamicParameters = (RuntimeDefinedParameterDictionary)cmdletInstance.GetDynamicParameters();

            var internalParameterMetadata = InternalParameterMetadata_Get.Invoke(null, new object[] { dynamicParameters, null, true });

            return internalParameterMetadata;
        }

        public void Dispose()
        {
            if (needRevert)
                CommandMetadata_staticCommandParameterMetadata_Field.SetValue(originalMetadata, originalStaticMetadata);
        }
    }
}