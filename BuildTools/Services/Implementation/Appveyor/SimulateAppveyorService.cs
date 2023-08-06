using System;
using System.Collections.Generic;
using Env = BuildTools.WellKnownEnvironmentVariable;

namespace BuildTools
{
    public class SimulateAppveyorService : IAppveyorService
    {
        private readonly IProjectConfigProvider configProvider;
        private readonly EnvironmentService environmentService;

        private readonly IAppveyorService[] services;

        internal SimulateAppveyorService(
            IProjectConfigProvider configProvider,
            EnvironmentService environmentService,
            ClearAppveyorBuildService clearAppveyorBuildService,
            InvokeAppveyorInstallService invokeAppveyorInstallService,

            InvokeAppveyorBeforeBuildService invokeAppveyorBeforeBuildService,
            InvokeAppveyorBuildService invokeAppveyorBuildService,
            InvokeAppveyorAfterBuildService invokeAppveyorAfterBuildService,
            
            InvokeAppveyorBeforeTestService invokeAppveyorBeforeTestService,
            InvokeAppveyorTestService invokeAppveyorTestService,
            InvokeAppveyorAfterTestService invokeAppveyorAfterTestService)
        {
            this.configProvider = configProvider;
            this.environmentService = environmentService;

            services = new IAppveyorService[]
            {
                clearAppveyorBuildService,
                invokeAppveyorInstallService,

                invokeAppveyorBeforeBuildService,
                invokeAppveyorBuildService,
                invokeAppveyorAfterBuildService,

                invokeAppveyorBeforeTestService,
                invokeAppveyorTestService,
                invokeAppveyorAfterTestService
            };
        }

        public void SimulateEnvironment(Action action, BuildConfiguration configuration)
        {
            var environmentVariables = new Dictionary<string, string>
            {
                { Env.Configuration, configuration.ToString() },
                { Env.AppveyorBuildFolder, configProvider.SolutionRoot },
                { Env.AppveyorBuildNumber, "1" },
                { Env.AppveyorBuildVersion, "1" },
                { Env.AppveyorRepoCommitMessage, "Did some stuff"},
                { Env.AppveyorRepoCommitMessageExtended, "For #4" },
                { Env.AppveyorAccountName, "Fake Account Name" },
                { Env.AppveyorProjectSlug, "Fake PRoject Name" },
            };

            using (var scope = new EnvironmentScope())
            {
                foreach (var item in environmentVariables)
                    scope.SetValue(item.Key, item.Value);

                action();
            }
        }

        public void Execute(BuildConfiguration configuration, bool isLegacy)
        {
            if (environmentService.IsAppveyor)
                throw new InvalidOperationException("Simulate-Appveyor should not be run from within Appveyor");

            SimulateEnvironment(() =>
            {
                foreach (var service in services)
                    service.Execute(configuration, isLegacy);
            }, configuration);
        }
    }
}
