﻿using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Dependency
{
    [TestClass]
    public class ChocolateyDependencyTests : BaseTest
    {
        private const string ChocolateyCommand = "chocolatey";
        private const string ChocolateyInstall = "C:\\ProgramData\\chocolatey";
        private const string ChocolateyExe = ChocolateyInstall + "\\bin\\chocolatey.exe";
        private const string ChocolateyUrl = "https://chocolatey.org/install.ps1";
        private const string ChocoScript = "function installChoco {}";

        private const string CodeCovCommand = "codecov";
        private const string CodeCovExe = ChocolateyInstall + "\\bin\\codecov.exe";

        [TestMethod]
        public void ChocolateyDependency_Install_Manager()
        {
            Test((
                ChocolateyDependencyInstaller installer,
                MockPowerShellService powerShell,
                MockFileSystemProvider fileSystem,
                MockWebClient webClient,
                MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[ChocolateyCommand] = new MockPowerShellCommand(ChocolateyCommand);

                fileSystem.DirectoryMap[ChocolateyInstall] = true;
                fileSystem.FileMap[ChocolateyExe] = false;
                envProvider.SetValue(WellKnownEnvironmentVariable.ChocolateyInstall, null);
                webClient.DownloadedString[ChocolateyUrl] = ChocoScript;

                var dep = new ChocolateyDependency();

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "chocolatey",
                    DependencyType.Chocolatey,
                    DependencyAction.Success,
                    null
                );

                powerShell.AssertInvoked(ChocoScript);
            });
        }

        [TestMethod]
        public void ChocolateyDependency_Install_Manager_AlreadyInstalled()
        {
            Test((
                ChocolateyDependencyInstaller installer,
                MockPowerShellService powerShell,
                MockFileSystemProvider fileSystem,
                MockWebClient webClient,
                MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[ChocolateyCommand] = new MockPowerShellCommand(ChocolateyCommand);

                fileSystem.DirectoryMap[ChocolateyInstall] = true;
                fileSystem.FileMap[ChocolateyExe] = true;
                envProvider.SetValue(WellKnownEnvironmentVariable.ChocolateyInstall, null);

                var dep = new ChocolateyDependency();

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "chocolatey",
                    DependencyType.Chocolatey,
                    DependencyAction.Skipped,
                    "1.0"
                );
            });
        }

        [TestMethod]
        public void ChocolateyDependency_Install_Command()
        {
            Test((
                ChocolateyDependencyInstaller installer,
                MockPowerShellService powerShell,
                MockFileSystemProvider fileSystem,
                MockWebClient webClient,
                MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[CodeCovCommand] = null;

                fileSystem.DirectoryMap[ChocolateyInstall] = true;
                fileSystem.FileMap[CodeCovExe] = false;
                fileSystem.FileMap[ChocolateyExe] = true;
                envProvider.SetValue(WellKnownEnvironmentVariable.ChocolateyInstall, null);

                var dep = new ChocolateyPackageDependency(CodeCovCommand);

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "codecov",
                    DependencyType.Chocolatey,
                    DependencyAction.Success,
                    null
                );
            });
        }

        [TestMethod]
        public void ChocolateyDependency_Install_Command_AlreadyInstalled()
        {
            Test((
                ChocolateyDependencyInstaller installer,
                MockPowerShellService powerShell,
                MockFileSystemProvider fileSystem,
                MockWebClient webClient,
                MockEnvironmentVariableProvider envProvider) =>
            {
                powerShell.IsWindows = true;
                powerShell.KnownCommands[CodeCovCommand] = new MockPowerShellCommand(CodeCovCommand);

                fileSystem.DirectoryMap[ChocolateyInstall] = true;
                fileSystem.FileMap[CodeCovExe] = true;
                envProvider.SetValue(WellKnownEnvironmentVariable.ChocolateyInstall, null);

                var dep = new ChocolateyPackageDependency(CodeCovCommand);

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "codecov",
                    DependencyType.Chocolatey,
                    DependencyAction.Skipped,
                    "1.0"
                );
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(DotnetDependencyInstaller),
                typeof(ChocolateyDependencyInstaller),
                typeof(PSPackageDependencyInstaller),
                typeof(PSPackageProviderDependencyInstaller),
                typeof(TargetingPackDependencyInstaller),

                typeof(Logger),
                typeof(DependencyProvider),
                typeof(EnvironmentService),

                { typeof(IAlternateDataStreamService), typeof(MockAlternateDataStreamService) },
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IHasher), typeof(MockHasher) },
                { typeof(IProcessService), typeof(MockProcessService) },
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