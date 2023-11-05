using System;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace BuildTools
{
    class NegatedEnumValueTransformationAttribute : ArgumentTransformationAttribute
    {
        private MethodInfo enumTryParse;

        public NegatedEnumValueTransformationAttribute(Type type)
        {
            enumTryParse = typeof(Enum).GetMethods().Single(m => m.Name == "TryParse" && m.IsGenericMethod && m.GetParameters().Length == 3 && m.GetParameters()[0].ParameterType == typeof(string)).MakeGenericMethod(type);
        }

        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            if (inputData == null)
                return null;

            object transformEnum(object o)
            {
                if (o is string s && s.StartsWith("~") && s.Length > 1)
                {
                    var args = new object[] { s.Substring(1), true, null };

                    if ((bool)enumTryParse.Invoke(null, args))
                        return $"~{args[2]}";
                }

                return o;
            }

            if (inputData.GetType().IsArray)
            {
                var arr = (Array)inputData;

                for (var i = 0; i < arr.Length; i++)
                    arr.SetValue(transformEnum(arr.GetValue(i)), i);

                return arr;
            }
            else
                return transformEnum(inputData);
        }
    }
}
