namespace BuildTools
{
    public interface ICommandService
    {
        IBuildCommand GetCommand(CommandKind kind);
    }
}