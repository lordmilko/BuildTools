using System;
using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPowerShellPackage : IPowerShellPackage
    {
        public string Name { get; }
        public Version Version { get; }

        public MockPowerShellPackage(string name, Version version)
        {
            Name = name;
            Version = version;
        }
    }
}