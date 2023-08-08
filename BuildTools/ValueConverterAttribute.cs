using System;

namespace BuildTools
{
    internal class ValueConverterAttribute : Attribute
    {
        public Type Type { get; }

        public ValueConverterAttribute(Type type)
        {
            Type = type;
        }
    }
}
