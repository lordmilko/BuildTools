using System;
using System.Runtime.Serialization;

namespace BuildTools
{
    [DataContract]
    public class AppveyorProjectDeployment
    {
        [DataMember(Name = "deploymentId")]
        public int DeploymentId { get; set; }

        [DataMember(Name = "build")]
        public AppveyorProjectDeploymentBuild Build { get; set; }

        [DataMember(Name = "environment")]
        public AppveyorProjectDeploymentEnvironment Environment { get; set; }

        [DataMember(Name = "status")]
        public string Status { get; set; }

        [DataMember(Name = "started")]
        public DateTime Started { get; set; }
    }
}
