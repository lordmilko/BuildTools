using System;
using System.IO;
using System.Linq;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools
{
    class BuildConfig
    {
        public string Name { get; set; }

        public ArgList ArgumentList { get; set; }

        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        public bool DebugBuild { get; set; }

        public bool? SourceLink { get; set; }

        public string Target { get; set; }
    }

    class InvokeBuildService
    {
        private readonly IPowerShellService powerShell;
        private readonly IProjectConfigProvider configProvider;
        private readonly ChocolateyDependencyInstaller chocolateyDependencyInstaller;
        private readonly DependencyProvider dependencyProvider;
        private readonly Logger logger;
        private readonly IProcessService processService;
        private readonly IVsProductLocator vsProductLocator;

        public InvokeBuildService(
            IPowerShellService powerShell,
            IProjectConfigProvider configProvider,
            ChocolateyDependencyInstaller chocolateyDependencyInstaller,
            DependencyProvider dependencyProvider,
            Logger logger,
            IProcessService processService,
            IVsProductLocator vsProductLocator)
        {
            this.powerShell = powerShell;
            this.configProvider = configProvider;
            this.chocolateyDependencyInstaller = chocolateyDependencyInstaller;
            this.dependencyProvider = dependencyProvider;
            this.logger = logger;
            this.processService = processService;
            this.vsProductLocator = vsProductLocator;
        }

        public void Build(BuildConfig buildConfig, bool isLegacy)
        {
            if (powerShell.IsWindows && buildConfig.SourceLink == null)
                buildConfig.SourceLink = true;

            if (buildConfig.Name != null)
            {
                var candidates = configProvider.GetProjects(isLegacy);

                var wildcard = new WildcardPattern(buildConfig.Name, WildcardOptions.IgnoreCase);

                var projects = candidates.Where(v => wildcard.IsMatch(v.Name)).ToArray();

                if (projects.Length == 0)
                    throw new InvalidOperationException($"Cannot find any projects that match the wildcard '{buildConfig.Name}'. Please specify one of {string.Join(", ", candidates.Select(c => c.Name))}");

                if (projects.Length > 1)
                    throw new InvalidOperationException($"Can only specify one project at a time, however wildcard '{buildConfig.Name}' matched multiple projects: {string.Join(", ", candidates.Select(c => c.Name))}");

                buildConfig.Target = projects[0].FilePath;
            }

            var root = configProvider.SolutionRoot;

            if (buildConfig.DebugBuild)
            {
                var binLog = Path.Combine(root, "msbuild.binlog");
                buildConfig.ArgumentList.Add($"/bl:{binLog}");
            }

            if (isLegacy)
                RestoreNuGetPackages();

            BuildInternal(buildConfig, isLegacy);
        }

        private void BuildInternal(BuildConfig buildConfig, bool isLegacy)
        {
            if (buildConfig.Target == null)
                buildConfig.Target = configProvider.GetSolutionPath(isLegacy);

            if (isLegacy)
                BuildFull(buildConfig);
            else
                BuildCore(buildConfig);
        }

        private void BuildFull(BuildConfig buildConfig)
        {
            var msbuild = vsProductLocator.GetMSBuild();

            var argList = new ArgList
            {
                buildConfig.Target,
                "/verbosity:minimal",
                $"/p:Configuration={buildConfig.Configuration}"
            };

            argList.AddRange(buildConfig.ArgumentList);

            logger.LogVerbose($"Executing command '$msbuild {argList}'");

            processService.Execute(msbuild, argList, writeHost: true);
        }

        private void BuildCore(BuildConfig buildConfig)
        {
            var argList = new ArgList
            {
                "build",
                buildConfig.Target,
                "-nologo",
                "-c",
                buildConfig.Configuration
            };

            if (buildConfig.SourceLink == true)
                argList.Add("-p:EnableSourceLink=true");

            argList.AddRange(buildConfig.ArgumentList);

            var dotnet = dependencyProvider.Install(WellKnownDependency.Dotnet);

            if (powerShell.IsWindows)
            {
                dependencyProvider.Install(WellKnownDependency.TargetingPack452);
                dependencyProvider.Install(WellKnownDependency.TargetingPack461);
            }

            logger.LogVerbose($"Executing command 'dotnet {argList}'");

            processService.Execute(dotnet.Path, argList, writeHost: true);
        }

        private void RestoreNuGetPackages()
        {
            var nuget = dependencyProvider.Install(WellKnownDependency.NuGet).Path;

            var sln = configProvider.GetSolutionPath(true);

            var nugetArgs = new ArgList
            {
                "restore",
                sln
            };

            logger.LogVerbose($"Executing command '{nuget} {nugetArgs}'");

            processService.Execute(nuget, nugetArgs, writeHost: true);
        }
    }
}
