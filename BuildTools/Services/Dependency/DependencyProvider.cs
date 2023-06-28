using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools
{
    class DependencyProvider
    {
        private readonly DotnetDependencyInstaller dotnetInstaller;
        private readonly Lazy<ChocolateyDependencyInstaller> chocolateyInstaller; //Depends on DependencyProvider
        private readonly PSPackageDependencyInstaller powerShellInstaller;
        private readonly PSPackageProviderDependencyInstaller powerShellProviderInstaller;
        private readonly TargetingPackDependencyInstaller targetingPackInstaller;

        private Dependency[] dependencies;

        public DependencyProvider(
            IPowerShellService powerShell,
            
            DotnetDependencyInstaller dotnetInstaller,
            Lazy<ChocolateyDependencyInstaller> chocolateyInstaller,
            PSPackageDependencyInstaller powerShellInstaller,
            PSPackageProviderDependencyInstaller powerShellProviderInstaller,
            TargetingPackDependencyInstaller targetingPackInstaller)
        {
            this.dotnetInstaller = dotnetInstaller;
            this.chocolateyInstaller = chocolateyInstaller;
            this.powerShellInstaller = powerShellInstaller;
            this.powerShellProviderInstaller = powerShellProviderInstaller;
            this.targetingPackInstaller = targetingPackInstaller;

            PSPackageDependency Pester()
            {
                var minimumVersion = "3.4.5";
                var version = "3.4.6";

                if (!powerShell.IsWindows)
                {
                    /* We want to be able to test PrtgAPI.Build on Linux, however the advanced mocking
                     * required by these tests won't work in Pester 3, so when we're actually on Linux
                     * use Pester 4 instead. We go with 4.7.2 because 4.7.3 truncates "Should Be" output
                     * to 5 characters which is useless */
                    minimumVersion = "4.7.0";
                    version = "4.7.2";
                }

                return new PSPackageDependency("Pester", version: version, minimumVersion: minimumVersion, true);
            }

            dependencies = new Dependency[]
            {
                new ChocolateyDependency(                                             minimumVersion: "0.10.5.0"),
                new DotnetDependency(),
                new ChocolateyPackageDependency("codecov"),
                new ChocolateyPackageDependency("opencover.portable",                 minimumVersion: "4.7.922.0", commandName: "opencover.console", displayName: "OpenCover"),
                new ChocolateyPackageDependency("reportgenerator.portable",           minimumVersion: "3.0.0.0",   commandName: "reportgenerator",   displayName: "ReportGenerator"),
                new ChocolateyPackageDependency(WellKnownDependency.vswhere,          minimumVersion: "2.6.7"),
                new ChocolateyPackageDependency("NuGet.CommandLine",                  minimumVersion: "5.2.0",     commandName: "nuget",             displayName: WellKnownDependency.NuGet),
                new PSPackageProviderDependency("NuGetProvider",                      minimumVersion: "2.8.5.201"),
                new PSPackageDependency        ("PowerShellGet",                      minimumVersion: "2.0.0"),
                Pester(),
                new PSPackageDependency        (WellKnownDependency.PSScriptAnalyzer),
                new TargetingPackDependency    (WellKnownDependency.TargetingPack452, version: "4.5.2"),
                new TargetingPackDependency    (WellKnownDependency.TargetingPack461, version: "4.6.1"),
            };
        }

        public Dependency GetDependency(string name) => GetDependencies(name).Single();

        public Dependency[] GetDependencies(params string[] name)
        {
            if (name == null || name.Length == 0)
                return dependencies;

            HashSet<string> matched = new HashSet<string>();

            var matches = dependencies.Where(d =>
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
