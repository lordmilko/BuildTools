using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Dependency
{
    [TestClass]
    public class TargetingPackDependencyTests : BaseTest
    {
        private const string NETFrameworkReferenceAssemblies = "C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework";
        private const string NETFrameworkReferenceAssemblies452 = NETFrameworkReferenceAssemblies + "\\v4.5.2";

        private const string Url452 = "https://download.microsoft.com/download/4/3/B/43B61315-B2CE-4F5B-9E32-34CCA07B2F0E/NDP452-KB2901951-x86-x64-DevPack.exe";
        private static string File452 => Path.Combine(Path.GetTempPath(), "NDP452-KB2901951-x86-x64-DevPack.exe");
        private const string Hash452 = "E37AA3BC40DAF9B4625F8CE44C1568A4";

        [TestMethod]
        public void TargetingPackDependency_Install_NotInstalled()
        {
            Test((
                TargetingPackDependencyInstaller installer,
                MockPowerShellService powerShell,
                MockProcessService process,
                MockFileSystemProvider fileSystem,
                MockWebClient webClient) =>
            {
                powerShell.IsWindows = true;

                fileSystem.DirectoryMap[NETFrameworkReferenceAssemblies] = true;
                fileSystem.DirectoryMap[NETFrameworkReferenceAssemblies452] = false;

                fileSystem.FileMap[File452] = false;

                var dep = new PSPackageDependency("net452", version: "4.5.2");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "net452",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "4.5.2"
                );

                webClient.AssertDownloaded(Url452, File452);
                process.AssertExecuted($"{File452} /quiet /norestart");
            });
        }

        [TestMethod]
        public void TargetingPackDependency_Install_HashMatch()
        {
            Test((
                TargetingPackDependencyInstaller installer,
                MockPowerShellService powerShell,
                MockProcessService process,
                MockFileSystemProvider fileSystem,
                MockWebClient webClient,
                MockHasher hasher) =>
            {
                powerShell.IsWindows = true;

                fileSystem.DirectoryMap[NETFrameworkReferenceAssemblies] = true;
                fileSystem.DirectoryMap[NETFrameworkReferenceAssemblies452] = false;

                fileSystem.FileMap[File452] = true;

                hasher.Hash = Hash452;

                var dep = new PSPackageDependency("net452", version: "4.5.2");

                var result = installer.Install(dep, true);

                Verify(
                    result,
                    "net452",
                    DependencyType.PSPackage,
                    DependencyAction.Success,
                    "4.5.2"
                );

                Assert.AreEqual(0, webClient.Downloaded.Count);
                process.AssertExecuted($"{File452} /quiet /norestart");
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(TargetingPackDependencyInstaller),
                typeof(Logger),
                typeof(EnvironmentService),
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IHasher), typeof(MockHasher) },
                { typeof(IProcessService), typeof(MockProcessService) },
                { typeof(IWebClient), typeof(MockWebClient) },
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
