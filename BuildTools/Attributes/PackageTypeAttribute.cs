using System;

namespace BuildTools
{
    [AttributeUsage(AttributeTargets.Property)]
    class PackageTypeAttribute : Attribute
    {
        public PackageType Type { get; }

        public PackageTypeAttribute(PackageType type)
        {
            Type = type;
        }
    }
}
