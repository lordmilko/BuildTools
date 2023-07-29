using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsData.Update, "Version")]
    [BuildCommand(CommandKind.UpdateVersion, CommandCategory.Version)]
    public abstract class UpdateVersion<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            var getVersion = commandService.GetCommand(CommandKind.GetVersion);
            var setVersion = commandService.GetCommand(CommandKind.SetVersion);

            help.Synopsis = $"Increments the version of all components used when building {project.Name}";
            help.Description = $@"
The {help.Command} cmdlet increments the version of {project.Name} by a single build version. The {help.Command} should typically be run when preparing to release a new version. The changes to the {project.Name} repo caused by running the {help.Command} cmdlet are typically commited as the ""release"" of the next {project.Name} version. Once pushed to GitHub, the CI system will mark the build and all future builds as ""release candidates"" until the version is actually released.

If you wish to decrement the build version or change the major, minor or revision version components, you can do so by overwriting the entire version using the {setVersion.Name} cmdlet.

For more information on the version components that may be processed, please see Get-Help {getVersion.Name}";

            help.Parameters = new HelpParameter[]
            {
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, $"Specifies whether to increase the version used when compiling {project.Name} using the .NET Core SDK or the legacy .NET Framework tooling.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Increment the {project.Name} build version by 1.")
            };

            help.RelatedLinks = new[]
            {
                getVersion,
                setVersion
            };
        }

        protected override void ProcessRecordEx()
        {
            var service = GetService<SetVersionService>();

            var result = service.UpdateVersion(IsLegacyMode);

            WriteObject(result);
        }

        public string[] GetLegacyParameterSets() => null;
    }
}
