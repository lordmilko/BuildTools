﻿using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Version")]
    [BuildCommand(CommandKind.GetVersion, CommandCategory.Version, Feature.Version)]
    public abstract class GetVersion<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        public static void CreateHelp(HelpConfig help, IProjectConfigProvider configProvider, ICommandService commandService)
        {
            var project = configProvider.Config;

            var setVersionName = commandService.GetCommandNameOrDefault(CommandKind.SetVersion);
            var updateVersionName = commandService.GetCommandNameOrDefault(CommandKind.UpdateVersion);

            help.Synopsis = $"Retrieves version information used by various components of {project.Name}";
            help.Description = $@"
The {help.Command} cmdlet retrieves version details found in various locations in the {project.Name} project. Version details can be updated using the {setVersionName} and {updateVersionName} cmdlets. The following table details the version details that can be retrieved:

{GetVersionTable(configProvider)}

Note that if {help.Command} detects that the .git folder is missing from the repo or that the ""git"" command is not installed on your system, the PreviousTag property will be empty.
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
                commandService.GetOptionalCommand(CommandKind.SetVersion),
                commandService.GetOptionalCommand(CommandKind.UpdateVersion)
            };
        }

        protected override void ProcessRecordEx()
        {
            var service = GetService<GetVersionService>();

            var result = service.GetVersion(IsLegacyMode);

            WriteObject(result);
        }

        private static string GetVersionTable(IProjectConfigProvider configProvider)
        {
            try
            {
                var props = configProvider.GetVersionPropsPath(true);
                var psd1 = configProvider.GetSourcePowerShellModuleManifest(true);

                var table = new VersionTableBuilder
                {
                    { VersionType.Package, props },
                    { VersionType.Assembly, props },
                    { VersionType.File, props },
                    { VersionType.Module, psd1 },
                    { VersionType.ModuleTag, psd1 },
                    { VersionType.PreviousTag, "Git" },
                };

                return table.ToString();
            }
            catch (Exception ex)
            {
                return $"[Failed to get version table: {ex.Message}]";
            }
        }

        public string[] GetLegacyParameterSets() => null;
    }
}
