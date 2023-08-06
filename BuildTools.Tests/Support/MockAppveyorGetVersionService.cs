using System;

namespace BuildTools.Tests
{
    class MockAppveyorGetVersionService : GetAppveyorVersionService
    {
        MockEnvironmentVariableProvider envProvider;

        public MockAppveyorGetVersionService(
            IEnvironmentVariableProvider envProvider,
            IAppveyorClient client,
            EnvironmentService environmentService,
            GetVersionService getVersionService,
            Logger logger) : base(client, environmentService, getVersionService, logger)
        {
            this.envProvider = (MockEnvironmentVariableProvider) envProvider;
        }

        protected override string GetLastAppveyorBuild() => WithAppveyor(() => base.GetLastAppveyorBuild());

        protected override string GetLastAppveyorNuGetVersion() => WithAppveyor(() => base.GetLastAppveyorNuGetVersion());

        private string WithAppveyor(Func<string> action)
        {
            //When we're not running in Appveyor, we won't normally try and make an API request to get version info
            //As such, we need to mock having Appveyor active to ensure this API request gets made

            var original = envProvider.GetValue(WellKnownEnvironmentVariable.Appveyor);

            try
            {
                envProvider.SetValue(WellKnownEnvironmentVariable.Appveyor, "1");

                return action();
            }
            finally
            {
                envProvider.SetValue(WellKnownEnvironmentVariable.Appveyor, original);
            }
        }
    }
}
