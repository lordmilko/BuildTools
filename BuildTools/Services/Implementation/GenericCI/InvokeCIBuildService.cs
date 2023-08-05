﻿namespace BuildTools
{
    public class InvokeCIBuildService : ICIService
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly InvokeBuildService invokeBuildService;
        private readonly Logger logger;

        internal InvokeCIBuildService(
            IProjectConfigProvider configProvider,
            InvokeBuildService invokeBuildService,
            Logger logger)
        {
            this.configProvider = configProvider;
            this.invokeBuildService = invokeBuildService;
            this.logger = logger;
        }

        public void Execute(BuildConfiguration configuration)
        {
            logger.LogHeader($"Building {configProvider.Config.Name}");

            invokeBuildService.Build(new BuildConfig
            {
                Configuration = configuration
            }, false);
        }
    }
}
