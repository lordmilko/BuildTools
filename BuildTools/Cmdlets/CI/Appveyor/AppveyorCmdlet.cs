using BuildTools.Cmdlets.CI;

namespace BuildTools.Cmdlets.Appveyor
{
    public abstract class AppveyorCmdlet<TService> : AppveyorCmdlet where TService : IAppveyorService
    {
        protected sealed override void ProcessRecordEx()
        {
            var service = (IAppveyorService) GetService<TService>();

            service.Execute(Configuration, IsLegacyMode);
        }
    }

    public abstract class AppveyorCmdlet : BaseCICmdlet<AppveyorEnvironment>
    {
        protected override bool IsLegacyMode => BuildToolsSessionState.AppveyorBuildCore ?? base.IsLegacyMode;

        public string[] GetLegacyParameterSets() => null;
    }
}
