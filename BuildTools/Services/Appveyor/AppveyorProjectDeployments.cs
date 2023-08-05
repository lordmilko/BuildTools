using System.Runtime.Serialization;

namespace BuildTools
{
    [DataContract]
    public class AppveyorProjectDeployments
    {
        [DataMember(Name = "deployments")]
        public AppveyorProjectDeployment[] Deployments { get; set; }
    }
}
