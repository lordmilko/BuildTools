namespace BuildTools
{
    public class ClearAppveyorBuildService : AppveyorService
    {
        private readonly ClearBuildService clearBuildService;

        internal ClearAppveyorBuildService(
            ClearBuildService clearBuildService,
            IProjectConfigProvider configProvider,
            Logger logger) : base(configProvider, logger)
        {
            this.clearBuildService = clearBuildService;
        }

        public override void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            LogHeader("Cleaning Appveyor build folder", isLegacy);

            clearBuildService.ClearMSBuild(configuration, isLegacy);
        }
    }
}
