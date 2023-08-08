namespace BuildTools
{
    public class InvokeAppveyorBeforeTestService : IAppveyorService
    {
        private readonly NewAppveyorPackageService newAppveyorPackageService;

        public InvokeAppveyorBeforeTestService(NewAppveyorPackageService newAppveyorPackageService)
        {
            this.newAppveyorPackageService = newAppveyorPackageService;
        }

        public void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            newAppveyorPackageService.Execute(configuration, isLegacy);
        }
    }
}
