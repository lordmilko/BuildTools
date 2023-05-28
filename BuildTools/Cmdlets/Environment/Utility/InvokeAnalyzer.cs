using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Install, "Analyze")]
    [BuildCommand(CommandKind.InvokePSAnalyzer, CommandCategory.Utility)]
    public abstract class InvokeAnalyzer<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}