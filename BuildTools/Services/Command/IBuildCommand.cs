using System;

namespace BuildTools
{
    public interface IBuildCommand
    {
        string Name { get; }

        Type Type { get; }

        CommandKind Kind { get; }

        CommandCategory Category { get; }

        string Description { get; }
    }
}