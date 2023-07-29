using System;
using System.IO;
using System.Linq;
using System.Management.Automation;

namespace BuildTools
{
    public enum CoverageReportType
    {
        Html
    }

    class CoverageConfig
    {
        public string Name { get; set; }

        public TestType[] Type { get; set; }

        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        public bool TestOnly { get; set; }

        public TestTarget Target => new TestTarget(Type);
    }

    class GetCoverageService
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly DependencyProvider dependencyProvider;
        private readonly IFileSystemProvider fileSystem;
        private readonly Logger logger;
        private readonly IProcessService processService;
        private readonly IVsProductLocator vsProductLocator;

        private readonly string openCover;
        private readonly string openCoverOutput;

        public GetCoverageService(
            IProjectConfigProvider configProvider,
            DependencyProvider dependencyProvider,
            IFileSystemProvider fileSystem,
            Logger logger,
            IProcessService processService,
            IVsProductLocator vsProductLocator)
        {
            this.configProvider = configProvider;
            this.dependencyProvider = dependencyProvider;
            this.fileSystem = fileSystem;
            this.logger = logger;
            this.processService = processService;
            this.vsProductLocator = vsProductLocator;

            openCover = dependencyProvider.Install(WellKnownDependency.OpenCover).Path;
            openCoverOutput = Path.Combine(Path.GetTempPath(), "opencover.xml");
        }

        public void GetCoverage(CoverageConfig coverageConfig, bool isLegacy)
        {
            ClearCoverage();

            var unitTestProject = configProvider.GetUnitTestProject(isLegacy);

            GetPowerShellCoverage(coverageConfig, unitTestProject, isLegacy);
            GetCSharpCoverage(coverageConfig, unitTestProject, isLegacy);
        }

        #region C#

        private void GetCSharpCoverage(CoverageConfig coverageConfig, BuildProject unitTestProject, bool isLegacy)
        {
            if (!coverageConfig.Target.CSharp)
                return;

            var testRunner = GetCSharpTestRunner(isLegacy);
            var testParams = GetCSharpTestParams(coverageConfig, isLegacy);

            fileSystem.WithCurrentDirectory(unitTestProject.DirectoryName, () =>
            {
                if (coverageConfig.TestOnly)
                {
                    logger.LogInformation($"\t\tExecuting {testRunner} {testParams}");
                    processService.Execute(testRunner, testParams, writeHost: true);
                }
                else
                {
                    var openCoverParams = GetCSharpOpenCoverParams(testRunner, testParams, isLegacy);

                    logger.LogInformation($"\t\tExecuting '{openCover} {openCoverParams}'");
                    processService.Execute(openCover, openCoverParams, writeHost: true);
                }
            });
        }

        private string GetCSharpTestRunner(bool isLegacy)
        {
            if (isLegacy)
                return vsProductLocator.GetVSTest();

            var dotnet = dependencyProvider.Install(WellKnownDependency.Dotnet);

            return dotnet.Path;
        }

        private ArgList GetCSharpTestParams(CoverageConfig coverageConfig, bool isLegacy)
        {
            var trimmedName = coverageConfig.Name?.Trim('*');

            string nameFilter = null;

            if (!string.IsNullOrEmpty(trimmedName))
                nameFilter = $"&FullyQualifiedName~{trimmedName}";

            var filter = $"TestCategory!=SkipCoverage&TestCategory!=SkipCI{nameFilter}";

            ArgList args;

            if (isLegacy)
            {
                var dll = configProvider.GetUnitTestDll(coverageConfig.Configuration, true);

                args = new ArgList
                {
                    $"/TestCaseFilter:{filter}",
                    $"\\\"{dll}\\\""
                };
            }
            else
            {
                var csproj = configProvider.GetUnitTestProject(isLegacy).FilePath;

                args = new ArgList
                {
                    "test",
                    "--filter",
                    filter,
                    $"\"{csproj}\"",
                    "--verbosity:n",
                    "--no-build",
                    "-c",
                    coverageConfig.Configuration
                };
            }

            if (coverageConfig.TestOnly)
            {
                // Replace the cmd escaped quotes with PowerShell escaped quotes, and then add an additional quote at the end of the TestCaseFilter to separate the arguments.
                // Trim any quotes from the end of the string, since PowerShell will add its own quote for us
                for (var i = 0; i < args.Arguments.Length; i++)
                {
                    args.Arguments[i] = args.Arguments[i].Replace("\\\"", "\" \"").Trim('"', ' ');
                }
            }

            return args;
        }

