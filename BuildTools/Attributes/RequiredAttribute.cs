using System;

namespace BuildTools
{
    /// <summary>
    /// Specifies that a given property is required.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class RequiredAttribute : Attribute
    {
    }
}
