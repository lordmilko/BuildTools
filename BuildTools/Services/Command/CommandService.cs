using System;
using System.Linq;

namespace BuildTools
{
    public class CommandService
    {
        private readonly BuildCommand[] commands;

        public CommandService(Type[] cmdletTypes)
        {
            commands = cmdletTypes.Select(t => new BuildCommand(t)).ToArray();
        }

        public BuildCommand GetCommand(CommandKind kind)
        {
            var matches = commands.Where(c => c.Kind == kind).ToArray();

            if (matches.Length == 0)
                throw new InvalidOperationException($"No commands of type '{kind}' were found.");

            if (matches.Length > 1)
                throw new InvalidOperationException($"More than one command of type '{kind}' was found.");

            return matches[0];
        }
    }
}