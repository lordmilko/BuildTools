using System;

namespace BuildTools
{
    [AttributeUsage(AttributeTargets.Class)]
    class BuildCommandAttribute : Attribute
    {
        public CommandKind Kind { get; }

        public CommandCategory Category { get; }

        public BuildCommandAttribute(CommandKind kind, CommandCategory category)
        {
            Kind = kind;
            Category = category;
        }
    }
}