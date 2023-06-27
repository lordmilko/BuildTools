using System;
using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Dependency
{
    [TestClass]
    public class DotnetDependencyTests : BaseTest
    {
        private const string DotnetCommand = "dotnet";
        private const string LocalDotnet = "C:\\Root\\packages\\dotnet-sdk";

        private static string WindowsDotnetInstall => Path.Combine(Path.GetTempPath(), "dotnet-install.ps1");

        [TestMethod]
        public void DotnetDependency_Install_NotInstalled()
        {
            Test((Lazy<DotnetDependencyInstaller> installer, MockPowerShellService powerShell, MockFileSystemProvider fileSystem, MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[DotnetCommand] = null;

                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
                fileSystem.DirectoryExistsMap[LocalDotnet] = false;
                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
                envProvider.SetValue(WellKnownEnvironmentVariable.Path, string.Empty);

                var dep = new DotnetDependency();

                var result = installer.Value.Install(dep, true);

                Verify(
                    result,
                    "dotnet",
                    DependencyType.Dotnet,
                    DependencyAction.Success,
                    null
                );

                powerShell.AssertInvoked($"& '{WindowsDotnetInstall}' -InstallDir '{LocalDotnet}' -NoPath");
            });
        }

        [TestMethod]
        public void DotnetDependency_Install_GloballyInstalled()
        {
            Test((Lazy<DotnetDependencyInstaller> installer, MockPowerShellService powerShell, MockFileSystemProvider fileSystem, MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.KnownCommands[DotnetCommand] = new MockPowerShellCommand(DotnetCommand);

                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;

                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);

                var dep = new DotnetDependency();

                var result = installer.Value.Install(dep, true);

                Verify(
                    result,
                    "dotnet",
                    DependencyType.Dotnet,
                    DependencyAction.Skipped,
                    null
                );
            });
        }

        [TestMethod]
        public void DotnetDependency_Install_LocallyInstalled()
        {
            Test((Lazy<DotnetDependencyInstaller> installer, MockPowerShellService powerShell, MockFileSystemProvider fileSystem, MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[DotnetCommand] = null;

                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
                fileSystem.DirectoryExistsMap[LocalDotnet] = true;
                fileSystem.EnumerateFilesMap[(LocalDotnet, "*", SearchOption.TopDirectoryOnly)] = new[] {"dotnet.exe"};
                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
                envProvider.SetValue(WellKnownEnvironmentVariable.Path, string.Empty);

                var dep = new DotnetDependency();

                var result = installer.Value.Install(dep, true);

                Verify(
                    result,
                    "dotnet",
                    DependencyType.Dotnet,
                    DependencyAction.Skipped,
                    null
                );
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(DotnetDependencyInstaller),
                typeof(Logger),
                typeof(EnvironmentService),

                { typeof(IAlternateDataStreamService), typeof(MockAlternateDataStreamService) },
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IWebClient), typeof(MockWebClient) },
                { typeof(IConsoleLogger), typeof(MockConsoleLogger) },
                { typeof(IFileLogger), typeof(MockFileLogger) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>())
            };
        }

        private void Verify(DependencyResult result, string name, DependencyType type, DependencyAction action, string version)
        {
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(type, result.Type);
            Assert.AreEqual(action, result.Action);
            Assert.AreEqual(version, result.Version?.ToString());
        }
    }
}
