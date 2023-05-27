using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    public abstract class BuildCmdlet<TEnvironment> : PSCmdlet
    {
        protected sealed override void ProcessRecord()
        {
            var powerShell = (PowerShellService) GetService<IPowerShellService>();

            powerShell.Push(this);

            try
            {
                ProcessRecordEx();
            }
            finally
            {
                powerShell.Pop();
            }
        }

        protected abstract void ProcessRecordEx();

        protected virtual T GetService<T>()
        {
            return BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<T>();
        }
    }
}
