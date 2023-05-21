using System;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPowerShellService : IPowerShellService, IMock<IPowerShellService>
    {
        public IPowerShellModule[] InstalledModules { get; set; }
        public IPackageProvider InstalledPackageProvider { get; set; }

        public bool IsISE { get; }
        public PSEdition Edition { get; }
        public bool IsProgressEnabled { get; }
        public bool IsWindows { get; set; }

        public CommandInfo GetCommand(string name)
        {
            throw new NotImplementedException();
        }

        public void WriteVerbose(string message)
        {
            throw new NotImplementedException();
        }

        public void WriteProgress(string message)
        {
            throw new NotImplementedException();
        }

        public IPowerShellModule[] GetInstalledModules(string name)
        {
            return InstalledModules ?? new IPowerShellModule[0];
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
    }
}
