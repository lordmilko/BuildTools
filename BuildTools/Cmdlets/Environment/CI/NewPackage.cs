using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Package")]
    [BuildCommand(CommandKind.NewPackage, CommandCategory.CI)]
    public abstract class NewPackage<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}