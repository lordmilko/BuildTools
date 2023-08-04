namespace BuildTools
{
    class NewAppveyorPackageService
    {
        private readonly AppveyorCSharpPackageProvider appveyorCSharpPackageProvider;
        private readonly AppveyorPowerShellPackageProvider appveyorPowerShellPackageProvider;

        private readonly IProjectConfigProvider configProvider;

        public NewAppveyorPackageService(
            AppveyorCSharpPackageProvider appveyorCSharpPackageProvider,
            AppveyorPowerShellPackageProvider appveyorPowerShellPackageProvider,
            IProjectConfigProvider configProvider)
        {
            this.appveyorCSharpPackageProvider = appveyorCSharpPackageProvider;
            this.appveyorPowerShellPackageProvider = appveyorPowerShellPackageProvider;
            this.configProvider = configProvider;
        }

        public void Execute(BuildConfiguration buildConfiguration, bool isLegacy)
        {
            var config = new PackageConfig(buildConfiguration, isLegacy, configProvider.Config.PowerShellMultiTargeted, configProvider.Config.PackageTypes);

            if (config.Target.CSharp)
                appveyorCSharpPackageProvider.Execute(config);

            if (config.Target.PowerShell || config.Target.Redist)
                appveyorPowerShellPackageProvider.Execute(config);
        }
    }
}
