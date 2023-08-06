using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Clear, "Build", DefaultParameterSetName = ParameterSet.Default)]
    [BuildCommand(CommandKind.ClearBuild, CommandCategory.Build)]
    public abstract class ClearBuild<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Default)]
        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.Full)]
        public SwitchParameter Full { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Clears the output of one or more previous {project.Name} builds.";
            help.Description = $"The {help.Command} cmdlet clears the output of previous builds of {project.Name}. By default, {help.Command} will attempt to use the appropriate build tool (msbuild or dotnet.exe) to clear the previous build. If -Full is specified, {help.Command} will instead force remove the bin and obj folders of each project in the solution.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Configuration), $"Configuration to clean. If no value is specified {project.Name} will clean the last Debug build."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, "Specifies whether to use legacy .NET tooling to clear the build."),
                new HelpParameter(nameof(Full), "Specifies whether to brute force remove all build and object files in the solution.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Clear the last build of {project.Name}"),
                new HelpExample($"{help.Command} -Full", $"Remove all obj and bin folders under each project of {project.Name}")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.InvokeBuild)
            };
        }

        protected override void ProcessRecordEx()
        {
            var clearService = GetService<ClearBuildService>();

            if (Full)
                clearService.ClearFull();
            else
                clearService.ClearMSBuild(Configuration, IsLegacyMode);
        }

        public string[] GetLegacyParameterSets() => new[] { ParameterSet.Default };
    }
}
