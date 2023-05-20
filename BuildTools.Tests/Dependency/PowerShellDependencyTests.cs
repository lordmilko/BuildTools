using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Dependency
{
    [TestClass]
    public class PowerShellDependencyTests : BaseTest
    {
        [TestMethod]
        public void PowerShellDependency_Install_NoVersion_NoneInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var dep = new PSPackageDependency("foo");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "1.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_RequiredVersion_NoneInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var dep = new PSPackageDependency("foo", version: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_RequiredVersion_LowerInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var mockPowerShell = (MockPowerShellService)powerShell;

                mockPowerShell.InstalledModules = new IPowerShellModule[]
                {
                    new MockPowerShellModule("foo", "1.0")
                };

                var dep = new PSPackageDependency("foo", version: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_RequiredVersion_ExactInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var mockPowerShell = (MockPowerShellService)powerShell;

                mockPowerShell.InstalledModules = new IPowerShellModule[]
                {
                    new MockPowerShellModule("foo", "2.0")
                };

                var dep = new PSPackageDependency("foo", version: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Skipped,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_RequiredVersion_HigherInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var mockPowerShell = (MockPowerShellService)powerShell;

                mockPowerShell.InstalledModules = new IPowerShellModule[]
                {
                    new MockPowerShellModule("foo", "2.0")
                };

                var dep = new PSPackageDependency("foo", version: "1.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "1.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_MinimumVersion_NoneInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var dep = new PSPackageDependency("foo", minimumVersion: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_MinimumVersion_LowerInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var mockPowerShell = (MockPowerShellService)powerShell;

                mockPowerShell.InstalledModules = new IPowerShellModule[]
                {
                    new MockPowerShellModule("foo", "1.0")
                };

                var dep = new PSPackageDependency("foo", minimumVersion: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_MinimumVersion_ExactInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var mockPowerShell = (MockPowerShellService)powerShell;

                mockPowerShell.InstalledModules = new IPowerShellModule[]
                {
                    new MockPowerShellModule("foo", "2.0")
                };

                var dep = new PSPackageDependency("foo", minimumVersion: "2.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Skipped,
                    "2.0"
                );
            });
        }

        [TestMethod]
        public void PowerShellDependency_Install_MinimumVersion_HigherInstalled()
        {
            Test((PSPackageDependencyInstaller installer, IPowerShellService powerShell) =>
            {
                var mockPowerShell = (MockPowerShellService)powerShell;

                mockPowerShell.InstalledModules = new IPowerShellModule[]
                {
                    new MockPowerShellModule("foo", "2.0")
                };

                var dep = new PSPackageDependency("foo", minimumVersion: "1.0");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "foo",
                    DependencyType.PSPackage,
                    DependencyAction.Skipped,
                    "2.0"
                );
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(PSPackageDependencyInstaller),
                typeof(Logger),
                typeof(EnvironmentService),
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
