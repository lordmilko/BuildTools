using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorAfterBuild")]
    public class InvokeAppveyorAfterBuild : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
        }
    }
}
