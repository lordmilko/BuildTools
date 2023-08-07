using System;
using System.IO;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class InvokeBuildTests : BaseTest
    {
        private const string NETFrameworkReferenceAssemblies = "C:\\Program Files (x86)\\Reference Assemblies\\Microsoft\\Framework\\.NETFramework";
        private const string NETFrameworkReferenceAssemblies452 = NETFrameworkReferenceAssemblies + "\\v4.5.2";
        private const string NETFrameworkReferenceAssemblies461 = NETFrameworkReferenceAssemblies + "\\v4.6.1";

        private const string ChocolateyInstall = "C:\\ProgramData\\chocolatey";
        private const string ChocolateyExe = ChocolateyInstall + "\\bin\\chocolatey.exe";
        private const string NuGetExe = ChocolateyInstall + "\\bin\\nuget.exe";

        [TestMethod]
        public void InvokeBuild_Normal_Debug()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<InvokeBuildService> invokeBuild) =>
            {
                //Preparation
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
                fileSystem.FileExistsMap["C:\\Root\\PrtgAPIv17.sln"] = true;

                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
                powerShell.KnownCommands["dotnet"] = new MockPowerShellCommand("dotnet");

                invokeBuild.Value.Build(new BuildConfig(), false);

                process.AssertExecuted("C:\\dotnet.exe build C:\\Root\\PrtgAPIv17.sln -nologo -c Debug");
            });
        }

        [TestMethod]
        public void InvokeBuild_Normal_Release()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<InvokeBuildService> invokeBuild) =>
            {
                //Preparation
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
                fileSystem.FileExistsMap["C:\\Root\\PrtgAPIv17.sln"] = true;

                fileSystem.DirectoryExistsMap[NETFrameworkReferenceAssemblies] = true;
                fileSystem.DirectoryExistsMap[NETFrameworkReferenceAssemblies452] = true;
                fileSystem.DirectoryExistsMap[NETFrameworkReferenceAssemblies461] = true;

                envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
                powerShell.KnownCommands["dotnet"] = new MockPowerShellCommand("dotnet");
                powerShell.IsWindows = true;

                invokeBuild.Value.Build(new BuildConfig
                {
                    Configuration = BuildConfiguration.Release
                }, false);

                process.AssertExecuted("C:\\dotnet.exe build C:\\Root\\PrtgAPIv17.sln -nologo -c Release -p:EnableSourceLink=true");
            });
        }

        [TestMethod]
        public void InvokeBuild_Legacy()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<InvokeBuildService> invokeBuild) =>
            {
                //Preparation
                fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
                fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
                fileSystem.DirectoryExistsMap[ChocolateyInstall] = true;
                fileSystem.FileExistsMap[ChocolateyExe] = true;
                fileSystem.FileExistsMap[NuGetExe] = true;
                powerShell.KnownCommands["nuget"] = new MockPowerShellCommand("nuget");
                envProvider.SetValue(WellKnownEnvironmentVariable.ChocolateyInstall, null);
                powerShell.IsWindows = true;

                invokeBuild.Value.Build(new BuildConfig(), true);

                process.AssertExecuted("C:\\msbuild.exe C:\\Root\\PrtgAPI.sln /verbosity:minimal /p:Configuration=Debug");
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(InvokeBuildService),

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

                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>(), p.GetService<IPowerShellService>())
            };
        }
    }
}
