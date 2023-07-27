using System;

namespace BuildTools.Tests
{
    class MockBuildCommand : IBuildCommand
    {
        public string Name { get; }
        public Type Type { get; }
        public CommandKind Kind { get; }
        public CommandCategory Category { get; }
        public string Description { get; }

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

        public IBuildCommand GetCommand(Type type)
        {
            throw new NotImplementedException();
        }

        public IBuildCommand[] GetCommands()
        {
            throw new NotImplementedException();
        }

        public void SetDescription(IBuildCommand command, string description)
        {
            throw new NotImplementedException();
        }
    }
}