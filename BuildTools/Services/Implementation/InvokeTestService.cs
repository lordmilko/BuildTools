using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class InvokeTestService
    {
        private readonly Logger logger;
        private readonly IProjectConfigProvider configProvider;
        private readonly DependencyProvider dependencyProvider;
        private readonly IFileSystemProvider fileSystem;
        private readonly IPowerShellService powerShell;
        private readonly IProcessService processService;
        private readonly IVsProductLocator vsProductLocator;

        public InvokeTestService(
            Logger logger,
            IProjectConfigProvider configProvider,
            DependencyProvider dependencyProvider,
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell,
            IProcessService processService,
            IVsProductLocator vsProductLocator)
        {
            this.logger = logger;
            this.configProvider = configProvider;
            this.dependencyProvider = dependencyProvider;
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;
            this.processService = processService;
            this.vsProductLocator = vsProductLocator;
        }

        public void Execute(InvokeTestConfig invokeTestConfig, bool isLegacy)
        {
            //We will throw if dotnet/vstest.console fails, so execute PowerShell tests first
            InvokePowerShellTest(invokeTestConfig, isLegacy);
            InvokeCSharpTest(invokeTestConfig, isLegacy);
        }

        #region C#

        private void InvokeCSharpTest(InvokeTestConfig invokeTestConfig, bool isLegacy)
        {
            if (!invokeTestConfig.Target.CSharp)
                return;

            var additionalArgs = new ArgList
            {
                GetLoggerArgs(isLegacy),
                GetLoggerFilters(invokeTestConfig.Name, invokeTestConfig.Tags, isLegacy)
            };

            InvokeCICSharpTest(invokeTestConfig, additionalArgs, isLegacy);
        }

        public void InvokeCICSharpTest(InvokeTestConfig invokeTestConfig, ArgList additionalArgs, bool isLegacy)
        {
            logger.LogInformation("\tExecuting C# tests");

            if (isLegacy)
            {
                var project = configProvider.GetTestProject(invokeTestConfig.Integration, isLegacy);
                var dll = configProvider.GetTestDll(invokeTestConfig.Integration, invokeTestConfig.Configuration, isLegacy);
                powerShell.WriteVerbose($"Using DLL '{dll}'");

                fileSystem.WithCurrentDirectory(project.DirectoryName, () => InvokeCICSharpTestFull(dll, additionalArgs));
            }
            else
            {
                var csproj = configProvider.GetTestProject(invokeTestConfig.Integration, isLegacy).FilePath;
                powerShell.WriteVerbose($"Using csproj '{csproj}'");

                InvokeCICSharpTestCore(csproj, invokeTestConfig.Configuration, additionalArgs);
            }
        }

        private void InvokeCICSharpTestFull(string dll, ArgList additionalArgs)
        {
            //We don't need to specify the build configuration, as we simply pass a DLL to vstest.console,
            //which will either be from Debug or Release
            var vsTestArgs = new ArgList
            {
                dll,

                additionalArgs
            };

            var vstest = vsProductLocator.GetVSTest();

            powerShell.WriteVerbose($"Executing command {vstest} {vsTestArgs}");
            processService.Execute(vstest, vsTestArgs, writeHost: true);
        }

        private void InvokeCICSharpTestCore(string csproj, BuildConfiguration configuration, ArgList additionalArgs)
        {
            var dotnetTestArgs = new ArgList
            {
                "test",
                csproj,
                "--nologo",
                "--no-restore",
                "--no-build",
                "--verbosity:n",
                "-c",
                configuration,

                additionalArgs
            };

            var dotnet = dependencyProvider.Install(WellKnownDependency.Dotnet);

            powerShell.WriteVerbose($"Executing command 'dotnet {dotnetTestArgs}'");
            processService.Execute(dotnet.Path, dotnetTestArgs, writeHost: true);
        }

        #endregion
        #region PowerShell

        private void InvokePowerShellTest(InvokeTestConfig invokeTestConfig, bool isLegacy)
        {
            if (!invokeTestConfig.Target.PowerShell)
                return;

            var project = configProvider.GetTestProject(invokeTestConfig.Integration, isLegacy);
            var testResultsDir = Path.Combine(project.DirectoryName, "TestResults");

            if (!fileSystem.DirectoryExists(testResultsDir))
                fileSystem.CreateDirectory(testResultsDir);

            var dateStr = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");

            var outputFile = Path.Combine(testResultsDir, $"{configProvider.Config.Name}_PowerShell_{dateStr}.xml");

            var additionalArgs = new ArgList
            {
                $"-OutputFile '{outputFile}'",
                "-OutputFormat NUnitXml"
            };

            if (invokeTestConfig.Name != null)
                additionalArgs.Add($"-TestName {string.Join(",", invokeTestConfig.Name)}");

            if (invokeTestConfig.Tags != null)
                additionalArgs.Add($"-Tag {string.Join(",", invokeTestConfig.Tags)}");

            InvokeCIPowerShellTest(project, additionalArgs);
        }

        public PesterResult InvokeCIPowerShellTest(BuildProject project, ArgList additionalArgs)
        {
            logger.LogInformation("\tExecuting PowerShell tests");

            var powerShellDir = configProvider.GetTestPowerShellDirectory(project);

            dependencyProvider.Install(WellKnownDependency.Pester);

            return powerShell.InvokePester(powerShellDir, additionalArgs).SingleOrDefault();
        }

        #endregion

        private ArgList GetLoggerArgs(bool isLegacy)
        {
            var dateStr = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff");

            var loggerTarget = $"trx;LogFileName={configProvider.Config.Name}_C#_{dateStr}.trx";

            if (isLegacy)
                return "/logger:$loggerTarget";

            return new ArgList
            {
                "--logger",
                loggerTarget
            };
        }

        private ArgList GetLoggerFilters(string[] name, string[] tags, bool isLegacy)
        {
            var filters = new List<string>();

            if (name != null && name.Length > 0)
                filters.Add(string.Join("|", name.Select(v => $"FullyQualifiedName~{v.Trim('*')}")));

            if (tags != null && tags.Length > 0)
                filters.Add(string.Join("|", tags.Select(v => $"TestCategory={v}")));

            if (filters.Count > 0)
            {
                var filter = filters.Count == 1 ? filters[0] : (string.Join("&", filters.Select(v => $"({v})")));

                if (isLegacy)
                    return $"/TestCaseFilter:{filter}";

                return new ArgList
                {
                    "--filter",
                    filter
                };
            }

            return default;
        }
    }
}
