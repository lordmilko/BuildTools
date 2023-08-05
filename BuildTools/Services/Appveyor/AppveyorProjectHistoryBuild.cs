using System.Runtime.Serialization;

namespace BuildTools
{
    [DataContract]
    class AppveyorProjectHistoryBuild
    {
        [DataMember(Name = "version")]
        public string Version { get; set; }
    }
}
