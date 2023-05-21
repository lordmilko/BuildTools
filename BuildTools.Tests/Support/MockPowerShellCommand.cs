using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPowerShellCommand : IPowerShellCommand
    {
        public string Name { get; }
        public string Source { get; }

        public MockPowerShellCommand(string name)
        {
            Name = name;
        }
    }
}