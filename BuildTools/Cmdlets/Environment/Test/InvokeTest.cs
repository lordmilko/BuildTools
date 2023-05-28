using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "Test")]
    [BuildCommand(CommandKind.InvokeTest, CommandCategory.Test)]
    public abstract class InvokeTest<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}