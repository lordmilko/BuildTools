using System;
using System.Management.Automation;
using System.Reflection;

namespace BuildTools
{
    public class BuildCommand
    {
        public string Name { get; }

        public Type Type { get; }

        public CommandKind Kind { get; }

        public CommandCategory Category { get; }

        public BuildCommand(Type type)
        {
            Type = type;

            var attrib = type.GetCustomAttribute<CmdletAttribute>();

            if (attrib == null)
                throw new InvalidOperationException($"Cmdlet '{type.Name}' is missing a '{nameof(CmdletAttribute)}'.");

            var buildAttrib = type.GetCustomAttribute<BuildCommandAttribute>();

            if (buildAttrib == null)
                throw new InvalidOperationException($"Cmdlet '{type.Name}' is missing a '{nameof(BuildCommandAttribute)}'.");

            Name = $"{attrib.VerbName}-{attrib.NounName}";
            Type = type;
            Kind = buildAttrib.Kind;
            Category = buildAttrib.Category;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}