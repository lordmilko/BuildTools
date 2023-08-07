using System;
using System.Collections;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class GetCoverageTests : BaseTest
    {
        private const string ChocolateyInstall = "C:\\ProgramData\\chocolatey";
        private const string ChocolateyExe = ChocolateyInstall + "\\bin\\chocolatey.exe";

        private const string OpenCoverCommand = "opencover.console";
        private const string OpenCoverExe = ChocolateyInstall + "\\bin\\opencover.console.exe";

        private static readonly string OpenCoverOutput = Path.Combine(Path.GetTempPath(), "opencover.xml");
        private static readonly string TestAdapters = Path.Combine(Path.GetDirectoryName(typeof(GetCoverageTests).Assembly.Location), "TestAdapters");
        private static readonly string PowerShellTestAdapter = Path.Combine(TestAdapters, "PowerShell.TestAdapter.dll");

        #region CSharp

        [TestMethod]
        public void GetCoverage_CSharp_Normal()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy <GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell, false);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] {TestType.CSharp}
                    },
                    false
                );

                Assert.AreEqual(2, process.Executed.Count);

                //We said it doesn't exist
                Assert.AreEqual("choco install opencover.portable --limitoutput --no-progress -y", process.Executed[0]);

                Assert.AreEqual(
                    "C:\\opencover.console.exe \"-target:C:\\dotnet.exe\" \"-targetargs:test --filter TestCategory!=SkipCoverage&TestCategory!=SkipCI " +
                    $"\"C:\\Root\\UnitTestProj\\PrtgAPIv17.Tests.csproj\" --verbosity:n --no-build -c Debug\" -output:\"{OpenCoverOutput}\" " +
                    "-filter:+\"[PrtgAPI*]* -[*Tests*]*\" -excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute -hideskipped:attribute " +
                    "-register -mergeoutput -oldstyle",
                    process.Executed[1]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_CSharp_Normal_TestOnly()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.CSharp },
                        TestOnly = true
                    },
                    false
                );

                Assert.AreEqual(
                    process.Executed[0],
                    "C:\\dotnet.exe test --filter TestCategory!=SkipCoverage&TestCategory!=SkipCI \"C:\\Root\\UnitTestProj\\PrtgAPIv17.Tests.csproj\" --verbosity:n --no-build -c Debug"
                );
            });
        }

        [TestMethod]
        public void GetCoverage_CSharp_Legacy()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.CSharp }
                    },
                    true
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\vstest.console.exe\" " +
                    "\"-targetargs:/TestCaseFilter:TestCategory!=SkipCoverage&TestCategory!=SkipCI \\\"C:\\Root\\UnitTestProj\\bin\\Debug\\PrtgAPI.Tests.dll\\\"\" " +
                    $"-output:\"{OpenCoverOutput}\" -filter:+\"[PrtgAPI*]* -[*Tests*]*\" " +
                    "-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute -hideskipped:attribute -register:path32 -mergeoutput",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_CSharp_TestOnly()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.CSharp },
                        TestOnly = true
                    },
                    true
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\vstest.console.exe /TestCaseFilter:TestCategory!=SkipCoverage&TestCategory!=SkipCI \\\"C:\\Root\\UnitTestProj\\bin\\Debug\\PrtgAPI.Tests.dll\\\"",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_CSharp_Normal_Filter()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.CSharp },
                        Name = "*foo*"
                    },
                    false
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\dotnet.exe\" \"-targetargs:test --filter TestCategory!=SkipCoverage&TestCategory!=SkipCI&FullyQualifiedName~foo " +
                    $"\"C:\\Root\\UnitTestProj\\PrtgAPIv17.Tests.csproj\" --verbosity:n --no-build -c Debug\" -output:\"{OpenCoverOutput}\" " +
                    "-filter:+\"[PrtgAPI*]* -[*Tests*]*\" -excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute -hideskipped:attribute " +
                    "-register -mergeoutput -oldstyle",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_CSharp_Legacy_Filter()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.CSharp },
                        Name = "*foo*"
                    },
                    true
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\vstest.console.exe\" " +
                    "\"-targetargs:/TestCaseFilter:TestCategory!=SkipCoverage&TestCategory!=SkipCI&FullyQualifiedName~foo \\\"C:\\Root\\UnitTestProj\\bin\\Debug\\PrtgAPI.Tests.dll\\\"\" " +
                    $"-output:\"{OpenCoverOutput}\" -filter:+\"[PrtgAPI*]* -[*Tests*]*\" " +
                    "-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute -hideskipped:attribute -register:path32 -mergeoutput",
                    process.Executed[0]
                );
            });
        }

        #endregion
        #region PowerShell

        [TestMethod]
        public void GetCoverage_PowerShell_Normal()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.PowerShell }
                    },
                    false
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\vstest.console.exe\" " +
                    $"\"-targetargs:\"C:\\First.Tests.ps1\" \"C:\\Second.Tests.ps1\" /TestAdapterPath:\"{TestAdapters}\"\" " +
                    $"-output:\"{OpenCoverOutput}\" -filter:+\"[PrtgAPI*]* -[*Tests*]*\" " +
                    $"-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute " +
                    $"-hideskipped:attribute -register",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_Normal_TestOnly()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.PowerShell },
                        TestOnly = true
                    },
                    false
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    $"C:\\vstest.console.exe \"C:\\First.Tests.ps1\" \"C:\\Second.Tests.ps1\" /TestAdapterPath:\"{TestAdapters}\"",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_Legacy()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.PowerShell }
                    },
                    true
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\vstest.console.exe\" " +
                    $"\"-targetargs:\"C:\\First.Tests.ps1\" \"C:\\Second.Tests.ps1\" /TestAdapterPath:\"{TestAdapters}\"\" " +
                    $"-output:\"{OpenCoverOutput}\" -filter:+\"[PrtgAPI*]* -[*Tests*]*\" " +
                    $"-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute " +
                    $"-hideskipped:attribute -register:path32",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_TestOnly()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.PowerShell },
                        TestOnly = true
                    },
                    true
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    $"C:\\vstest.console.exe \"C:\\First.Tests.ps1\" \"C:\\Second.Tests.ps1\" /TestAdapterPath:\"{TestAdapters}\"",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_Normal_Filter()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.PowerShell },
                        Name = "*first*"
                    },
                    false
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\vstest.console.exe\" " +
                    $"\"-targetargs:\"C:\\First.Tests.ps1\" /TestAdapterPath:\"{TestAdapters}\"\" " +
                    $"-output:\"{OpenCoverOutput}\" -filter:+\"[PrtgAPI*]* -[*Tests*]*\" " +
                    $"-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute " +
                    $"-hideskipped:attribute -register",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_Legacy_Filter()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.PowerShell },
                        Name = "*first*"
                    },
                    true
                );

                Assert.AreEqual(1, process.Executed.Count);

                Assert.AreEqual(
                    "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\vstest.console.exe\" " +
                    $"\"-targetargs:\"C:\\First.Tests.ps1\" /TestAdapterPath:\"{TestAdapters}\"\" " +
                    $"-output:\"{OpenCoverOutput}\" -filter:+\"[PrtgAPI*]* -[*Tests*]*\" " +
                    $"-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute " +
                    $"-hideskipped:attribute -register:path32",
                    process.Executed[0]
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_Filter_NoMatch()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                Setup(fileSystem, envProvider, powerShell);

                getService.Value.GetCoverage(
                    new CoverageConfig(null)
                    {
                        Type = new[] { TestType.PowerShell },
                        Name = "*foo*"
                    },
                    false
                );

                Assert.AreEqual(0, process.Executed.Count);
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_Normal_NetFrameworkNotBuilt()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                PreSetup(fileSystem, envProvider, powerShell);
                SetupTest(fileSystem, envProvider, powerShell);
                SetupPowerShell(fileSystem, setOutput: false);
                fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj"] = true;
                fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj\\bin"] = true;
                fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj\\bin\\Debug"] = true;
                fileSystem.EnumerateDirectoriesMap[("C:\\Root\\UnitTestProj\\bin\\Debug", "*", SearchOption.TopDirectoryOnly)] = new[]
                {
                    "C:\\Root\\UnitTestProj\\bin\\Debug\\netcoreapp2.1"
                };

                AssertEx.Throws<InvalidOperationException>(
                    () => getService.Value.GetCoverage(
                        new CoverageConfig(null)
                        {
                            Type = new[] { TestType.PowerShell }
                        },
                        false
                    ),
                    "Cannot run PowerShell tests as test project has not been compiled for PowerShell Desktop. Found 'bin\\Debug\\netcoreapp2.1'"
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_Legacy_NotBuilt()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<GetCoverageService> getService) =>
            {
                PreSetup(fileSystem, envProvider, powerShell);
                SetupTest(fileSystem, envProvider, powerShell);
                SetupPowerShell(fileSystem, setOutput: false);
                fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj"] = true;
                fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj\\bin"] = true;
                fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj\\bin\\Debug"] = true;
                fileSystem.FileExistsMap["C:\\Root\\UnitTestProj\\bin\\Debug\\PrtgAPI.PowerShell.dll"] = false;

                AssertEx.Throws<InvalidOperationException>(
                    () => getService.Value.GetCoverage(
                        new CoverageConfig(null)
                        {
                            Type = new[] { TestType.PowerShell }
                        },
                        true
                    ),
                    "PrtgAPI for PowerShell Desktop is required to run PowerShell tests however 'C:\\Root\\UnitTestProj\\bin\\Debug\\PrtgAPI.PowerShell.dll' is missing. Has PrtgAPI been compiled?"
                );
            });
        }

        [TestMethod]
        public void GetCoverage_PowerShell_CustomExclude()
        {
            Test((
                MockFileSystemProvider fileSystem,
                MockEnvironmentVariableProvider envProvider,
                MockPowerShellService powerShell,
                MockProcessService process,
                Lazy<IProjectConfigProvider> configProvider,
                IProjectConfigProviderFactory configProviderFactory,
                Lazy <GetCoverageService> getService) =>
            {
                var runspace = RunspaceFactory.CreateRunspace();
                runspace.Open();
                Runspace.DefaultRunspace = runspace;

                try
                {
                    Setup(fileSystem, envProvider, powerShell);

                    fileSystem.FileExistsMap["C:\\Root\\build\\Config.psd1"] = true;
                    fileSystem.ReadFileTextMap["C:\\Root\\build\\Config.psd1"] = "@{}";
                    powerShell.InvokeScriptMap["@{}"] = new Hashtable
                    {
                        { "Name", "PrtgAPI" },
                        { "SolutionName", "PrtgAPI.sln" },
                        { "CmdletPrefix", "Prtg" },
                        { "PowerShellUnitTestFilter", ScriptBlock.Create("$_.BaseName -ne 'First.Tests'") },
                        { "Copyright", "lordmilko, 2015" }
                    };
                    fileSystem.EnumerateFilesMap[("C:\\Root\\build", "*.sln", SearchOption.TopDirectoryOnly)] = Array.Empty<string>();
                    fileSystem.DirectoryExistsMap["C:\\Root\\build\\src"] = false;
                    var temp = configProviderFactory.CreateProvider("C:\\Root\\build");
                    configProvider.Value.Config.PowerShellUnitTestFilter = temp.Config.PowerShellUnitTestFilter;

                    getService.Value.GetCoverage(
                        new CoverageConfig(null)
                        {
                            Type = new[] { TestType.PowerShell }
                        },
                        false
                    );

                    Assert.AreEqual(1, process.Executed.Count);

                    Assert.AreEqual(
                        "C:\\ProgramData\\chocolatey\\bin\\opencover.console.exe \"-target:C:\\vstest.console.exe\" " +
                        $"\"-targetargs:\"C:\\Second.Tests.ps1\" /TestAdapterPath:\"{TestAdapters}\"\" " +
                        $"-output:\"{OpenCoverOutput}\" -filter:+\"[PrtgAPI*]* -[*Tests*]*\" " +
                        $"-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute " +
                        $"-hideskipped:attribute -register",
                        process.Executed[0]
                    );
                }
                finally
                {
                    Runspace.DefaultRunspace = null;
                    runspace.Dispose();
                }
            });
        }

        #endregion

        private void Setup(
            MockFileSystemProvider fileSystem,
            MockEnvironmentVariableProvider envProvider,
            MockPowerShellService powerShell,
            bool openCoverInstalled = true)
        {
            //Preparation
            PreSetup(fileSystem, envProvider, powerShell, openCoverInstalled);

            //Legacy
            SetupLegacy(fileSystem);

            //PowerShell
            SetupPowerShell(fileSystem);

            //Test
            SetupTest(fileSystem, envProvider, powerShell);
        }

        private void PreSetup(
            MockFileSystemProvider fileSystem,
            MockEnvironmentVariableProvider envProvider,
            MockPowerShellService powerShell,
            bool openCoverInstalled = true)
        {
            fileSystem.EnumerateFilesMap[("C:\\Root", "*.sln", SearchOption.TopDirectoryOnly)] = new[] { "PrtgAPI.sln", "PrtgAPIv17.sln" };
            fileSystem.DirectoryExistsMap["C:\\Root\\src"] = true;
            fileSystem.DirectoryExistsMap[ChocolateyInstall] = true;
            fileSystem.FileExistsMap[ChocolateyExe] = true;
            fileSystem.FileExistsMap[OpenCoverExe] = openCoverInstalled;
            envProvider.SetValue(WellKnownEnvironmentVariable.ChocolateyInstall, null);
            powerShell.KnownCommands[OpenCoverCommand] = openCoverInstalled ? new MockPowerShellCommand("opencover.console") : null;
            powerShell.IsWindows = true;

            if (openCoverInstalled)
                fileSystem.VersionInfoMap[OpenCoverExe] = new Version(5, 0);
        }

        private void SetupLegacy(MockFileSystemProvider fileSystem)
        {
            fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj"] = true;
            fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj\\bin"] = true;
            fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj\\bin\\Debug"] = true;
            fileSystem.FileExistsMap["C:\\Root\\UnitTestProj\\bin\\Debug\\PrtgAPI.Tests.dll"] = true;
        }

        private void SetupPowerShell(MockFileSystemProvider fileSystem, bool setTests = true, bool setOutput = true, bool setTestAdapter = true)
        {
            if (setTests)
            {
                fileSystem.DirectoryExistsMap["C:\\Root\\UnitTestProj\\PowerShell"] = true;
                fileSystem.EnumerateFilesMap[("C:\\Root\\UnitTestProj\\PowerShell", "*.Tests.ps1", SearchOption.AllDirectories)] = new[]
                {
                    "C:\\First.Tests.ps1",
                    "C:\\Second.Tests.ps1",
                };
            }

            if (setOutput)
            {
                fileSystem.EnumerateDirectoriesMap[("C:\\Root\\UnitTestProj\\bin\\Debug", "*", SearchOption.TopDirectoryOnly)] = new[]
                {
                    "C:\\Root\\UnitTestProj\\bin\\Debug\\net461",
                    "C:\\Root\\UnitTestProj\\bin\\Debug\\netcoreapp2.1"
                };
                fileSystem.FileExistsMap["C:\\Root\\UnitTestProj\\bin\\Debug\\net461\\PrtgAPI.PowerShell.dll"] = true;
                fileSystem.FileExistsMap["C:\\Root\\UnitTestProj\\bin\\Debug\\PrtgAPI.PowerShell.dll"] = true;
            }
            
            if (setTestAdapter)
            {
                fileSystem.DirectoryExistsMap[TestAdapters] = true;
                fileSystem.FileExistsMap[PowerShellTestAdapter] = true;
            }
        }

        private void SetupTest(
            MockFileSystemProvider fileSystem,
            MockEnvironmentVariableProvider envProvider,
            MockPowerShellService powerShell)
        {
            fileSystem.FileExistsMap[OpenCoverOutput] = false;
            envProvider.SetValue(WellKnownEnvironmentVariable.CI, string.Empty);
            powerShell.KnownCommands["dotnet"] = new MockPowerShellCommand("dotnet");

            fileSystem.EnumerateFilesMap[("C:\\Root", "*.csproj", SearchOption.AllDirectories)] = new[]
            {
                "C:\\Root\\PrimaryProj\\PrtgAPI.csproj",
                "C:\\Root\\PSProj\\PrtgAPI.PowerShell.csproj",
                "C:\\Root\\UnitTestProj\\PrtgAPI.Tests.csproj",

                "C:\\Root\\PrimaryProj\\PrtgAPIv17.csproj",
                "C:\\Root\\PSProj\\PrtgAPIv17.PowerShell.csproj",
                "C:\\Root\\UnitTestProj\\PrtgAPIv17.Tests.csproj",
            };
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(GetCoverageService),

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

                { typeof(IProjectConfigProviderFactory), typeof(ProjectConfigProviderFactory) },

                p => (IProjectConfigProvider) new ProjectConfigProvider(WellKnownConfig.PrtgAPI, "C:\\Root", p.GetService<IFileSystemProvider>())
            };
        }
    }
}
