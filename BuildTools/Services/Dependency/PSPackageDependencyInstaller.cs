using System;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class PSPackageDependencyInstaller : IDependencyInstaller
    {
        private readonly IPowerShellService powerShell;
        private readonly Logger logger;

        public PSPackageDependencyInstaller(
            IPowerShellService powerShell,
            Logger logger)
        {
            this.powerShell = powerShell;
            this.logger = logger;
        }

        public DependencyResult Install(Dependency dependency, bool log, bool logSkipped = false)
        {
            var powerShellDependency = (PSPackageDependency) dependency;

            Version requiredVersion = null;
            Version minimumVersion = null;
            bool skipPublisherCheck = false;

            if (dependency.Version != null)
                requiredVersion = dependency.Version;
            else
            {
                if (dependency.MinimumVersion != null)
                    minimumVersion = dependency.MinimumVersion;
            }

            if (powerShellDependency.SkipPublisherCheck)
                skipPublisherCheck = true;

            var installedModules = powerShell.GetInstalledModules(dependency.Name);

            if (dependency.MinimumVersion != null)
                installedModules = installedModules.Where(m => m.Version >= dependency.MinimumVersion).ToArray();
            else
            {
                if (dependency.Version != null)
                    installedModules = installedModules.Where(m => m.Version == dependency.Version).ToArray();
            }

            if (installedModules.Length == 0)
            {
                if (log)
                    logger.LogInformation($"\tInstalling '{dependency.Name}' PowerShell Module");

                var result = powerShell.InstallPackage(dependency.Name, requiredVersion, minimumVersion, skipPublisherCheck);

                return new DependencyResult(dependency, result.Version, DependencyAction.Success);
            }
            else
            {
                if (log && logSkipped)
                    logger.LogInformation($"\tSkipping installing package '{dependency.Name}' as it is already installed");

                return new DependencyResult(dependency, installedModules.First().Version, DependencyAction.Skipped);
            }
        }
    }
}