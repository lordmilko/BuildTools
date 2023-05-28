using System;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "Build")]
    [BuildCommand(CommandKind.InvokeBuild, CommandCategory.Build)]
    public abstract class InvokeBuild<TEnvironment> : BuildCmdlet<TEnvironment> //, IDynamicParameters //todo: if we're running on windows, add the -Legacy parameter
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name { get; set; }

        [Alias("Args")]
        [Parameter(Mandatory = false, Position = 1)]
        public string[] ArgumentList { get; set; }

        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        [Alias("Dbg", "DebugMode")]
        [Parameter(Mandatory = false)]
        public SwitchParameter DebugBuild { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter SourceLink { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter ViewLog { get; set; } = true;

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}