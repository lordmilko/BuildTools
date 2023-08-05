using System.IO;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "BuildEnvironment")]
    public class NewBuildEnvironment : BuildCmdlet<object>
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Force { get; set; }

        protected override void ProcessRecordEx()
        {
            var service = GetService<NewBuildEnvironmentService>();

            foreach (var item in service.Execute(Path, Force))
                WriteObject(new FileInfo(item));
        }

        protected override T GetService<T>() => BuildToolsSessionState.GlobalServiceProvider.GetService<T>();
    }
}
