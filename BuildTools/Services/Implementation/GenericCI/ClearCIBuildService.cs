namespace BuildTools
{
    public class ClearCIBuildService : ICIService
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly ClearBuildService clearBuildService;

        internal ClearCIBuildService(IProjectConfigProvider configProvider, ClearBuildService clearBuildService)
        {
            this.configProvider = configProvider;
            this.clearBuildService = clearBuildService;
        }


        public void Execute(BuildConfiguration configuration)
        {
            if (!configProvider.HasFeature(Feature.Build))
                return;

            clearBuildService.ClearMSBuild(configuration, false);
        }
    }
}
