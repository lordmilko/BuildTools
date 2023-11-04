using System.IO;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Alias("New-BuildManifest")]
    [Cmdlet(VerbsCommon.New, "BuildEnvironment")]
    public class NewBuildEnvironment : GlobalBuildCmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Path { get; set; } = ".";

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecordEx()
        {
            var service = GetService<NewBuildEnvironmentService>();

            foreach (var item in service.Execute(Path, Force, DefaultConfigSettingValueProvider.Instance))
                WriteObject(new FileInfo(item));
        }
    }
}
