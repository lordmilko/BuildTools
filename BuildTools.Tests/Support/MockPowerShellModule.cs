using System;
using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPowerShellModule : IPowerShellModule
    {
        public string Name { get; }
        public Version Version { get; }

        public MockPowerShellModule(string name, string version)
        {
            Name = name;
            Version = new Version(version);
        }
    }
}