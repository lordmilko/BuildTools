using System;
using System.IO;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class ClearBuildService
    {
        private readonly IProcessService processService;
        private readonly IPowerShellService powerShell;
        private readonly Logger logger;
        private readonly IProjectConfigProvider configProvider;
        private readonly DependencyProvider dependencyProvider;
        private readonly IFileSystemProvider fileSystem;
        private readonly IVsProductLocator vsProductLocator;

        public ClearBuildService(
            IProcessService processService,
            IPowerShellService powerShell,
            Logger logger,
            IProjectConfigProvider configProvider,
            DependencyProvider dependencyProvider,
            IFileSystemProvider fileSystem,
            IVsProductLocator vsProductLocator)
        {
            this.processService = processService;
            this.powerShell = powerShell;
            this.logger = logger;
            this.configProvider = configProvider;
            this.dependencyProvider = dependencyProvider;
            this.fileSystem = fileSystem;
            this.vsProductLocator = vsProductLocator;
        }

        public void ClearFull()
        {
            ClearCommon();

            var root = configProvider.SolutionRoot;

            var projectFiles = fileSystem.EnumerateFiles(root, "*.csproj", SearchOption.AllDirectories);

            foreach (var projectFile in projectFiles)
            {
                var fileName = Path.GetFileName(projectFile);

                logger.LogInformation($"Processing {fileName}");

                var projectFolder = Path.GetDirectoryName(projectFile);

                var bin = Path.Combine(projectFolder, "bin");
                var obj = Path.Combine(projectFolder, "obj");

                if (fileSystem.DirectoryExists(bin))
                {
                    logger.LogAttention($"\tRemoving {bin}");
                    RemoveItems(bin);
                }

                if (fileSystem.DirectoryExists(obj))
                {
                    //obj will be automatically recreated and removed each time Clear-Build is run,
                    //due to dotnet/msbuild clean recreating it
                    logger.LogAttention($"\tRemoving {obj}");
                    RemoveItems(obj);
                }
            }

            logger.LogInformation("Processing Redistributable Packages");

            ClearRedist();
        }

        public void ClearMSBuild(BuildConfiguration configuration, bool isLegacy)
        {
            ClearCommon();

            if (isLegacy)
                ClearNetFramework(configuration);
            else
                ClearNetCore(configuration);

            ClearRedist();
        }

        private void ClearNetFramework(BuildConfiguration configuration)
        {
            var solutionPath = configProvider.GetSolutionPath(true);

            var msbuild = vsProductLocator.GetMSBuild();

            var msbuildArgs = new ArgList
            {
                "/t:clean",
                $"\"{solutionPath}\"",
                $"/p:Configuration={configuration}"
            };

            logger.LogVerbose($"Executing command 'msbuild {msbuildArgs}'");

            processService.Execute(msbuild, msbuildArgs);
        }

        private void ClearNetCore(BuildConfiguration configuration)
        {
            var dotnet = dependencyProvider.Install(WellKnownDependency.Dotnet);

            var solutionPath = configProvider.GetSolutionPath(false);

            if (!powerShell.IsWindows)
            {
                //Running dotnet clean on Linux can result in ResolvePackageAsserts
                //unexpectedly failing due to something to do with the NuGet fallback cache.
                //Executing a package restore resolves this problem.
                processService.Execute(dotnet.Path, $"restore {solutionPath}");
            }

            var cleanArgs = new ArgList
            {
                "clean",
                $"\"{solutionPath}\"",
                "-c",
                configuration
            };

            logger.LogVerbose($"Executing command 'dotnet {cleanArgs}'");

            processService.Execute(dotnet.Path, cleanArgs);
        }

        private void ClearCommon()
        {
            if (processService.IsRunning("devenv"))
                logger.LogAttention("Warning: Visual Studio is currently running. Some items may not be able to be removed");

            var root = configProvider.SolutionRoot;

            var binLog = Path.Combine(root, "msbuild.binlog");

            if (fileSystem.FileExists(binLog))
                fileSystem.DeleteFile(binLog);
        }

        private void RemoveItems(string path)
        {
            var files = fileSystem.EnumerateFiles(path, searchOption: SearchOption.AllDirectories);

            foreach (var file in files)
            {
                logger.LogAttention($"\t\tRemoving '{file}'");

                fileSystem.DeleteFile(file);
            }

            var folders = fileSystem.EnumerateDirectories(path, searchOption: SearchOption.AllDirectories);

            foreach (var folder in folders)
            {
                if (fileSystem.DirectoryExists(folder))
                {
                    logger.LogAttention($"\t\tRemoving '{folder}'");

                    fileSystem.DeleteDirectory(folder);
                }
            }

            fileSystem.DeleteDirectory(path);
        }

        private void ClearRedist()
        {
            var root = configProvider.SolutionRoot;

            var packagesDir = Path.Combine(root, "packages");

            var files = fileSystem.EnumerateFiles(root, "*.*nupkg", SearchOption.AllDirectories)
                .Where(f => !f.StartsWith(packagesDir, StringComparison.OrdinalIgnoreCase))
                .Union(fileSystem.EnumerateFiles(root, "*.zip"));

            foreach (var file in files)
            {
                logger.LogAttention($"\tRemoving {file}");
                fileSystem.DeleteFile(file);
            }
        }
    }
}
