using System;

namespace BuildTools
{
    [AttributeUsage(AttributeTargets.Class)]
    class BuildCommandAttribute : Attribute
    {
        public CommandKind Kind { get; }

        public CommandCategory Category { get; }

        public Feature Feature { get; }

        public BuildCommandAttribute(CommandKind kind, CommandCategory category, Feature feature)
        {
            Kind = kind;
            Category = category;
            Feature = feature;
        }
    }
}
