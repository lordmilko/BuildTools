using System;

namespace BuildTools
{
    /// <summary>
    /// Specifies that a given property is required when a given feature is available.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class RequiredWithAttribute : RequiredAttribute
    {
        public Feature Feature { get; }

        public RequiredWithAttribute(Feature feature)
        {
            Feature = feature;
        }
    }
}
