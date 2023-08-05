namespace BuildTools
{
    public class NewAppveyorPackageService : IAppveyorService
    {
        private readonly AppveyorCSharpPackageProvider appveyorCSharpPackageProvider;
        private readonly AppveyorPowerShellPackageProvider appveyorPowerShellPackageProvider;

        private readonly IProjectConfigProvider configProvider;

        internal NewAppveyorPackageService(
            AppveyorCSharpPackageProvider appveyorCSharpPackageProvider,
            AppveyorPowerShellPackageProvider appveyorPowerShellPackageProvider,
            IProjectConfigProvider configProvider)
        {
            this.appveyorCSharpPackageProvider = appveyorCSharpPackageProvider;
            this.appveyorPowerShellPackageProvider = appveyorPowerShellPackageProvider;
            this.configProvider = configProvider;
        }

        public void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            var config = new PackageConfig(configuration, isLegacy, configProvider.Config.PowerShellMultiTargeted, configProvider.Config.PackageTypes);

            if (config.Target.CSharp)
                appveyorCSharpPackageProvider.Execute(config);

            if (config.Target.PowerShell || config.Target.Redist)
                appveyorPowerShellPackageProvider.Execute(config);
        }
    }
}
