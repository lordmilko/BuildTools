using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "Package")]
    [BuildCommand(CommandKind.NewPackage, CommandCategory.CI)]
    public abstract class NewPackage<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = false)]
        [ArgumentCompleter(typeof(SupportedPackageTypeCompleter<>))]
        [ValidateSetEx(typeof(SupportedPackageTypeValidator<>))]
        public string[] Type { get; set; }

        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        public static void CreateHelp(HelpConfig help, ProjectConfig project, CommandService commandService)
        {
            help.Synopsis = $"Creates NuGet packages from {project.Name} for distribution";
            help.Description = $@"
The {help.Command} cmdlet generates NuGet packages from {project.Name} for distribution within a NuGet package management system. By default, packages will be built using the last Debug build for both the C# and PowerShell versions of {project.Name} using .NET Core SDK tooling (where applicable). Packages can be built for a specific project type by specifying a value to the -Type parameter. Upon generating a package, a FileInfo object will be emitted to the pipeline indicating the name and path to the generated package.

Unlike packaging done in CI builds, {help.Command} does not verify that the contents of the generated package are correct.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Type), "Type of packages to create. By default C# and PowerShell packages as well as a redistributable zip file are created."),
                new HelpParameter(nameof(Configuration), "Configuration to pack. If no value is specified, the last Debug build will be packed."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, $"Specifies whether to pack the .NET Core version of {project.Name} or the legacy .NET Framework version.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, "Create NuGet packages for both C# and PowerShell"),
                new HelpExample($"{help.Command} -Type PowerShell", "Create NuGet packages for PowerShell only"),
                new HelpExample($"{help.Command} -Configuration Release", "Create Release NuGet packages for both C# and PowerShell")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.InvokeBuild)
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}