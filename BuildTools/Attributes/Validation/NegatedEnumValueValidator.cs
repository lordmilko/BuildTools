using System;
using System.Linq;

namespace BuildTools
{
    class NegatedEnumValueValidator<T> : IValidateSetValuesGenerator where T : Enum
    {
        public static readonly NegatedEnumValueValidator<T> Instance = new NegatedEnumValueValidator<T>();

        public string[] GetValidValues()
        {
            var values = Enum.GetNames(typeof(T)).ToList();
            values.AddRange(values.Select(v => $"~{v}").ToArray());
            return values.ToArray();
        }
    }
}
