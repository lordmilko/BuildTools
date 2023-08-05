using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorTest")]
    class InvokeAppveyorTest : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
            var invokeAppveyorTest = GetService<InvokeAppveyorTestService>();

            invokeAppveyorTest.Execute(Configuration, IsLegacyMode);
        }
    }
}
