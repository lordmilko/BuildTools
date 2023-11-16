using System;
using System.Collections.Generic;
using System.IO;
using BuildTools.PowerShell;

namespace BuildTools
{
    class ChocolateyDependencyInstaller : IDependencyInstaller
    {
        private readonly IPowerShellService powerShell;
        private readonly EnvironmentService environmentService;
        private readonly IFileSystemProvider fileSystem;
        private readonly IProcessService process;
        private readonly IWebClient webClient;
        private readonly Logger logger;
        private readonly DependencyProvider provider;

        public ChocolateyDependencyInstaller(
            IPowerShellService powerShell,
            EnvironmentService environmentService,
            IFileSystemProvider fileSystem,
            IProcessService process,
            IWebClient webClient,
            Logger logger,
            DependencyProvider provider)
        {
            this.powerShell = powerShell;
            this.environmentService = environmentService;
            this.fileSystem = fileSystem;
            this.process = process;
            this.webClient = webClient;
            this.logger = logger;
            this.provider = provider;
        }

        public DependencyResult Install(Dependency dependency, bool log, bool logSkipped = false)
        {
            var chocolateyDependency = (ChocolateyPackageDependency) dependency;

            if (!powerShell.IsWindows)
                throw new InvalidOperationException($"Cannot install package '{dependency}': package is only supported on Windows");

            var commandName = chocolateyDependency.CommandName;

            var action = "install";

            bool upgrade = false;

            if (upgrade)
                action = "upgrade";

            if (!upgrade)
            {
                var installed = false;
                Version existingVersion = null;

                var existingCommand = GetChocolateyCommand(commandName, false);

                if (existingCommand != null)
                {
                    var fileVersion = fileSystem.GetFileVersion(existingCommand);

                    if (chocolateyDependency.MinimumVersion != null)
                    {
                        if (fileVersion >= chocolateyDependency.MinimumVersion)
                        {
                            existingVersion = fileVersion;
                            installed = true;
                        }
                        else
                            action = "upgrade";
                    }
                    else
                    {
                        existingVersion = fileVersion;
                        installed = true;
                    }
                }

                if (installed)
                {
                    var versionStr = string.Empty;

                    if (chocolateyDependency.Version != null)
                        versionStr = $" version {chocolateyDependency.Version}";

                    if (log && logSkipped)
                        logger.LogInformation($"\tSkipping installing package '{dependency}'{versionStr} as it is already installed");

                    return new DependencyResult(dependency, existingCommand, existingVersion, DependencyAction.Skipped);
                }
            }

            var chocoArgs = new List<string>
            {
                action,
                dependency.Name,
                "--limitoutput",
                "--no-progress",
                "-y"
            };

            if (dependency.Version != null)
            {
                chocoArgs.Add("--version");
                chocoArgs.Add(dependency.Version.ToString());
            }

            if (log)
                logger.LogInformation($"\tExecuting 'choco {string.Join(" ", chocoArgs)}'");

            var isManager = dependency is ChocolateyDependency;

            if (!isManager || action == "upgrade")
            {
                if (!isManager)
                    provider.Install(WellKnownDependency.Chocolatey, log, false);

                process.Execute("choco", chocoArgs);
            }
            else
            {
                var script = webClient.GetString("https://chocolatey.org/install.ps1");

                powerShell.InvokeWithArgs(script);
            }

            var command = powerShell.GetCommand(commandName);

            if (command == null)
                throw new InvalidOperationException($"{dependency} did not install correctly: command '{commandName}' was not found.");

            return new DependencyResult(dependency, command.Source, command.Version, DependencyAction.Success);
        }

        private string GetChocolateyCommand(string commandName, bool allowPath = true)
        {
            if (!commandName.EndsWith(".exe"))
                commandName += ".exe";

            //Just because our command exists on the PATH, doesn't mean it's the latest version.
            //Check for our command under the chocolatey folder; if it exists, we'll prefer to use it over anything else

            var root = environmentService.ChocolateyInstall ?? "C:\\ProgramData\\chocolatey";

            if (fileSystem.DirectoryExists(root))
            {
                var exe = Path.Combine(root, "bin", commandName);

                if (fileSystem.FileExists(exe))
                    return exe;
            }

            if (allowPath)
            {
                var command = powerShell.GetCommand(commandName);

                logger.LogWarning($"Cannot find {commandName} under chocolatey; using '{command.Source}' from PATH");

                return command.Source;
            }

            return null;
        }
    }
}
