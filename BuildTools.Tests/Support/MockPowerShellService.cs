using System;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools.Tests
{
    class MockPowerShellService : IPowerShellService
    {
        public IPowerShellModule[] InstalledModules { get; set; }

        public bool IsISE { get; }
        public PSEdition Edition { get; }
        public bool IsProgressEnabled { get; }
        public bool IsWindows { get; }

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

        public PowerShellPackage InstallPackage(string name, Version requiredVersion = null, Version minimumVersion = null,
            bool skipPublisherCheck = false)
        {
            return new PowerShellPackage(new PSObject
            {
                Properties =
                {
                    new PSNoteProperty("Name", name),
                    new PSNoteProperty("Version", (requiredVersion ?? minimumVersion ?? new Version("1.0")).ToString())
                }
            });
        }

        public PackageProvider GetPackageProvider(string name)
        {
            throw new NotImplementedException();
        }

        public PackageProvider InstallPackageProvider(string name, Version minimumVersion = null)
        {
            throw new NotImplementedException();
        }
    }
}
