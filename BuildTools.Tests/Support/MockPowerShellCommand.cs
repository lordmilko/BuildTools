using System;
using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPowerShellCommand : IPowerShellCommand
    {
        public string Name { get; }
        public string Source { get; }

        public Version Version { get; }

        public MockPowerShellCommand(string name)
        {
            Name = name;
            Source = $"C:\\{name}.exe";
        }
    }
}