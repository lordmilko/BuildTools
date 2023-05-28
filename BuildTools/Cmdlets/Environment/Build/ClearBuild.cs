using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Clear, "Build")]
    [BuildCommand(CommandKind.ClearBuild, CommandCategory.Build)]
    public abstract class ClearBuild<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}