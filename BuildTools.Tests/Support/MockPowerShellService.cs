using System;
using System.Collections.Generic;
using System.Management.Automation;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    class MockPowerShellService : IPowerShellService, IMock<IPowerShellService>
    {
        public IPowerShellModule[] InstalledModules { get; set; }
        public IPackageProvider InstalledPackageProvider { get; set; }

        public Dictionary<string, IPowerShellCommand> KnownCommands { get; } = new Dictionary<string, IPowerShellCommand>();

        public List<string> InvokedCommands { get; } = new List<string>();

        public bool IsISE { get; }
        public PSEdition Edition { get; }
        public bool IsProgressEnabled { get; }
        public bool IsWindows { get; set; }

        public IPowerShellCommand GetCommand(string name)
        {
            if (KnownCommands.TryGetValue(name, out var command))
                return command;

            throw new InvalidOperationException($"Existence of command '{name}' has not been specified.");
        }

        public void WriteVerbose(string message)
        {
            throw new NotImplementedException();
        }

        public void WriteProgress(string message)
        {
            throw new NotImplementedException();
        }

        public void WriteWarning(string message)
        {
            throw new NotImplementedException();
        }

        public IPowerShellModule[] GetInstalledModules(string name)
        {
            return InstalledModules ?? new IPowerShellModule[0];
        }

        public IPowerShellModule RegisterModule(string name, IList<Type> cmdletTypes)
        {
            throw new NotImplementedException();
        }

        public IPowerShellPackage InstallPackage(string name, Version requiredVersion = null, Version minimumVersion = null,
            bool skipPublisherCheck = false)
        {
            return new MockPowerShellPackage(
                name,
                requiredVersion ?? minimumVersion ?? new Version("1.0")
            );
        }

        public IPackageProvider GetPackageProvider(string name)
        {
            return InstalledPackageProvider;
        }

        public IPackageProvider InstallPackageProvider(string name, Version minimumVersion = null)
        {
            return new MockPackageProvider(
                name,
                minimumVersion ?? new Version("1.0")
            );
        }

        public object Invoke(string script, params object[] input)
        {
            InvokedCommands.Add(script);
            return null;
        }

        public void AssertInvoked(string script)
        {
            Assert.IsTrue(InvokedCommands.Contains(script), $"Command '{script}' was not invoked");
        }
    }
}
