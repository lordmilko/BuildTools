using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsCommon.New, "AppveyorPackage")]
    public class NewAppveyorPackage : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
            var service = GetService<NewAppveyorPackageService>();

            service.Execute(Configuration, IsLegacyMode);
        }
    }
}
