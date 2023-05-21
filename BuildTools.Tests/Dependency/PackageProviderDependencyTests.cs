using System;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Dependency
{
    [TestClass]
    public class PackageProviderDependencyTests : BaseTest
    {
        [TestMethod]
        public void PackageProvider_WithProviderSuffix()
        {
            Test((PSPackageProviderDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var dep = new PSPackageProviderDependency("FooProvider");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "FooProvider",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Success,
                    "1.0"
                );
            });
        }

        [TestMethod]
        public void PackageProvider_WithoutProviderSuffix()
        {
            Test((PSPackageProviderDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var dep = new PSPackageProviderDependency("foo");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Success,
                    "1.0"
                );
            });
        }

        [TestMethod]
        public void PackageProvider_NoVersion_NoneInstalled()
        {
            Test((PSPackageProviderDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var dep = new PSPackageProviderDependency("foo");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Success,
                    "1.0"
                );
            });
        }

        [TestMethod]
        public void PackageProvider_NoVersion_AlreadyInstalled()
        {
            Test((PSPackageProviderDependencyInstaller installer, MockPowerShellService powerShell) =>
            {
                powerShell.InstalledPackageProvider = new MockPackageProvider("foo", new Version("2.0"));

                var dep = new PSPackageProviderDependency("foo");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Skipped,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PackageProvider_MinimumVersion_NoneInstalled()
        {
            Test((PSPackageProviderDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var dep = new PSPackageProviderDependency("foo", minimumVersion: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Success,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PackageProvider_MinimumVersion_LowerInstalled()
        {
            Test((PSPackageProviderDependencyInstaller installer, MockPowerShellService powerShell) =>
            {
                powerShell.InstalledPackageProvider = new MockPackageProvider("foo", new Version("1.0"));

                var dep = new PSPackageProviderDependency("foo", minimumVersion: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Success,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PackageProvider_MinimumVersion_ExactInstalled()
        {
            Test((PSPackageProviderDependencyInstaller installer, MockPowerShellService powerShell) =>
            {
                powerShell.InstalledPackageProvider = new MockPackageProvider("foo", new Version("2.0"));

                var dep = new PSPackageProviderDependency("foo", minimumVersion: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Skipped,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PackageProvider_MinimumVersion_HigherInstalled()
        {
            Test((PSPackageProviderDependencyInstaller installer, MockPowerShellService powerShell) =>
            {
                powerShell.InstalledPackageProvider = new MockPackageProvider("foo", new Version("2.0"));

                var dep = new PSPackageProviderDependency("foo", minimumVersion: "1.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackageProvider,
                    DependencyAction.Skipped,
                    "2.0"
                );
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(PSPackageProviderDependencyInstaller),
                typeof(Logger),
                typeof(EnvironmentService),
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IConsoleLogger), typeof(MockConsoleLogger) },
                { typeof(IFileLogger), typeof(MockFileLogger) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
            };
        }

        private void Verify(DependencyResult result, string name, DependencyType type, DependencyAction action, string version)
        {
            Assert.AreEqual(name, result.Name);
            Assert.AreEqual(type, result.Type);
            Assert.AreEqual(action, result.Action);
            Assert.AreEqual(version, result.Version.ToString());
        }
    }
}
