using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsCommon.Clear, "AppveyorBuild")]
    public class ClearAppveyorBuild : AppveyorCmdlet<ClearAppveyorBuildService>, ILegacyProvider
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorInstall")]
    class InvokeAppveyorInstall : AppveyorCmdlet<InvokeAppveyorInstallService>
    {
    }

    #region Build

    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorBeforeBuild")]
    public class InvokeAppveyorBeforeBuild : AppveyorCmdlet<InvokeAppveyorBeforeBuildService>, ILegacyProvider
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorBuild")]
    public class InvokeAppveyorBuild : AppveyorCmdlet<InvokeAppveyorBuildService>, ILegacyProvider
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorAfterBuild")]
    public class InvokeAppveyorAfterBuild : AppveyorCmdlet<InvokeAppveyorAfterBuildService>, ILegacyProvider
    {
    }

    #endregion
    #region Test

    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorBeforeTest")]
    public class InvokeAppveyorBeforeTest : AppveyorCmdlet<NewAppveyorPackageService>, ILegacyProvider
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorTest")]
    public class InvokeAppveyorTest : AppveyorCmdlet<InvokeAppveyorTestService>, ILegacyProvider
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorAfterTest")]
    public class InvokeAppveyorAfterTest : AppveyorCmdlet<InvokeAppveyorAfterTestService>, ILegacyProvider
    {
    }

    #endregion

    [Cmdlet(VerbsCommon.New, "AppveyorPackage")]
    public class NewAppveyorPackage : AppveyorCmdlet<NewAppveyorPackageService>, ILegacyProvider
    {
    }

    [Alias("Simulate-Appveyor")]
    [Cmdlet(VerbsLifecycle.Invoke, "SimulateAppveyor")]
    public class SimulateAppveyor : AppveyorCmdlet<SimulateAppveyorService>
    {
        [Parameter(Mandatory = false)]
        public override BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;
    }
}
