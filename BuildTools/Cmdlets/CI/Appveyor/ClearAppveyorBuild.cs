using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsCommon.Clear, "AppveyorBuild")]
    public class ClearAppveyorBuild : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
            LogHeader("Cleaning Appveyor build folder");

            var service = GetService<ClearBuildService>();

            service.ClearMSBuild(Configuration, IsLegacyMode);
        }
    }
}
