using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;
using System.Threading.Tasks;

namespace BuildTools.Cmdlets.CI
{
    [Cmdlet(VerbsCommon.Clear, "CIBuild")]
    public class ClearCIBuild : GenericCICmdlet<ClearCIBuildService>
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "CIBuild")]
    public class InvokeCIBuild : GenericCICmdlet<InvokeCIBuildService>
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "CIInstall")]
    public class InvokeCIInstall : GenericCICmdlet<InvokeCIInstallService>
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "CIScript")]
    public class InvokeCIScript : GenericCICmdlet<InvokeCIScriptService>
    {
    }

    [Cmdlet(VerbsLifecycle.Invoke, "CITest")]
    public class InvokeCITest : GenericCICmdlet<InvokeCITestService>
    {
    }
}
