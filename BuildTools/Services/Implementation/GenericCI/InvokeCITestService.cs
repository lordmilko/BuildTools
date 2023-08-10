using System;
using System.Linq;

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
            if (!configProvider.HasFeature(Feature.Test))
                return;

            logger.LogHeader("Executing tests");

            if (HasType(TestType.PowerShell))
            {
                var project = configProvider.GetUnitTestProject(false);
                var result = invokeTestService.InvokeCIPowerShellTest(project, default);

                if (result.FailedCount > 0)
                    throw new InvalidOperationException($"{result.FailedCount} Pester tests failed");
            }

            if (HasType(TestType.CSharp))
            {
                var csharpArgs = new ArgList
                {
                    "--filter",
                    "TestCategory!=SkipCI"
                };

                invokeTestService.InvokeCICSharpTest(new InvokeTestConfig(configProvider.Config.TestTypes)
                {
                    Configuration = configuration
                }, csharpArgs, false);
            }
        }

        private bool HasType(TestType type) => configProvider.Config.TestTypes.Contains(type);
    }
}
