using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace BuildTools
{
    class AppveyorCSharpPackageProvider : AppveyorPackageProvider
    {
        private readonly CSharpPackageSourceService csharpPackageSourceService;
        private readonly CSharpPackageProvider csharpPackageProvider;

        public AppveyorCSharpPackageProvider(
            CSharpPackageSourceService csharpPackageSourceService,
            CSharpPackageProvider csharpPackageProvider,
            AppveyorPackageProviderServices services) : base(services)
        {
            this.csharpPackageSourceService = csharpPackageSourceService;
            this.csharpPackageProvider = csharpPackageProvider;
        }

        public void Execute(PackageConfig config)
        {
            logger.LogSubHeader("\tProcessing C# package");

            csharpPackageSourceService.Install();

            csharpPackageProvider.Execute(config, GetCSharpVersion(config.IsLegacy));

            TestPackage(config);

            MoveAppveyorPackages(string.Empty);

            csharpPackageSourceService.Uninstall();
        }

        private Version GetCSharpVersion(bool isLegacy)
        {
            if (environmentService.IsAppveyor)
            {
                //Trim any version qualifiers (-build.2, etc)
                var version = environmentService.AppveyorBuildVersion;

                if (string.IsNullOrEmpty(version))
                    throw new InvalidOperationException($"Cannot get C# version as environment variable '{WellKnownEnvironmentVariable.AppveyorBuildVersion}' is not defined");

                var index = version.IndexOf('-');

                if (index != -1)
                    return new Version(version.Substring(0, index));
            }

            return new Version(getVersionService.GetVersion(isLegacy).File.ToString(3));
        }

        protected override void TestPackageDefinition(PackageConfig config, string extractFolder)
        {
            logger.LogInformation("\t\t\tValidating package definition");

            var nuspec = fileSystem.EnumerateFiles(extractFolder, "*.nuspec").FirstOrDefault();

            if (nuspec == null)
                throw new FileNotFoundException($"Couldn't find nuspec in folder '{extractFolder}'");

            var content = fileSystem.ReadFileText(nuspec);
            var xml = XDocument.Parse(content);

            var ns = xml.Root.Name.Namespace;

            var metadata = xml.Element(ns + "package").Element(ns + "metadata");

            //Validate release notes
            var expectedVersion = getVersionService.GetVersion(config.IsLegacy).File.ToString(3);

            var nuspecVersion = metadata.Element(ns + "version").Value;
            var releaseNotes = metadata.Element(ns + "releaseNotes").Value;
            var repository = metadata.Element(ns + "repository")?.Value;

            if (expectedVersion != nuspecVersion)
                throw new InvalidOperationException($"Expected package to have version '{expectedVersion}' but instead had version '{nuspecVersion}'");

            var expectedUrl = $"https://github.com/lordmilko/{configProvider.Config.Name}/releases/tag/v{expectedVersion}";

            if (!releaseNotes.Contains(expectedUrl))
                throw new InvalidOperationException($"Release notes did not contain correct release version. Expected notes to contain URL '{expectedUrl}'. Release notes were '{releaseNotes}'");

            if (!config.IsLegacy && !string.IsNullOrEmpty(repository))
                throw new InvalidOperationException("Package did not contain SourceLink details");
        }

        protected override void TestPackageContents(PackageConfig config, string extractFolder)
        {
            var csharpFiles = configProvider.Config.PackageFiles.CSharp;

            var context = new PackageFileContext(config.Configuration, config.IsLegacy, configProvider.Config.PowerShellMultiTargeted, configProvider.Config.DebugTargetFramework);
            var required = csharpFiles.Where(v => v.Condition == null || v.Condition(context)).Select(v => v.Name).ToArray();

            if (!config.IsLegacy && config.Configuration != BuildConfiguration.Release)
            {
                var debugVersion = configProvider.Config.DebugTargetFramework;

                if (debugVersion != null)
                {
                    logger.LogInformation($"\t\t\t\tUsing debug build '{debugVersion}' for testing nupkg contents");

                    for (var i = 0; i < required.Length; i++)
                    {
                        var item = required[i];

                        var prefix = "lib\\";

                        if (item.StartsWith(prefix))
                        {
                            var nextSlash = item.IndexOf('\\', prefix.Length + 1);

                            var endStr = item.Substring(nextSlash);

                            var newStr = $"{prefix}{debugVersion}{endStr}";

                            required[i] = newStr;
                        }
                    }
                }
            }

            TestPackageContents(extractFolder, required);
        }

        protected override void TestPackageInstalls(PackageConfig config)
        {
            logger.LogInformation("\t\t\tTesting package installs properly");

            var nupkg = GetCSharpNupkg();
            var packageName = Path.GetFileNameWithoutExtension(nupkg);
            var installPath = Path.Combine(PackageSourceService.PackageLocation, packageName);

            if (IsNuGetPackageInstalled(installPath))
            {
                logger.LogInformation($"\t\t\t\t'{packageName}' is already installed. Uninstalling package");
                UninstallCSharpPackageInternal(installPath);
            }

            InstallCSharpPackageInternal(installPath);
            TestCSharpPackageInstallInternal(config);
            UninstallCSharpPackageInternal(installPath);
        }

        private bool IsNuGetPackageInstalled(string installPath) =>
            powerShell.GetPackage(configProvider.Config.PowerShellModuleName, installPath) != null || fileSystem.DirectoryExists(installPath);

        private void InstallCSharpPackageInternal(string installPath)
        {
            logger.LogInformation($"\t\t\t\tInstalling package from {PackageSourceService.RepoName}");

            powerShell.InstallPackage(
                configProvider.Config.PowerShellModuleName,
                providerName: "NuGet",
                source: PackageSourceService.RepoName,
                destination: PackageSourceService.PackageLocation,
                skipDependencies: true
            );

            if (!(fileSystem.DirectoryExists(installPath)))
                throw new InvalidOperationException("Package did not install successfully");
        }

        private void TestCSharpPackageInstallInternal(PackageConfig config)
        {
            logger.LogInformation("\t\t\t\tTesting package contents");

            var version = getVersionService.GetVersion(config.IsLegacy).File.ToString(3);

            var path = Path.Combine(PackageSourceService.PackageLocation, $"{configProvider.Config.PowerShellModuleName}.{version}", "lib");

            var candidates = fileSystem.EnumerateDirectories(path).ToArray();

            var folders = candidates.Where(v =>
            {
                var dir = Path.GetFileName(v);

                return dir.StartsWith("net4") || dir.StartsWith("netstandard");
            }).ToArray();

            if (folders.Length == 0)
                throw new InvalidOperationException($"Could not find a .NET Framework/.NET Standard directory under '{path}'. Available directories: {string.Join(", ", candidates.Select(Path.GetFileName))}");

            foreach (var folder in folders)
            {
                var dll = Path.Combine(folder, $"{configProvider.Config.Name}.dll");

                if (!fileSystem.FileExists(dll))
                    throw new FileNotFoundException($"Could not find expected library DLL '{dll}'", dll);

                //Version.props should be included by Directory.Build.props. If we forgot to
                //include it, the file version will be 1.0. If our expected file version is not
                //1.0, this indicates we forgot to include Version.props

                var expectedVersionInfo = getVersionService.GetVersion(config.IsLegacy);
                var actualVersionInfo = fileSystem.GetVersionInfo(dll);

                if (expectedVersionInfo.File != actualVersionInfo && actualVersionInfo == new Version(1, 0))
                    throw new InvalidOperationException($"File '{dll}' has default file version '{actualVersionInfo}' however it was expected to have version '{expectedVersionInfo.File}'. This indicates Version.props was not imported via any csproj/props file. Consider adding '<Import Project=\"..\\build\\Version.props\" />' to the top of a 'src\\Directory.Build.props' file.");

                var tests = configProvider.Config.PackageTests.CSharp;

                if (tests.Any(t => !(t is ScriptPackageTest)))
                    throw new InvalidOperationException($"C# packages only support package tests of type {nameof(ScriptPackageTest)}");

                var scriptTests = tests.Cast<ScriptPackageTest>().ToArray();

                foreach (var test in scriptTests)
                    test.Test(processService, powerShell.Edition, dll);
            }
        }

        private void UninstallCSharpPackageInternal(string installPath)
        {
            logger.LogInformation("\t\t\t\tUninstall package");

            var package = powerShell.GetPackage(configProvider.Config.PowerShellModuleName, PackageSourceService.PackageLocation);

            if (package != null)
                powerShell.UninstallPackage(package);

            if (fileSystem.DirectoryExists(installPath))
                throw new InvalidOperationException("Module did not uninstall properly");
        }
    }
}
