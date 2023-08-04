using System;

namespace BuildTools
{
    /// <summary>
    /// Specifies that a given property is optional.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class OptionalAttribute : Attribute
    {
    }
}
