using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Start, "Module")]
    [BuildCommand(CommandKind.LaunchModule, CommandCategory.Utility)]
    public abstract class StartModule<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        [Parameter(Mandatory = false)]
        public string TargetFramework { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, CommandService commandService)
        {
            help.Synopsis = $"Starts a new PowerShell console containing the compiled version of {project.Name}.";
            help.Description = $@"
The {help.Command} cmdlet starts starts a previously compiled version of {project.Name} in a new PowerShell console. By default, {help.Command} will attempt to launch the last Debug build of {project.Name}. Builds for .NET Core and .NET Standard will be launched in PowerShell Core, while builds for the .NET Framework will be launched in Windows PowerShell. If builds for multiple target frameworks are detected, {help.Command} will throw an execption specifying the builds that were found. A specific target framework can be specified to the -TargetFramework parameter.

If -Legacy is true, {help.Command} will skip enumerating target frameworks and instead attempt to open a build from the legacy .NET Framework version of {project.Name} in a Windows PowerShell console.
";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Configuration), "Build configuration to launch. If no configuration is specified, Debug will be used."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, $"Specifies whether to launch {project.Name} compiled using the .NET Core project or the legacy .NET Framework project."),
                new HelpParameter(nameof(TargetFramework), "Specifies the target framework to launch when multiple frameworks have been compiled.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, "Open a PowerShell console containing the only target framework that has been compiled"),
                new HelpExample($"{help.Command} -TargetFramework net461", $"Open a PowerShell console containing the version of {project.Name} compiled for the target framework 'net461'")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.InvokeBuild)
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new System.NotImplementedException();
        }

        public string[] GetLegacyParameterSets()
        {
            throw new System.NotImplementedException();
        }
    }
}