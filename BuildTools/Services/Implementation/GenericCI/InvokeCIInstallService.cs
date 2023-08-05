namespace BuildTools
{
    public class InvokeCIInstallService : ICIService
    {
        private readonly DependencyProvider dependencyProvider;

        internal InvokeCIInstallService(DependencyProvider dependencyProvider)
        {
            this.dependencyProvider = dependencyProvider;
        }

        public void Execute(BuildConfiguration configuration) =>
            dependencyProvider.Install(WellKnownDependency.Pester, logSkipped: true);
    }
}