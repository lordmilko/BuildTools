using System;

namespace BuildTools
{
    public class InvokeCITestService : ICIService
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly InvokeTestService invokeTestService;
        private readonly Logger logger;

        internal InvokeCITestService(
            IProjectConfigProvider configProvider,
            InvokeTestService invokeTestService,
            Logger logger)
        {
            this.configProvider = configProvider;
            this.invokeTestService = invokeTestService;
            this.logger = logger;
        }

        public void Execute(BuildConfiguration configuration)
        {
            logger.LogHeader("Executing tests");

            var project = configProvider.GetUnitTestProject(false);
            var result = invokeTestService.InvokeCIPowerShellTest(project, default);

            if (result.FailedCount > 0)
                throw new InvalidOperationException($"{result.FailedCount} Pester tests failed");

            var csharpArgs = new ArgList
            {
                "--filter",
                "TestCategory!=SkipCI"
            };

            invokeTestService.InvokeCICSharpTest(new InvokeTestConfig
            {
                Configuration = configuration
            }, csharpArgs, false);
        }
    }
}