        private ArgList GetCSharpOpenCoverParams(string testRunner, ArgList testParams, bool isLegacy)
        {
            var openCoverParams = GetCommonOpenCoverParams(testRunner, testParams, isLegacy);

            openCoverParams.Add("-mergeoutput");

            if (!isLegacy)
                openCoverParams.Add("-oldstyle");

            return openCoverParams;
        }

        #endregion
        #region PowerShell

        private void GetPowerShellCoverage(CoverageConfig coverageConfig, BuildProject unitTestProject, bool isLegacy)
        {
            if (!coverageConfig.Target.PowerShell)
                return;

            var tests = GetPowerShellTests(coverageConfig, isLegacy);

            if (tests.Length == 0)
            {
                logger.LogInformation($"\t\tNo tests matched the specified name '{coverageConfig.Name ?? "*"}'; skipping PowerShell coverage");
                return;
            }

            AssertHasPowerShellDll(coverageConfig, unitTestProject, isLegacy);

            var testRunner = vsProductLocator.GetVSTest();
            var testParams = GetPowerShellTestParams(coverageConfig, tests);

            fileSystem.WithCurrentDirectory(
                unitTestProject.DirectoryName,
                () =>
                {
                    if (coverageConfig.TestOnly)
                    {
                        logger.LogInformation($"\t\tExecuting {testRunner} {testParams}");
                        processService.Execute(testRunner, testParams, writeHost: true);
                    }
                    else
                    {
                        var openCoverParams = GetCommonOpenCoverParams(testRunner, testParams, isLegacy);

                        logger.LogInformation($"\t\tExecuting {openCover} {openCoverParams}");
                        processService.Execute(openCover, openCoverParams, writeHost: true);
                    }
                }
            );
        }

        private void AssertHasPowerShellDll(CoverageConfig coverageConfig, BuildProject unitTestProject, bool isLegacy)
        {
            var unitTestBuildDir = configProvider.GetProjectConfigurationDirectory(unitTestProject, coverageConfig.Configuration);

            var powerShellName = $"{configProvider.GetPowerShellProjectName()}.dll";

            if (!isLegacy)
            {
                var candidates = fileSystem.EnumerateDirectories(unitTestBuildDir).ToArray();

                var netFramework = candidates.FirstOrDefault(v => !Path.GetFileName(v).StartsWith("netcore"));

                if (netFramework == null)
                {
                    var projectDir = unitTestProject.DirectoryName;
                    var relativePaths = candidates.Select(v => $"'{v.Substring(projectDir.Length).TrimStart(Path.DirectorySeparatorChar)}'").ToArray();

                    throw new InvalidOperationException($"Cannot run PowerShell tests as test project has not been compiled for PowerShell Desktop. Found {string.Join(", ", relativePaths)}");
                }

                unitTestBuildDir = netFramework;
            }

            var powerShellDll = Path.Combine(unitTestBuildDir, powerShellName);

            if (!fileSystem.FileExists(powerShellDll))
                throw new InvalidOperationException($"{configProvider.Config.Name} for PowerShell Desktop is required to run PowerShell tests however '{powerShellDll}' is missing. Has {configProvider.Config.Name} been compiled?");
        }

        private ArgList GetPowerShellTestParams(CoverageConfig coverageConfig, string[] tests)
        {
            var dir = Path.GetDirectoryName(GetType().Assembly.Location);
            var subDir = Path.Combine(dir, "TestAdapters");

            if (!fileSystem.DirectoryExists(subDir))
                throw new DirectoryNotFoundException($"TestAdapters directory '{subDir}' was not found. Is BuildTools corrupt?");

            var dll = Path.Combine(subDir, "PowerShell.TestAdapter.dll");

            if (!fileSystem.FileExists(dll))
                throw new FileNotFoundException($"PowerShell Test Adapter '{dll}' was not found. Is BuildTools corrupt?", dll);

            var testParams = $"/TestAdapterPath:\"{subDir}\"";

            ArgList args;

            if (coverageConfig.TestOnly)
            {
                args = new ArgList
                {
                    tests.Select(v => v.Replace("\\\"", "\"")).ToArray(),
                    testParams.Replace("\\\"", "\"")
                };

                return args;
            }
            else
            {
                args = new ArgList
                {
                    tests,
                    testParams
                };
            }

            return args;
        }

