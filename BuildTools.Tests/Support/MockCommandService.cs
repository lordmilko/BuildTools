namespace BuildTools.Tests
{
    class MockBuildCommand : IBuildCommand
    {
        public string Name { get; }
        public CommandKind Kind { get; }

        public MockBuildCommand(CommandKind kind)
        {
            Name = kind.ToString();
            Kind = kind;
        }
    }

    class MockCommandService : ICommandService
    {
        public IBuildCommand GetCommand(CommandKind kind) =>
            new MockBuildCommand(kind);
    }
}