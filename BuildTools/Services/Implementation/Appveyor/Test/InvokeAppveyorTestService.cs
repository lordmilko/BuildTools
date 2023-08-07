using System.Text;
using BuildTools.PowerShell;

namespace BuildTools
{
    public class InvokeAppveyorTestService : IAppveyorService
    {
        private readonly InvokeTestService invokeTestService;
        private readonly IProjectConfigProvider configProvider;
        private readonly EnvironmentService environmentService;
        private readonly IPowerShellService powerShell;
        private readonly IProcessService processService;
        private readonly Logger logger;

        internal InvokeAppveyorTestService(InvokeTestService invokeTestService, IProjectConfigProvider configProvider, EnvironmentService environmentService, IPowerShellService powerShell, IProcessService processService, Logger logger)
        {
            this.invokeTestService = invokeTestService;
            this.configProvider = configProvider;
            this.environmentService = environmentService;
            this.powerShell = powerShell;
            this.processService = processService;
            this.logger = logger;
        }

        public void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            logger.LogHeader("Executing tests");

            ProcessPowerShell(isLegacy);
            ProcessCSharp(configuration, isLegacy);
        }

        private void ProcessPowerShell(bool isLegacy)
        {
            if (powerShell.Edition == PSEdition.Desktop && !environmentService.IsAppveyor && !isLegacy)
            {
                logger.LogInformation("Executing PowerShell tests under PowerShell Core");

                var project = configProvider.GetUnitTestProject(false);
                var directory = configProvider.GetTestPowerShellDirectory(project);

                processService.Execute("pwsh", $"-Command \"Invoke-Pester '{directory}' -EnableExit\"", writeHost: true);
            }
            else
            {
                var project = configProvider.GetUnitTestProject(true);
                var result = invokeTestService.InvokeCIPowerShellTest(project, default);

                if (environmentService.IsAppveyor)
                {
                    foreach (var test in result.TestResult)
                    {
                        var args = new[]
                        {
                            $"-Name {GetPesterTestName(test)}",
                            "-Framework = 'Pester'",
                            $"-Filename = '{test.Describe}.Tests.ps1'",
                            $"-Outcome = {test.Result}",
                            $"-ErrorMessage = '{test.FailureMessage}'",
                            $"-Duration = {test.Time.TotalMilliseconds}"
                        };

                        powerShell.InvokeWithArgs("Add-AppveyorTest", args);
                    }
                }
            }
        }

        private void ProcessCSharp(BuildConfiguration configuration, bool isLegacy)
        {
            var config = new InvokeTestConfig(configProvider.Config.TestTypes)
            {
                Configuration = configuration
            };

            if (isLegacy)
            {
                var args = new ArgList
                {
                    "/TestCaseFilter:TestCategory!=SkipCI"
                };

                if (environmentService.IsAppveyor)
                    args.Add("/logger:Appveyor");

                invokeTestService.InvokeCICSharpTest(config, args, true);
            }
            else
            {
                var args = new ArgList
                {
                    "--filter",
                    "TestCategory!=SkipCI"
                };

                //.NET Core is not currently supported https://github.com/appveyor/ci/issues/2212

                //if (environmentService.IsAppveyor)
                //    args.Add("--logger:Appveyor")

                invokeTestService.InvokeCICSharpTest(config, args, false);
            }
        }

        private string GetPesterTestName(PesterTestResult test)
        {
            var builder = new StringBuilder(test.Describe);

            if (!string.IsNullOrEmpty(test.Context))
                builder.AppendFormat(": {0}", test.Context);

            builder.AppendFormat(": {0}", test.Name);

            return builder.ToString();
        }
    }
}
