using System;
using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class ClearBuildTests : BaseTest
    {
        [TestMethod]
        public void ClearBuild_Normal()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<ClearBuildService> clearBuild) =>
            {
                //Preparation
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] {"PrtgAPI.sln", "PrtgAPIv17.sln"};
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.*nupkg", SearchOption.AllDirectories)] = new[] {"foo.nupkg"};
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.zip", SearchOption.TopDirectoryOnly)] = new[] {"foo.zip"};
                fileSystem.FileExistsMap["C:\\Root\\msbuild.binlog"] = false;
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
                fileSystem.FileExistsMap["C:\\Root\\PrtgAPIv17.sln"] = true;

                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
                powerShell.KnownCommands["dotnet"] = new MockPowerShellCommand("dotnet");

                //Action
                clearBuild.Value.ClearMSBuild(BuildConfiguration.Debug, false);

                //Validation
                fileSystem.AssertDeletedFiles("foo.nupkg", "foo.zip");

                process.AssertExecuted("C:\\dotnet.exe clean \"C:\\Root\\PrtgAPIv17.sln\" -c Debug");
            });
        }

        [TestMethod]
        public void ClearBuild_Legacy()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<ClearBuildService> clearBuild) =>
            {
                //Preparation
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.*nupkg", SearchOption.AllDirectories)] = new[] { "foo.nupkg" };
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.zip", SearchOption.TopDirectoryOnly)] = new[] { "foo.zip" };
                fileSystem.FileExistsMap["C:\\Root\\msbuild.binlog"] = false;
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;

                //Action
                clearBuild.Value.ClearMSBuild(BuildConfiguration.Debug, true);

                //Validation
                fileSystem.AssertDeletedFiles("foo.nupkg", "foo.zip");

                process.AssertExecuted("C:\\msbuild.exe /t:clean \"C:\\Root\\PrtgAPI.sln\" /p:Configuration=Debug");
            });
        }

        [TestMethod]
        public void ClearBuild_Full()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<ClearBuildService> clearBuild) =>
            {
                //Preparation
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.*nupkg", SearchOption.AllDirectories)] = new[] { "foo.nupkg" };
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.zip", SearchOption.TopDirectoryOnly)] = new[] { "foo.zip" };
                fileSystem.FileExistsMap["C:\\Root\\msbuild.binlog"] = false;
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;

                fileSystem.EnumerateFilesMap[("C:\\Root", "*.csproj", SearchOption.AllDirectories)] = new[]
                {
                    "C:\\Root\\first\\first.csproj",
                    "C:\\Root\\second\\second.csproj",
                };
                fileSystem.DirectoryExistsMap["C:\\Root\\first\\bin"] = true;
                fileSystem.EnumerateFilesMap[("C:\\Root\\first\\bin", "*", SearchOption.AllDirectories)] = new[] {"first.exe"};
                fileSystem.EnumerateDirectoriesMap[("C:\\Root\\first\\bin", "*", SearchOption.AllDirectories)] = new string[0];
                fileSystem.DirectoryExistsMap["C:\\Root\\first\\obj"] = false;
                fileSystem.DirectoryExistsMap["C:\\Root\\second\\bin"] = false;
                fileSystem.DirectoryExistsMap["C:\\Root\\second\\obj"] = false;

                //Action
                clearBuild.Value.ClearFull();

                //Validation
                Assert.AreEqual(0, process.Executed.Count);

                fileSystem.AssertDeletedFiles("first.exe", "foo.nupkg", "foo.zip");
                fileSystem.AssertDeletedDirectories("C:\\Root\\first\\bin");
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(ClearBuildService),

                typeof(EnvironmentService),
                typeof(Logger),

                typeof(DependencyProvider),

                typeof(DotnetDependencyInstaller),
                typeof(ChocolateyDependencyInstaller),
                typeof(PSPackageDependencyInstaller),
                typeof(PSPackageProviderDependencyInstaller),
                typeof(TargetingPackDependencyInstaller),

                { typeof(IConsoleLogger), typeof(MockConsoleLogger) },
                { typeof(IFileLogger), typeof(MockFileLogger) },

                { typeof(IAlternateDataStreamService), typeof(MockAlternateDataStreamService) },
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IHasher), typeof(MockHasher) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IProcessService), typeof(MockProcessService) },
                { typeof(IVsProductLocator), typeof(MockVsProductLocator) },
                { typeof(IWebClient), typeof(MockWebClient) },

                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>())
            };
        }
    }
}
