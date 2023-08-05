using System;
using System.Linq;
using System.Management.Automation;
using System.Reflection;

namespace BuildTools
{
    public class BuildCommand : IBuildCommand
    {
        public string Name { get; }

        public Type Type { get; }

        public CommandKind Kind { get; }

        public CommandCategory Category { get; }

        public string Description { get; internal set; }

        public BuildCommand(Type type)
        {
            Type = type;

            var cmdletAttrib = type.GetCustomAttribute<CmdletAttribute>();
            var aliasAttrib = type.GetCustomAttribute<AliasAttribute>();

            if (cmdletAttrib == null)
                throw new InvalidOperationException($"Cmdlet '{type.Name}' is missing a '{nameof(CmdletAttribute)}'.");

            var buildAttrib = type.GetCustomAttribute<BuildCommandAttribute>();

            if (buildAttrib == null)
                throw new InvalidOperationException($"Cmdlet '{type.Name}' is missing a '{nameof(BuildCommandAttribute)}'.");

            Name = $"{cmdletAttrib.VerbName}-{cmdletAttrib.NounName}";

            if (aliasAttrib != null)
            {
                var alternateName = aliasAttrib.AliasNames.FirstOrDefault(v => v.Contains('-'));

                if (alternateName != null)
                    Name = alternateName;
            }

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
