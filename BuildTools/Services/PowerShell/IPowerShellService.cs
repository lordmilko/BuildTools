using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    interface IPowerShellService
    {
        bool IsISE { get; }

        PSEdition Edition { get; }

        bool IsProgressEnabled { get; }

        bool IsWindows { get; }

        IPowerShellCommand GetCommand(string name);

        void WriteColor(string message, ConsoleColor? color = null, bool newLine = true);

        void WriteError(ErrorRecord errorRecord);

        void WriteVerbose(string message);

        void WriteProgress(
            string activity = null,
            string status = null,
            string currentOperation = null,
            int? percentComplete = null);

        void WriteWarning(string message);

        void CompleteProgress();

        IPowerShellModule[] GetInstalledModules(string name);

        IPowerShellModule RegisterModule(string name, IList<Type> cmdletTypes);

        void PublishModule(string path);

        IPowerShellPackage InstallPackage(string name, Version requiredVersion = null, Version minimumVersion = null, bool skipPublisherCheck = false);

        #region PackageProvider

        IPackageProvider GetPackageProvider(string name);

        IPackageProvider InstallPackageProvider(string name, Version minimumVersion = null);

        #endregion
        #region PackageSource

        IPackageSource[] GetPackageSource();

        void RegisterPackageSource();

        void UnregisterPackageSource();

        #endregion
        #region PSRepository

        IPSRepository[] GetPSRepository();

        void RegisterPSRepository();

        void UnregisterPSRepository();

        #endregion

        object Invoke(string script, params object[] input);

        void InitializePrompt(ProjectConfig config);
    }
}