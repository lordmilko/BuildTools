using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "GitStatus")]
    [BuildCommand(CommandKind.GitStatus, CommandCategory.Utility)]
    public abstract class GetGitStatus<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}