namespace BuildTools
{
    public class InvokeAppveyorBuildService : AppveyorService
    {
        private readonly EnvironmentService environmentService;
        private readonly InvokeBuildService invokeBuildService;

        internal InvokeAppveyorBuildService(
            EnvironmentService environmentService,
            InvokeBuildService invokeBuildService,
            IProjectConfigProvider configProvider,
            Logger logger) : base(configProvider, logger)
        {
            this.environmentService = environmentService;
            this.invokeBuildService = invokeBuildService;
        }

        public override void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            LogHeader($"Building {configProvider.Config.Name}", isLegacy);

            var additionalArgs = new ArgList();

            //.NET Core is not currently supported https://github.com/appveyor/ci/issues/2212
            if (environmentService.IsAppveyor && isLegacy)
                additionalArgs.Add("/logger:\"C:\\Program Files\\AppVeyor\\BuildAgent\\Appveyor.MSBuildLogger.dll\"");

            if (!isLegacy)
                additionalArgs.Add("--no-restore");

            invokeBuildService.Build(new BuildConfig
            {
                ArgumentList = additionalArgs,
                Configuration = configuration,
                SourceLink = true
            }, isLegacy);
        }
    }
}
