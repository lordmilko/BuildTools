using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Set, "Version")]
    [BuildCommand(CommandKind.SetVersion, CommandCategory.Version)]
    public abstract class SetVersion<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = true, Position = 0)]
        public Version Version { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Sets the version of all components used when building {project.Name}";
            help.Description = $"The {help.Command} cmdlet updates the version of {project.Name}. The {help.Command} cmdlet allows the major, minor, build and revision components to be replaced with any arbitrary version. Typically the {help.Command} cmdlet is used to revert mistakes made when utilizing the {commandService.GetCommand(CommandKind.UpdateVersion).Name} cmdlet as part of a normal release, or to reset the version when updating the major or minor version components.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Version), $"The version to set {project.Name} to. Must at least include a major and minor version number."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, $"Specifies whether to increase the version used when compiling {project.Name} using the .NET Core SDK or the legacy .NET Framework tooling.")
            };

            help.Examples = new[]
            {
                new HelpExample($"{help.Command} 1.2.3", "Set the version to version 1.2.3.0"),
                new HelpExample($"{help.Command} 1.2.3.4", "Set the version to version 1.2.3.4. Systems that only utilize the first three version components will be versioned as 1.2.3")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetCommand(CommandKind.GetVersion),
                commandService.GetCommand(CommandKind.UpdateVersion)
            };
        }

        protected override void ProcessRecordEx()
        {
            var service = GetService<SetVersionService>();

            var result = service.SetVersion(Version, IsLegacyMode, null);

            WriteObject(result);
        }

        public string[] GetLegacyParameterSets()
        {
            throw new NotImplementedException();
        }
    }
}