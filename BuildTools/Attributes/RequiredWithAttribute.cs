using System;

namespace BuildTools
{
    /// <summary>
    /// Specifies that a given property is required when a given feature is available.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    class RequiredWithAttribute : RequiredAttribute
    {
        public CommandKind CommandKind { get; }

        public RequiredWithAttribute(CommandKind commandKind)
        {
            CommandKind = commandKind;
        }
    }
}
