using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using BuildTools.Cmdlets;

namespace BuildTools
{
    class DependencyProvider
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly DotnetDependencyInstaller dotnetInstaller;
        private readonly Lazy<ChocolateyDependencyInstaller> chocolateyInstaller; //Depends on DependencyProvider
        private readonly PSPackageDependencyInstaller powerShellInstaller;
        private readonly PSPackageProviderDependencyInstaller powerShellProviderInstaller;
        private readonly TargetingPackDependencyInstaller targetingPackInstaller;

        private Dependency[] dependencies;

        private Dependency[] Dependencies
        {
            get
            {
                if (dependencies == null)
                    EnsureDependencies();

                return dependencies;
            }
        }

        public DependencyProvider(
            IProjectConfigProvider configProvider,
            DotnetDependencyInstaller dotnetInstaller,
            Lazy<ChocolateyDependencyInstaller> chocolateyInstaller,
            PSPackageDependencyInstaller powerShellInstaller,
            PSPackageProviderDependencyInstaller powerShellProviderInstaller,
            TargetingPackDependencyInstaller targetingPackInstaller)
        {
            this.configProvider = configProvider;
            this.dotnetInstaller = dotnetInstaller;
            this.chocolateyInstaller = chocolateyInstaller;
            this.powerShellInstaller = powerShellInstaller;
            this.powerShellProviderInstaller = powerShellProviderInstaller;
            this.targetingPackInstaller = targetingPackInstaller;
        }

        private void EnsureDependencies()
        {
            var hasCoverage = configProvider.HasFeature(Feature.Coverage);
            var hasLegacyCSharpPackage = configProvider.HasFeature(Feature.Package) && configProvider.Config.PackageTypes.Contains(PackageType.CSharp) && configProvider.HasLegacyProjects;
            var hasPowerShellTests = configProvider.HasFeature(Feature.Test) && configProvider.Config.TestTypes.Contains(TestType.PowerShell);
            var hasScriptAnalyzer = configProvider.Config.HasCommand(typeof(InvokeAnalyzer<>));

            dependencies = new Dependency[]
            {
                new ChocolateyDependency(                                             minimumVersion: "0.10.5.0"),
                new DotnetDependency(),
                new ChocolateyPackageDependency("codecov",                                                                                                                                             condition: hasCoverage),
                new ChocolateyPackageDependency("opencover.portable",                 minimumVersion: "4.7.922.0", commandName: "opencover.console", displayName: WellKnownDependency.OpenCover,       condition: hasCoverage),
                new ChocolateyPackageDependency("reportgenerator.portable",           minimumVersion: "3.0.0.0",   commandName: "reportgenerator",   displayName: WellKnownDependency.ReportGenerator, condition: hasCoverage),
                new ChocolateyPackageDependency(WellKnownDependency.vswhere,          minimumVersion: "2.6.7"),
                new ChocolateyPackageDependency("NuGet.CommandLine",                  minimumVersion: "5.2.0",     commandName: "nuget",             displayName: WellKnownDependency.NuGet,           condition: hasLegacyCSharpPackage),
                new PSPackageProviderDependency("NuGetProvider",                      minimumVersion: "2.8.5.201"),
                new PSPackageDependency        ("PowerShellGet",                      minimumVersion: "2.0.0"),
                new PSPackageDependency(WellKnownDependency.Pester, version: "3.4.6", minimumVersion: "3.4.5", skipPublisherCheck: true,                                                               condition: hasPowerShellTests),
                new PSPackageDependency        (WellKnownDependency.PSScriptAnalyzer,                                                                                                                  condition: hasScriptAnalyzer),
                new TargetingPackDependency    (WellKnownDependency.TargetingPack452, version: "4.5.2"),
                new TargetingPackDependency    (WellKnownDependency.TargetingPack461, version: "4.6.1"),
            }.Where(d => d.Condition).ToArray();
        }

        public Dependency GetDependency(string name) => GetDependencies(name).Single();

        public Dependency[] GetDependencies(params string[] name)
        {
            if (name == null || name.Length == 0)
                return Dependencies;

            HashSet<string> matched = new HashSet<string>();

            var matches = Dependencies.Where(d =>
            {
                foreach (var value in name)
                {
                    var wildcard = new WildcardPattern(value, WildcardOptions.IgnoreCase);

                    if (d.DisplayName != null)
                    {
                        if (wildcard.IsMatch(d.DisplayName))
                        {
                            matched.Add(value);
                            return true;
                        }
                    }

                    if (wildcard.IsMatch(d.Name))
                    {
                        matched.Add(value);
                        return true;
                    }
                }

                return false;
            }).ToArray();

            var unmatched = name.Except(matched).ToArray();

            if (unmatched.Length > 0)
            {
                if (unmatched.Length == 1)
                    throw new ArgumentException($"Could not find dependency '{unmatched[0]}'");
                else
                {
                    var str = unmatched.Select(u => $"'{u}'");

                    throw new ArgumentException($"Could not find dependencies {string.Join(", ", str)}");
                }
            }

            return matches;
        }

        public DependencyResult Install(string name, bool log = true, bool logSkipped = false)
        {
            var dependency = GetDependency(name);

            return Install(dependency, log, logSkipped);
        }

        public DependencyResult Install(Dependency dependency, bool log = true, bool logSkipped = false)
        {
            if (dependency == null)
                throw new ArgumentNullException(nameof(dependency));

            IDependencyInstaller installer;

            switch (dependency)
            {
                case DotnetDependency _:
                    installer = dotnetInstaller;
                    break;

                case ChocolateyPackageDependency _:
                    installer = chocolateyInstaller.Value;
                    break;

                case PSPackageDependency _:
                    installer = powerShellInstaller;
                    break;

                case PSPackageProviderDependency _:
                    installer = powerShellProviderInstaller;
                    break;

                case TargetingPackDependency _:
                    installer = targetingPackInstaller;
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle dependency of type '{dependency.GetType().Name}'.");
            }

            var result = installer.Install(dependency, log, logSkipped);

            return result;
        }
    }
}
