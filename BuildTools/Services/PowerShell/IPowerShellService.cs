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

        IPowerShellModule GetModule(string name);

        IPowerShellModule ImportModule(string name, bool global);

        IPowerShellModule RegisterModule(string name, IList<Type> cmdletTypes);

        void PublishModule(string path);

        void UpdateModuleManifest(string path, string rootModule = null);

        IPowerShellPackage GetPackage(string name, string destination = null);

        IPowerShellPackage InstallPackage(
            string name,
            Version requiredVersion = null,
            Version minimumVersion = null,
            bool force = false,
            bool forceBootstrap = false,
            bool allowClobber = false,
            string providerName = "PowerShellGet",
            string source = null,
            string destination = null,
            bool skipDependencies = false,
            bool skipPublisherCheck = false);

        void UninstallPackage(string name);

        void UninstallPackage(IPowerShellPackage package);

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

        PesterResult[] InvokePester(string path, string[] additionalArgs);

        object InvokeAndUnwrap(string script, params object[] input);

        object[] InvokeWithArgs(string cmdlet, params string[] args);

        void InitializePrompt(ProjectConfig config);

        void SetWindowTitle(string value);

        void Clear();
    }
}
