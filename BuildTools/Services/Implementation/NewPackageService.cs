using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class NewPackageService
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly CSharpPackageProvider csharpPackageProvider;
        private readonly PowerShellPackageProvider powerShellPackageProvider;
        private readonly PowerShellPackageSourceService powerShellPackageSourceService;
        private readonly IPowerShellService powerShell;
        private readonly IFileSystemProvider fileSystem;
        private readonly GetVersionService getVersionService;
        private readonly ICommandService commandService;
        private readonly Logger logger;

        public NewPackageService(
            IProjectConfigProvider configProvider,
            CSharpPackageProvider csharpPackageProvider,
            PowerShellPackageProvider powerShellPackageProvider,
            PowerShellPackageSourceService powerShellPackageSourceService,
            IPowerShellService powerShell,
            IFileSystemProvider fileSystem,
            GetVersionService getVersionService,
            ICommandService commandService,
            Logger logger)
        {
            this.configProvider = configProvider;
            this.csharpPackageProvider = csharpPackageProvider;
            this.powerShellPackageProvider = powerShellPackageProvider;
            this.powerShellPackageSourceService = powerShellPackageSourceService;
            this.powerShell = powerShell;
            this.fileSystem = fileSystem;
            this.getVersionService = getVersionService;
            this.commandService = commandService;
            this.logger = logger;
        }

        public FileInfo[] Execute(PackageType[] packageTypes, BuildConfiguration buildConfiguration, bool isLegacy)
        {
            if (!powerShell.IsWindows && buildConfiguration == BuildConfiguration.Release)
                throw new InvalidOperationException("Release packages can only be created on Windows.");

            var results = new List<FileInfo>();

            var config = new PackageConfig(
                buildConfiguration,
                isLegacy,
                configProvider.Config.PowerShellMultiTargeted,
                packageTypes
            );

            var cmdletName = commandService.GetCommand(CommandKind.NewPackage).Name;

            var numLogs = 0;

            if (config.Target.CSharp)
                numLogs++;

            if (config.Target.PowerShell || config.Target.Redist)
                numLogs++;

            var count = 0;

            try
            {
                if (config.Target.CSharp)
                {
                    count++;

                    if (fileSystem.DirectoryExists(PackageSourceService.RepoLocation))
                        fileSystem.DeleteDirectory(PackageSourceService.RepoLocation);

                    fileSystem.CreateDirectory(PackageSourceService.RepoLocation);

                    powerShell.WriteProgress(cmdletName, "Creating C# Package", percentComplete: (int)((double) count / numLogs * 100));

                    //When publishing C# packages, we can simply use nuget/dotnet pack. When you publish a PowerShell module however you do need to have a repository to point to.
                    //We only use CSharpPackageSourceService during CI where we test actually trying to install the package

                    var version = getVersionService.GetVersion(isLegacy).Package;

                    csharpPackageProvider.Execute(config, version);
                    results.AddRange(MovePackages(string.Empty, configProvider.SolutionRoot));

                    fileSystem.DeleteDirectory(PackageSourceService.RepoLocation);
                }

                if (config.Target.PowerShell || config.Target.Redist)
                {
                    count++;

                    var text = PackageType.PowerShell;

                    if (packageTypes.Length != 0 && !packageTypes.Contains(PackageType.PowerShell))
                        text = PackageType.Redistributable;

                    powerShell.WriteProgress(cmdletName, $"Creating {text.GetDescription(false)} Package", percentComplete: (int)((double)count / numLogs * 100));

                    powerShellPackageSourceService.Install();

                    powerShellPackageProvider.Execute(config);
                    results.AddRange(MovePackages("_PowerShell", configProvider.SolutionRoot));

                    // Don't uninstall the repository unless we succeeded, so we can troubleshoot any issues
                    // inside the repository incase the pack fails
                    powerShellPackageSourceService.Uninstall();
                }
            }
            finally
            {
                powerShell.CompleteProgress();
            }

            return results.ToArray();
        }

        internal FileInfo[] MovePackages(
            string suffix,
            string destinationFolder)
        {
            var output = new List<FileInfo>();

            var pkgs = fileSystem.EnumerateFiles(PackageSourceService.RepoLocation, "*.*nupkg");

            foreach (var pkg in pkgs)
            {
                var baseName = Path.GetFileNameWithoutExtension(pkg);
                var newName = $"{baseName}{suffix}{Path.GetExtension(pkg)}";
                var newPath = Path.Combine(destinationFolder, newName);

                logger.LogInformation($"\t\t\t\tMoving package '{Path.GetFileName(pkg)}' to '{newPath}'");
                fileSystem.MoveFile(pkg, newPath);

                output.Add(new FileInfo(newPath));
            }

            var zips = fileSystem.EnumerateFiles(PackageSourceService.RepoLocation, "*.zip");

            foreach (var zip in zips)
            {
                var fileName = Path.GetFileName(zip);
                var newPath = Path.Combine(destinationFolder, fileName);

                logger.LogInformation($"\t\t\t\tMoving package '{fileName}' to '{newPath}'");
                fileSystem.MoveFile(zip, newPath);

                output.Add(new FileInfo(newPath));
            }

            return output.ToArray();
        }
    }
}
