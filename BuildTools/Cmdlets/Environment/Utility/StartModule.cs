using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "Module")]
    [BuildCommand(CommandKind.LaunchModule, CommandCategory.Utility)]
    public abstract class StartModule<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new System.NotImplementedException();
        }
    }
}