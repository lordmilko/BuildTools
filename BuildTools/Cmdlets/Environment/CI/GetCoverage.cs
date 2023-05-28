using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Coverage")]
    [BuildCommand(CommandKind.Coverage, CommandCategory.CI)]
    public abstract class GetCoverage<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}