using BuildTools.PowerShell;

namespace BuildTools
{
    class SetAppveyorVersionService
    {
        private readonly GetAppveyorVersionService getAppveyorVersionService;
        private readonly SetVersionService setVersionService;
        private readonly IPowerShellService powerShell;
        private readonly Logger logger;

        public SetAppveyorVersionService(
            GetAppveyorVersionService getAppveyorVersion,
            SetVersionService setVersionService,
            IPowerShellService powerShell,
            Logger logger)
        {
            this.getAppveyorVersionService = getAppveyorVersion;
            this.setVersionService = setVersionService;
            this.powerShell = powerShell;
            this.logger = logger;
        }

        public void SetVersion(bool isLegacy)
        {
            logger.LogInformation("Calculating version");
            var version = getAppveyorVersionService.GetVersion(isLegacy);

            logger.LogInformation($"Setting AppVeyor build to version '{version}'");

            powerShell.InvokeWithArgs("Update-AppVeyorBuild", $"-Version {version}");

            setVersionService.SetVersion(null, isLegacy, version);
        }
    }
}
