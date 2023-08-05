using System.Runtime.Serialization;

namespace BuildTools
{
    [DataContract]
    class AppveyorProjectHistory
    {
        [DataMember(Name = "builds")]
        public AppveyorProjectHistoryBuild[] Builds { get; set; }
    }
}
