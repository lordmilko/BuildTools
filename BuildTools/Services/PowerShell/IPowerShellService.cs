using System;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    interface IPowerShellService
    {
        bool IsISE { get; }

        PSEdition Edition { get; }

        bool IsProgressEnabled { get; }

        bool IsWindows { get; }

        CommandInfo GetCommand(string name);

        void WriteVerbose(string message);

        void WriteProgress(string message);

        IPowerShellModule[] GetInstalledModules(string name);

        IPowerShellPackage InstallPackage(string name, Version requiredVersion = null, Version minimumVersion = null, bool skipPublisherCheck = false);

        IPackageProvider GetPackageProvider(string name);

        IPackageProvider InstallPackageProvider(string name, Version minimumVersion = null);
    }
}