using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsData.Update, "Version")]
    [BuildCommand(CommandKind.UpdateVersion, CommandCategory.Version)]
    public abstract class UpdateVersion<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}