using System;
using System.Linq;

namespace BuildTools
{
    internal class CommandService : ICommandService
    {
        private readonly IBuildCommand[] commands;

        public CommandService(Type[] cmdletTypes)
        {
            commands = cmdletTypes.Select(t => (IBuildCommand) new BuildCommand(t)).ToArray();
        }

        public IBuildCommand GetOptionalCommand(CommandKind kind) => GetCommandInternal(kind, true);

        public IBuildCommand GetCommand(CommandKind kind) => GetCommandInternal(kind, false);

        private IBuildCommand GetCommandInternal(CommandKind kind, bool optional)
        {
            var matches = commands.Where(c => c.Kind == kind).ToArray();

            if (matches.Length == 0)
            {
                if (optional)
                    return null;

                throw new InvalidOperationException($"No commands of type '{kind}' were found.");
            }

            if (matches.Length > 1)
                throw new InvalidOperationException($"More than one command of type '{kind}' was found.");

            return matches[0];
        }

        public IBuildCommand GetCommand(Type type)
        {
            var match = commands.SingleOrDefault(c => c.Type == type);

            if (match == null)
                throw new InvalidOperationException($"A command implementing type '{type.Name}' was not found.");

            return match;
        }

        public IBuildCommand[] GetCommands() => commands;

        public void SetDescription(IBuildCommand command, string description)
        {
            ((BuildCommand)command).Description = description;
        }
    }
}
