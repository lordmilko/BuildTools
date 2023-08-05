namespace BuildTools
{
    public class InvokeAppveyorBeforeBuildService : AppveyorService
    {
        private readonly DependencyProvider dependencyProvider;
        private readonly IProcessService processService;
        private readonly SetAppveyorVersionService setAppveyorVersionService;

        internal InvokeAppveyorBeforeBuildService(
            DependencyProvider dependencyProvider,
            IProcessService processService,
            SetAppveyorVersionService setAppveyorVersionService,
            IProjectConfigProvider configProvider,
            Logger logger) : base(configProvider, logger)
        {
            this.dependencyProvider = dependencyProvider;
            this.processService = processService;
            this.setAppveyorVersionService = setAppveyorVersionService;
        }

        public override void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            LogHeader("Restoring NuGet Packages", isLegacy);
            var solutionPath = configProvider.GetSolutionPath(isLegacy);

            if (isLegacy)
            {
                var args = new ArgList
                {
                    "restore",
                    solutionPath
                };

                var nuget = dependencyProvider.Install(WellKnownDependency.NuGet);

                processService.Execute(nuget.Path, args);
            }
            else
            {
                var args = new ArgList
                {
                    "restore",
                    solutionPath,
                    "-p:EnableSourceLink=true"
                };

                var dotnet = dependencyProvider.Install(WellKnownDependency.Dotnet);

                processService.Execute(dotnet.Path, args);
            }

            setAppveyorVersionService.SetVersion(isLegacy);
        }
    }
}
