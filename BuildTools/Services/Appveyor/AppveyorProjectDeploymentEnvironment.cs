using System.Runtime.Serialization;

namespace BuildTools
{
    [DataContract]
    public class AppveyorProjectDeploymentEnvironment
    {
        [DataMember(Name = "name")]
        public string Name { get; set; }

        [DataMember(Name = "provider")]
        public string Provider { get; set; }
    }
}
