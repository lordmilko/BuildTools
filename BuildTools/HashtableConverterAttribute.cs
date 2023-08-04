using System;

namespace BuildTools
{
    internal class HashtableConverterAttribute : Attribute
    {
        public Type Type { get; }

        public HashtableConverterAttribute(Type type)
        {
            Type = type;
        }
    }
}
