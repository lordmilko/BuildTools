using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorAfterTest")]
    public class InvokeAppveyorAfterTest : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
            var service = GetService<MeasureAppveyorCoverageService>();

            service.Execute(Configuration, IsLegacyMode);
        }
    }
}
