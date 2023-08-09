using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsData.Update, "BuildManifest")]
    public class UpdateBuildManifest : GlobalBuildCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Path { get; set; }

        protected override void ProcessRecordEx()
        {
            var service = GetService<UpdateBuildManifestService>();

            service.Execute(Path);
        }
    }
}
