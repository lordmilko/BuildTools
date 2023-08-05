using System;

namespace BuildTools.Tests
{
    class MockAppveyorClient : IAppveyorClient, IMock<IAppveyorClient>
    {
        public string LastBuild { get; set; }

        public string LastRelease { get; set; }

        public AppveyorProjectDeployment[] GetAppveyorDeployments()
        {
            //GetLastAppveyorNuGetVersion
            return new[]
            {
                new AppveyorProjectDeployment
                {
                    Environment = new AppveyorProjectDeploymentEnvironment
                    {
                        Provider = "NuGet"
                    },

                    Build = new AppveyorProjectDeploymentBuild
                    {
                        Version = LastRelease
                    }
                }
            };
        }

        public AppveyorProjectHistoryBuild[] GetBuildHistory()
        {
            //GetLastAppveyorBuild
            return new[]
            {
                new AppveyorProjectHistoryBuild
                {
                    Version = LastBuild
                }
            };
        }

        public void ResetBuildVersion()
        {
            throw new NotImplementedException();
        }
    }
}
