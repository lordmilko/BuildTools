using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorBeforeTest")]
    public class InvokeAppveyorBeforeTest : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
            var service = GetService<NewAppveyorPackageService>();

            service.Execute(Configuration, IsLegacyMode);
        }
    }
}
