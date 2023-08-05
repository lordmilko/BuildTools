using System;
using BuildTools.PowerShell;

namespace BuildTools
{
    class PSPackageProviderDependencyInstaller : IDependencyInstaller
    {
        private readonly IPowerShellService powerShell;
        private readonly Logger logger;

        public PSPackageProviderDependencyInstaller(
            IPowerShellService powerShell,
            Logger logger)
        {
            this.powerShell = powerShell;
            this.logger = logger;
        }

        public DependencyResult Install(Dependency dependency, bool log, bool logSkipped = false)
        {
            var name = dependency.Name;
            Version minimumVersion = null;

            var suffix = "Provider";

            if (name.EndsWith(suffix))
                name = name.Substring(0, name.Length - suffix.Length);

            if (dependency.MinimumVersion != null)
                minimumVersion = dependency.MinimumVersion;

            var provider = powerShell.GetPackageProvider(name);

            if (provider == null || (minimumVersion != null && minimumVersion > provider.Version))
            {
                if (log)
                    logger.LogInformation($"\tInstalling '{name}' package provider");

                var result = powerShell.InstallPackageProvider(name, minimumVersion);

                return new DependencyResult(dependency, null, result.Version, DependencyAction.Success);
            }
            else
            {
                if (log && logSkipped)
                    logger.LogInformation($"\tSkipping installing '{name}' package provider as it is already installed");

                return new DependencyResult(dependency, null, provider.Version, DependencyAction.Skipped);
            }
        }
    }
}
