using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "Version")]
    [BuildCommand(CommandKind.SetVersion, CommandCategory.Version)]
    public abstract class SetVersion<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}