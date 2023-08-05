namespace BuildTools
{
    public class SimulateCIService : ICIService
    {
        private readonly ClearCIBuildService clearCIBuildService;
        private readonly InvokeCIInstallService invokeCIInstallService;
        private readonly InvokeCIScriptService invokeCIScriptService;

        public SimulateCIService(
            ClearCIBuildService clearCiBuildService,
            InvokeCIInstallService invokeCiInstallService,
            InvokeCIScriptService invokeCiScriptService)
        {
            clearCIBuildService = clearCiBuildService;
            invokeCIInstallService = invokeCiInstallService;
            invokeCIScriptService = invokeCiScriptService;
        }

        public void Execute(BuildConfiguration configuration)
        {
            clearCIBuildService.Execute(configuration);

            invokeCIInstallService.Execute(configuration);
            invokeCIScriptService.Execute(configuration);
        }
    }
}
