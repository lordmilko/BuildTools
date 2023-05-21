using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Dependency
{
    [TestClass]
    public class DotnetDependencyTests : BaseTest
    {
        private const string DotnetCommand = "dotnet";
        private const string LocalDotnet = "C:\\packages\\dotnet-sdk";

        private static string WindowsDotnetInstall => Path.Combine(Path.GetTempPath(), "dotnet-install.ps1");

        [TestMethod]
        public void DotnetDependency_Install_NotInstalled()
        {
            Test((DotnetDependencyInstaller installer, MockPowerShellService powerShell, MockFileSystemProvider fileSystem, MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[DotnetCommand] = null;

                fileSystem.DirectoryMap[LocalDotnet] = false;
                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
                envProvider.SetValue(WellKnownEnvironmentVariable.Path, string.Empty);

                var dep = new DotnetDependency();

                var result = installer.Install(dep, true);

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
            Test((DotnetDependencyInstaller installer, MockPowerShellService powerShell, MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.KnownCommands[DotnetCommand] = new MockPowerShellCommand(DotnetCommand);
                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);

                var dep = new DotnetDependency();

                var result = installer.Install(dep, true);

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
            Test((DotnetDependencyInstaller installer, MockPowerShellService powerShell, MockFileSystemProvider fileSystem, MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[DotnetCommand] = null;

                fileSystem.DirectoryMap[LocalDotnet] = true;
                fileSystem.DirectoryFiles[LocalDotnet] = new[] {"dotnet.exe"};
                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
                envProvider.SetValue(WellKnownEnvironmentVariable.Path, string.Empty);

                var dep = new DotnetDependency();

                var result = installer.Install(dep, true);

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
                { typeof(IProjectConfigProvider), typeof(MockProjectConfigProvider) }
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
