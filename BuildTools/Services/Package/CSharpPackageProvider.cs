using System;
using System.Collections.Generic;

namespace BuildTools
{
    class CSharpPackageProvider
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly DependencyProvider dependencyProvider;
        private readonly Logger logger;
        private readonly IProcessService processService;

        public CSharpPackageProvider(
            IProjectConfigProvider configProvider,
            DependencyProvider dependencyProvider,
            Logger logger,
            IProcessService processService)
        {
            this.configProvider = configProvider;
            this.dependencyProvider = dependencyProvider;
            this.logger = logger;
            this.processService = processService;
        }

        public void Execute(bool isLegacy, BuildConfiguration configuration, Version version)
        {
            logger.LogInformation("\t\tBuilding package");

            string processName;
            ArgList args;

            if (isLegacy)
            {
                processName = dependencyProvider.Install(WellKnownDependency.NuGet).Path;

                var exclusions = new List<string>
                {
                    "**/*.tt"
                };

                if (configProvider.Config.CSharpLegacyPackageExcludes != null)
                    exclusions.AddRange(configProvider.Config.CSharpLegacyPackageExcludes);

                args = new ArgList
                {
                    "pack",
                    configProvider.GetPrimaryProject(true).FilePath,
                    "-Exclude",
                    string.Join(";", exclusions),
                    "-outputdirectory",
                    $"\"{PackageSourceService.RepoLocation}\"",
                    "-NoPackageAnalysis",
                    "-symbols",
                    "-SymbolPackageFormat",
                    "snupkg",
                    "-version",
                    version,
                    "-properties",
                    "Configuration=$Configuration"
                };
            }
            else
            {
                args = new ArgList
                {
                    "pack",
                    configProvider.GetPrimaryProject(false).FilePath,
                    "--include-symbols",
                    "--no-restore",
                    "--no-build",
                    "-c",
                    configuration,
                    "--output",
                    $"\"{PackageSourceService.RepoLocation}\"",
                    "/nologo",
                    "-p:EnableSourceLink=true;SymbolPackageFormat=snupkg"
                };

                processName = dependencyProvider.Install(WellKnownDependency.Dotnet).Path;
            }

            logger.LogVerbose($"Executing command '{processName} {args}'");
            processService.Execute(processName, args);
        }
    }
}
