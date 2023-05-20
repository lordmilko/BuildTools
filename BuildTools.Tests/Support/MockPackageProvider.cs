using System;
using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPackageProvider : IPackageProvider
    {
        public string Name { get; }
        public Version Version { get; }

        public MockPackageProvider(string name, Version version)
        {
            Name = name;
            Version = version;
        }
    }
}