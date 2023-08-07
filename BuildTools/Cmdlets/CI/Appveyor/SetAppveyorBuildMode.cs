using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsCommon.Set, "AppveyorBuildMode")]
    public class SetAppveyorBuildMode : AppveyorCmdlet
    {
        [Parameter(Mandatory = true)]
        public SwitchParameter IsLegacy { get; set; }

        protected override void ProcessRecordEx()
        {
            BuildToolsSessionState.AppveyorBuildLegacy = IsLegacy;
        }
    }
}
