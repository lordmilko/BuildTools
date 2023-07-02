using System;
using System.Collections;
using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class NewPackageTests : BaseTest
    {
        private const string SolutionRoot = "C:\\Root";
        private const string SourceRoot = SolutionRoot + "\\src";
        private const string PowerShellProject = SourceRoot + "\\PrtgAPI.PowerShell";
        private const string PowerShellProjectBin = PowerShellProject + "\\bin";
        private const string PowerShellProjectBinDebug = PowerShellProjectBin + "\\Debug";

        private static readonly string TempOutputPrtgAPI = Path.Combine(PackageSourceService.RepoLocation, "TempOutput", "PrtgAPI");

        private void MockFileSystemCommon(MockFileSystemProvider fileSystem, bool redist = false)
        {
            fileSystem.EnumerateFilesMap[(SolutionRoot, "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
            fileSystem.DirectoryExistsMap[SourceRoot] = true;

            fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.*nupkg", SearchOption.TopDirectoryOnly)] = redist ? new string[0] : new[] { "C:\\foo.nupkg" };

            fileSystem.EnumerateFilesMap[(PackageSourceService.RepoLocation, "*.zip", SearchOption.TopDirectoryOnly)] = new[] { "C:\\foo.zip" };
        }

        private void MockFileSystemCSharp(MockFileSystemProvider fileSystem)
        {
            fileSystem.EnumerateFilesMap[(SolutionRoot, "*.csproj", SearchOption.AllDirectories)] = new[]
            {
                "C:\\Root\\first\\PrtgAPI.csproj",
                "C:\\Root\\second\\PrtgAPI.PowerShell.csproj",
            };

            fileSystem.DirectoryExistsMap[PackageSourceService.RepoLocation] = true;
        }

        private void MockFileSystemPowerShell(MockFileSystemProvider fileSystem)
        {
            fileSystem.DirectoryExistsMap[PowerShellProject] = true;
            fileSystem.DirectoryExistsMap[PowerShellProjectBin] = true;
            fileSystem.DirectoryExistsMap[PowerShellProjectBinDebug] = true;
            fileSystem.EnumerateDirectoriesMap[(PowerShellProjectBinDebug, "net4*", SearchOption.TopDirectoryOnly)] = new[] { "C:\\net452" };
            fileSystem.FileExistsMap["C:\\net452\\PrtgAPI\\PrtgAPI.PowerShell.dll"] = true;
            fileSystem.DirectoryExistsMap["C:\\net452\\PrtgAPI"] = true;

            var exts = new[]
            {
                "*.cmd",
                "*.pdb",
                "*.sh",
                "*.json",

                "*.dll"
            };

            foreach (var ext in exts)
                fileSystem.EnumerateFilesMap[(TempOutputPrtgAPI, ext, SearchOption.AllDirectories)] = new string[0];

            fileSystem.DirectoryExistsMap[PackageSourceService.RepoLocation] = true;
        }

        [TestMethod]
        public void NewPackage_All_Normal()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                Lazy<IProjectConfigProvider> configProvider,
                MockProcessService process,
                Lazy<NewPackageService> newPackage) =>
            {
                MockFileSystemCommon(fileSystem);
                MockFileSystemCSharp(fileSystem);
                MockFileSystemPowerShell(fileSystem);
                MockVersion(fileSystem, powerShell);

                //Redist
                fileSystem.FileExistsMap[Path.Combine(PackageSourceService.RepoLocation, "PrtgAPI.zip")] = true;
                fileSystem.EnumerateDirectoryFileSystemEntriesMap[(TempOutputPrtgAPI, "*", SearchOption.AllDirectories)] = new string[0];

                powerShell.KnownCommands["dotnet"] = new MockPowerShellCommand("dotnet");

                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);

                newPackage.Value.Execute(
                    configProvider.Value.Config.PackageTypes,
                    BuildConfiguration.Debug,
                    false
                );

                process.AssertExecuted($"C:\\dotnet.exe pack C:\\Root\\first\\PrtgAPI.csproj --include-symbols --no-restore --no-build -c Debug --output \"{PackageSourceService.RepoLocation}\" /nologo -p:EnableSourceLink=true;SymbolPackageFormat=snupkg");

                //The zip file will be listed twice simply because our MockFileSystemProvider lists the zip file any time MovePackages() asks if there's any zip files to move
                fileSystem.AssertMovedFile("C:\\foo.nupkg", "C:\\Root\\foo.nupkg");
                fileSystem.AssertMovedFile("C:\\foo.zip", "C:\\Root\\foo.zip");
                fileSystem.AssertMovedFile("C:\\foo.nupkg", "C:\\Root\\foo_PowerShell.nupkg");
            });
        }

        [TestMethod]
        public void NewPackage_CSharp_Normal()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                Lazy<IProjectConfigProvider> configProvider,
                MockProcessService process,
                Lazy<NewPackageService> newPackage) =>
            {
                MockFileSystemCommon(fileSystem);
                MockFileSystemCSharp(fileSystem);
                MockVersion(fileSystem, powerShell);

                powerShell.KnownCommands["dotnet"] = new MockPowerShellCommand("dotnet");
                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);

                newPackage.Value.Execute(
                    new[]{PackageType.CSharp},
                    BuildConfiguration.Debug,
                    false
                );

                process.AssertExecuted($"C:\\dotnet.exe pack C:\\Root\\first\\PrtgAPI.csproj --include-symbols --no-restore --no-build -c Debug --output \"{PackageSourceService.RepoLocation}\" /nologo -p:EnableSourceLink=true;SymbolPackageFormat=snupkg");

                fileSystem.AssertMovedFile("C:\\foo.nupkg", "C:\\Root\\foo.nupkg");
                fileSystem.AssertMovedFile("C:\\foo.zip", "C:\\Root\\foo.zip");
            });
        }

        [TestMethod]
        public void NewPackage_PowerShell_Normal()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                Lazy<IProjectConfigProvider> configProvider,
                MockProcessService process,
                Lazy<NewPackageService> newPackage) =>
            {
                MockFileSystemCommon(fileSystem);
                MockFileSystemPowerShell(fileSystem);

                newPackage.Value.Execute(
                    new[] { PackageType.PowerShell },
                    BuildConfiguration.Debug,
                    false
                );

                fileSystem.AssertMovedFile("C:\\foo.zip", "C:\\Root\\foo.zip");
                fileSystem.AssertMovedFile("C:\\foo.nupkg", "C:\\Root\\foo_PowerShell.nupkg");
            });
        }

        [TestMethod]
        public void NewPackage_Redist_Normal()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                Lazy<IProjectConfigProvider> configProvider,
                MockProcessService process,
                Lazy<NewPackageService> newPackage) =>
            {
                MockFileSystemCommon(fileSystem, true);
                MockFileSystemPowerShell(fileSystem);

                //Redist
                fileSystem.FileExistsMap[Path.Combine(PackageSourceService.RepoLocation, "PrtgAPI.zip")] = true;
                fileSystem.EnumerateDirectoryFileSystemEntriesMap[(TempOutputPrtgAPI, "*", SearchOption.AllDirectories)] = new string[0];

                newPackage.Value.Execute(
                    new[] { PackageType.Redistributable },
                    BuildConfiguration.Debug,
                    false
                );

                fileSystem.AssertMovedFile("C:\\foo.zip", "C:\\Root\\foo.zip");
            });
        }

        private void MockVersion(MockFileSystemProvider fileSystem, MockPowerShellService powerShell)
        {
            fileSystem.DirectoryExistsMap["C:\\Root\\.git"] = true;

            fileSystem.FileExistsMap["C:\\Root\\build\\Version.props"] = true;
            fileSystem.EnumerateFilesMap[("C:\\Root\\src\\PrtgAPI.PowerShell", "*.psd1", SearchOption.TopDirectoryOnly)] = new[] { "C:\\Root\\src\\PrtgAPI.PowerShell\\PrtgAPI.psd1" };
            fileSystem.GetFileTextMap["C:\\Root\\src\\PrtgAPI.PowerShell\\PrtgAPI.psd1"] = "@{}";

            fileSystem.GetFileTextMap["C:\\Root\\build\\Version.props"] = @"
<Project>
  <PropertyGroup>
    <Version>0.9.16</Version>
    <AssemblyVersion>0.9.0.0</AssemblyVersion>
    <FileVersion>0.9.16.0</FileVersion>
    <InformationalVersion>0.9.16</InformationalVersion>
  </PropertyGroup>
</Project>
";

            powerShell.InvokeScriptMap["@{}"] = new Hashtable
            {
                { "ModuleVersion", "0.9.16" },
                { "PrivateData", new Hashtable
                {
                    { "PSData", new Hashtable
                    {
                        { "ReleaseNotes", @"Release Notes: https://github.com/lordmilko/PrtgAPI/releases/tag/v0.9.16

---

PrtgAPI is a C#/PowerShell library that abstracts away the complexity of interfacing with the PRTG Network Monitor HTTP API.
" }
                    } }
                } }
            };

            powerShell.KnownCommands["git"] = new MockPowerShellCommand("git");
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(GetVersionService),
                typeof(NewPackageService),

                typeof(EnvironmentService),
                typeof(Logger),

                typeof(DependencyProvider),

                typeof(DotnetDependencyInstaller),
                typeof(ChocolateyDependencyInstaller),
                typeof(PSPackageDependencyInstaller),
                typeof(PSPackageProviderDependencyInstaller),
                typeof(TargetingPackDependencyInstaller),

                typeof(CSharpPackageProvider),
                typeof(PowerShellPackageProvider),
                typeof(CSharpPackageSourceService),
                typeof(PowerShellPackageSourceService),

                { typeof(IConsoleLogger), typeof(MockConsoleLogger) },
                { typeof(IFileLogger), typeof(MockFileLogger) },

                { typeof(IAlternateDataStreamService), typeof(MockAlternateDataStreamService) },
                { typeof(ICommandService), typeof(MockCommandService) },
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IHasher), typeof(MockHasher) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IProcessService), typeof(MockProcessService) },
                { typeof(IWebClient), typeof(MockWebClient) },
                { typeof(IZipService), typeof(ZipService) },

                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>())
            };
        }
    }
}
