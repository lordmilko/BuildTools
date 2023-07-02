namespace BuildTools
{
    public interface IBuildCommand
    {
        string Name { get; }

        CommandKind Kind { get; }
    }
}