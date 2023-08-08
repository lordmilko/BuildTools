namespace BuildTools
{
    public class InvokeCIInstallService : ICIService
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly DependencyProvider dependencyProvider;

        internal InvokeCIInstallService(IProjectConfigProvider configProvider, DependencyProvider dependencyProvider)
        {
            this.configProvider = configProvider;
            this.dependencyProvider = dependencyProvider;
        }

        public void Execute(BuildConfiguration configuration)
        {
            if (!configProvider.HasFeature(Feature.Dependency))
                return;

            dependencyProvider.Install(WellKnownDependency.Pester, logSkipped: true);
        }
    }
}
