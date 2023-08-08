using System;

namespace BuildTools
{
    public interface ICommandService
    {
        IBuildCommand GetOptionalCommand(CommandKind kind);

        IBuildCommand GetCommand(CommandKind kind);

        IBuildCommand GetCommand(Type type);

        IBuildCommand[] GetCommands();

        void SetDescription(IBuildCommand command, string description);
    }
}
