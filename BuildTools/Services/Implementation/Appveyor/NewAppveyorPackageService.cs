namespace BuildTools
{
    public class NewAppveyorPackageService : AppveyorService
    {
        private readonly AppveyorCSharpPackageProvider appveyorCSharpPackageProvider;
        private readonly AppveyorPowerShellPackageProvider appveyorPowerShellPackageProvider;

        internal NewAppveyorPackageService(
            AppveyorCSharpPackageProvider appveyorCSharpPackageProvider,
            AppveyorPowerShellPackageProvider appveyorPowerShellPackageProvider,
            IProjectConfigProvider configProvider,
            Logger logger) : base(configProvider, logger)
        {
            this.appveyorCSharpPackageProvider = appveyorCSharpPackageProvider;
            this.appveyorPowerShellPackageProvider = appveyorPowerShellPackageProvider;
        }

        public override void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            if (!configProvider.HasFeature(Feature.Package))
                return;

            LogHeader("Building NuGet Package", isLegacy);

            var config = new PackageConfig(configuration, isLegacy, configProvider.Config.PowerShellMultiTargeted, configProvider.Config.PackageTypes);

            if (config.Target.CSharp)
                appveyorCSharpPackageProvider.Execute(config);

            if (config.Target.PowerShell || config.Target.Redist)
                appveyorPowerShellPackageProvider.Execute(config);
        }
    }
}
