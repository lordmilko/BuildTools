using System.Runtime.Serialization;

namespace BuildTools
{
    [DataContract]
    public class AppveyorProjectDeploymentBuild
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
