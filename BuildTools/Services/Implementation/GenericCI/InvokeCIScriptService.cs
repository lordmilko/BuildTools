namespace BuildTools
{
    public class InvokeCIScriptService : ICIService
    {
        private readonly InvokeCIBuildService invokeCIBuildService;
        private readonly InvokeCITestService invokeCITestService;

        public InvokeCIScriptService(InvokeCIBuildService invokeCiBuildService, InvokeCITestService invokeCiTestService)
        {
            invokeCIBuildService = invokeCiBuildService;
            invokeCITestService = invokeCiTestService;
        }

        public void Execute(BuildConfiguration configuration)
        {
            invokeCIBuildService.Execute(configuration);
            invokeCITestService.Execute(configuration);
        }
    }
}