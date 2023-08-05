namespace BuildTools
{
    public class InvokeAppveyorInstallService : AppveyorService
    {
        private readonly DependencyProvider dependencyProvider;

        internal InvokeAppveyorInstallService(
            DependencyProvider dependencyProvider,
            IProjectConfigProvider configProvider,
            Logger logger) : base(configProvider, logger)
        {
            this.dependencyProvider = dependencyProvider;
        }

        public override void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            logger.LogHeader("Installing build dependencies");

            var dependencies = dependencyProvider.GetDependencies();

            foreach (var dependency in dependencies)
                dependencyProvider.Install(dependency, logSkipped: true);
        }
    }
}
