using System;
using System.Linq;
using BuildTools.PowerShell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTools.Tests
{
    [TestClass]
    public class AppveyorClientTests : BaseTest
    {
        [TestMethod]
        public void AppveyorClient_GetDeployments()
        {
            Test(client =>
            {
                var deployments = client.GetAppveyorDeployments();

                Assert.AreEqual(1, deployments.Length);

                var deployment = deployments.Single();
                Assert.AreEqual(new DateTime(635435175708776088), deployment.Started);
            });
        }

        [TestMethod]
        public void AppveyorClient_GetHistory()
        {
            Test(client =>
            {
                var history = client.GetBuildHistory();

                Assert.AreEqual(2, history.Length);
                Assert.AreEqual("1.0.5", history[0].Version);
                Assert.AreEqual("1.0.3", history[1].Version);
            });
        }

        private void Test(Action<IAppveyorClient> action)
        {
            Test((
                IAppveyorClient client,
                MockEnvironmentVariableProvider envProvider) =>
            {
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorAPIToken, "token");
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorAccountName, "lordmilko");
                envProvider.SetValue(WellKnownEnvironmentVariable.AppveyorProjectSlug, "PrtgAPI");

                action(client);
            });
        }

        protected override void CreateServices(out ServiceCollection serviceCollection)
        {
            serviceCollection = new ServiceCollection
            {
                typeof(EnvironmentService),
                {  typeof(IAppveyorClient), typeof(AppveyorClient) },
                { typeof(IEnvironmentVariableProvider), typeof(MockEnvironmentVariableProvider) },
                { typeof(IPowerShellService), typeof(MockPowerShellService) },
                { typeof(IWebClient), typeof(MockWebClient) }
            };
        }
    }
}
