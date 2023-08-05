namespace BuildTools
{
    public class ClearCIBuildService : ICIService
    {
        private readonly ClearBuildService clearBuildService;

        internal ClearCIBuildService(ClearBuildService clearBuildService)
        {
            this.clearBuildService = clearBuildService;
        }

        public void Execute(BuildConfiguration configuration) =>
            clearBuildService.ClearMSBuild(configuration, false);
    }
}
