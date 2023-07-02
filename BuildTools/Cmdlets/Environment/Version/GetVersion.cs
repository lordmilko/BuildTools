using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Version")]
    [BuildCommand(CommandKind.GetVersion, CommandCategory.Version)]
    public abstract class GetVersion<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            var setVersion = commandService.GetCommand(CommandKind.SetVersion);
            var updateVersion = commandService.GetCommand(CommandKind.UpdateVersion);

            help.Synopsis = $"Retrieves version information used by various components of {project.Name}";
            help.Description = $@"
The {help.Command} cmdlet retrieves version details found in various locations in the {project.Name} project. Version details can be updated using the {setVersion.Name} and {updateVersion.Name} cmdlet. The following table details the version details that can be retrieved:

{GetVersionTable()}

Note that if {help.Command} detects that the .git folder is missing from the repo or that the ""git"" command is not installed on your system, the PreviousTag property will be omitted from results.
";

            help.Parameters = new HelpParameter[]
            {
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, $"Specifies whether to retrieve the versions used when compiling {project.Name} using the .NET Core SDK or the legacy .NET Framework tooling.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Retrieve version information about the {project.Name} project.")
            };

            help.RelatedLinks = new[]
            {
                setVersion,
                updateVersion
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }

        private static string GetVersionTable()
        {
            throw new NotImplementedException();
        }

        public string[] GetLegacyParameterSets()
        {
            throw new NotImplementedException();
        }
    }
}