        private string[] GetPowerShellTests(CoverageConfig coverageConfig, bool isLegacy)
        {
            var project = configProvider.GetUnitTestProject(isLegacy);

            var powerShellDir = configProvider.GetTestPowerShellDirectory(project);

            var tests = fileSystem.EnumerateFiles(powerShellDir, "*.Tests.ps1", SearchOption.AllDirectories);

            if (coverageConfig.Name != null)
            {
                var wildcard = new WildcardPattern(coverageConfig.Name, WildcardOptions.IgnoreCase);

                tests = tests.Where(t => wildcard.IsMatch(t));
            }

            if (configProvider.Config.UnitTestPowerShellFilter != null)
                tests = tests.Where(t => configProvider.Config.UnitTestPowerShellFilter(new FileInfo(t)));

            var results = tests.Select(t => $"\"{t}\"").ToArray();

            if (results.Length == 0)
            {
                if (coverageConfig.Name == null)
                    throw new InvalidOperationException("Couldn't find any PowerShell tests");
            }
            else
                logger.LogInformation($"\t\tFound {results.Length} PowerShell tests");

            return results;
        }

        #endregion
        #region OpenCover

        private void ClearCoverage()
        {
            if (fileSystem.FileExists(openCoverOutput))
                fileSystem.DeleteFile(openCoverOutput);
        }

        private ArgList GetCommonOpenCoverParams(string testRunner, ArgList testParams, bool isLegacy)
        {
            var openCoverParams = new ArgList
            {
                //https://github.com/OpenCover/opencover/wiki/Usage#understanding-filters
                $"\"-target:{testRunner}\"",
                $"\"-targetargs:{testParams}\"",
                $"-output:\"{openCoverOutput}\"",

                //Filter syntax: +[<modulesToInclude>]<classesToinclude> -[<modulesToExclude>]<classesInThoseModulesToExclude>
                $"-filter:+\"[{configProvider.Config.Name}*]* -[*Tests*]*\"",
                "-excludebyattribute:System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute",
                "-hideskipped:attribute",

                isLegacy ? "-register:path32" : "-register"
            };

            return openCoverParams;
        }

        #endregion
        #region Report

        public void CreateReport(CoverageReportType type = CoverageReportType.Html, string targetDir = null)
        {
            targetDir ??= (Path.Combine(Path.GetTempPath(), "report"));

            logger.LogHeader("Generating a coverage report");

            var args = new ArgList
            {
                $"\"-reports:{openCoverOutput}\"",
                $"-reporttypes:{type}",
                $"\"-targetdir:{targetDir}\"",
                "-verbosity:off"
            };

            var reportGenerator = GetReportGeneratorPath();

            logger.LogInformation($"\t\tExecuting '{reportGenerator} {args}'");
            processService.Execute(reportGenerator, args);
        }

        private string GetReportGeneratorPath()
        {
            var reportGenerator = dependencyProvider.Install(WellKnownDependency.ReportGenerator);

            if (reportGenerator.Version >= new Version(4, 3))
            {
                //The chocolatey shim probably points to the .NET Core 3.0 version. Fallback
                //to the .NET Framework version

                var bin = Path.GetDirectoryName(reportGenerator.Path);
                var chocolatey = Path.GetDirectoryName(bin);

                var tools = Path.Combine(chocolatey, "lib", "reportgenerator.portable", "tools");

                if (!fileSystem.DirectoryExists(tools))
                    throw new DirectoryNotFoundException($"Folder '{tools}' does not exist. Unable to locate .NET Framework Report Generator");

                var netfx = fileSystem.EnumerateDirectories(tools, "net4*").FirstOrDefault();

                if (netfx == null)
                    throw new InvalidOperationException("Unable to find a .NET Framework version of Report Generator");

                var exe = Path.Combine(netfx, "reportgenerator.exe");

                if (!fileSystem.FileExists(exe))
                    throw new FileNotFoundException($"Could not find ReportGenerator at path {exe}", exe);

                return exe;
            }

            return reportGenerator.Path;
        }

        #endregion
    }
}
