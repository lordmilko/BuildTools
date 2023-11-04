using System;
using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests.Implementation
{
    [TestClass]
    public class GetAppveyorVersionTests : BaseTest
    {
        [TestMethod]
        public void GetAppveyorVersion_SameAssembly_FirstBuild_AfterSameRelease_IsFirstPreview_1()
        {
            //Release 0.1 -> Commit (p1) = Reset Counter = 0.1.1-preview.1

            Test(
                assembly: "0.1.0",
                lastBuild: "0.1.0",
                lastRelease: "0.1.0",
                expected: "0.1.1-preview.1"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_SameAssembly_SecondBuild_AfterSameRelease_IsSecondPreview()
        {
            //Release 0.1 -> Commit (p1) -> Commit (p2) = 0.1.1-preview.2

            Test(
                assembly: "0.1.0",
                lastBuild: "0.1.1-preview.1",
                lastRelease: "0.1.0",
                expected: "0.1.1-preview.2"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_NewAssembly_AfterPreviewBuild_IsNewRelease()
        {
            //Release 0.1 -> Commit (p1) -> Commit (p2) -> Set 0.1.1 = 0.1.1

            Test(
                assembly: "0.1.1",
                lastBuild: "0.1.1-preview.2",
                lastRelease: "0.1.0",
                expected: "0.1.1"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_NewAssembly_SecondBuild_AfterPreviousRelease_IsSecondBuild()
        {
            //Release 0.1 -> Commit (p1) -> Commit (p2) -> Set 0.1.1 -> Commit (u1) = Reset Counter + 0.1.1-build.1

            Test(
                assembly: "0.1.1",
                lastBuild: "0.1.1",
                lastRelease: "0.1.0",
                expected: "0.1.1-build.1"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_SameAssembly_FirstBuild_AfterSameRelease_IsFirstPreview_2()
        {
            //Release 0.1 -> Commit (p1) -> Commit (p2) -> Set/Release 0.1.1 -> Commit (p1) = Reset Counter + 0.1.2-preview.{build}

            Test(
                assembly: "0.1.1",
                lastBuild: "0.1.1",
                lastRelease: "0.1.1",
                expected: "0.1.2-preview.1"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_NewAssembly_AfterPreviousRelease_IsNewAssembly()
        {
            //Release 0.1 -> Release 0.2 = 0.2

            Test(
                assembly: "0.2.0",
                lastBuild: "0.1.0",
                lastRelease: "0.1.0",
                expected: "0.2.0"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_NewAssembly_AfterPreviousSecondBuild_IsNewAssembly()
        {
            //Release 0.1 (u1) -> Release 0.2 = 0.2

            Test(
                assembly: "0.2.0",
                lastBuild: "0.1.0-build.1",
                lastRelease: "0.1.0-build.1",
                expected: "0.2.0"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_SameAssembly_AfterReleaseSecondBuild_IsFirstPreview()
        {
            //Release 0.1 (u1) -> Commit (p1) = 0.1.1-preview.1

            Test(
                assembly: "0.1.0",
                lastBuild: "0.1.0-build.1",
                lastRelease: "0.1.0-build.1",
                expected: "0.1.1-preview.1"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_FirstBuild()
        {
            //First Build

            Test(
                assembly: "0.1.0",
                lastBuild: null,
                lastRelease: null,
                expected: "0.1.0"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_SecondBuild()
        {
            //First Release, Second Build

            Test(
                assembly: "0.1.0",
                lastBuild: "0.1.0",
                lastRelease: null,
                expected: "0.1.0-build.1"
            );
        }

        [TestMethod]
        public void GetAppveyorVersion_IgnoreDeployedPreview()
        {
            //If you erroneously attempted to deploy a preview version, it will have failed, so we need
            //to make sure we ignore it while checking our last deployed version

            Test(
                assembly: "0.1.0",
                lastBuild: "0.1.0",
                lastRelease: "0.1.0-preview.1",
                expected: "0.1.0-build.1"
            );
        }

        private void Test(string assembly, string lastBuild, string lastRelease, string expected)
        {
            Test((
                Lazy<GetAppveyorVersionService> getAppveyorVersion,
                MockGetVersionService getVersionService,
                MockAppveyorClient appveyorClient,
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell) =>
            {
                //GetVersionTests.MockVersion(fileSystem, powerShell, false);
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;

                envProvider.SetValue(WellKnownEnvironmentVariable.Appveyor, string.Empty);
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorAPIToken, "token");
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorBuildNumber, "1");
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorAccountName, "lordmilko");
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorProjectSlug, "PrtgAPI");

                appveyorClient.LastBuild = lastBuild;
                appveyorClient.LastRelease = lastRelease;
                getVersionService.FileVersion = Version.Parse(assembly);

                var version = getAppveyorVersion.Value.GetVersion(false);

                Assert.AreEqual(expected, version);
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(EnvironmentService),
                typeof(Logger),

                { typeof(GetAppveyorVersionService), typeof(MockAppveyorGetVersionService) },
                { typeof(GetVersionService), typeof(MockGetVersionService) },

                { typeof(IConsoleLogger), typeof(MockConsoleLogger) },
                { typeof(IFileLogger), typeof(MockFileLogger) },

                { typeof(IAppveyorClient), typeof(MockAppveyorClient) },
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IFileSystemProvider), typeof(MockFileSystemProvider) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IProcessService), typeof(MockProcessService) },
                { typeof(IWebClient), typeof(MockWebClient) },

                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>(), p.GetService<IPowerShellService>())
            };
        }
    }
}
