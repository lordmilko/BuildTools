using System;

namespace BuildTools
{
    [AttributeUsage(AttributeTargets.Class)]
    class CommandCategoryAttribute : Attribute
    {
        public CommandCategory Category { get; }

        public CommandCategoryAttribute(CommandCategory category)
        {
            Category = category;
        }
    }
}