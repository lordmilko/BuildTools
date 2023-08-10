using System;
using BuildTools.Cmdlets.CI;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets.Appveyor
{
    public abstract class AppveyorCmdlet<TService> : AppveyorCmdlet where TService : IAppveyorService
    {
        protected sealed override void ProcessRecordEx()
        {
            try
            {
                var service = (IAppveyorService) GetService<TService>();

                service.Execute(Configuration, IsLegacyMode);
            }
            catch (Exception ex)
            {
                var powerShell = GetService<IPowerShellService>();
                powerShell.WriteColor(ex.Message, ConsoleColor.Red);
                powerShell.WriteColor(ex.StackTrace, ConsoleColor.Red);
                throw;
            }
        }
    }

    public abstract class AppveyorCmdlet : BaseCICmdlet<AppveyorEnvironment>
    {
        protected override bool IsLegacyMode => BuildToolsSessionState.AppveyorBuildLegacy ?? base.IsLegacyMode;

        public string[] GetLegacyParameterSets() => null;
    }
}
