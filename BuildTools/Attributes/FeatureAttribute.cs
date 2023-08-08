using System;

namespace BuildTools
{
    [AttributeUsage(AttributeTargets.Class)]
    class FeatureAttribute : Attribute
    {
        public Feature Feature { get; }

        public FeatureAttribute(Feature feature)
        {
            Feature = feature;
        }
    }
}
