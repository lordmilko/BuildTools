using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Help")]
    [BuildCommand(CommandKind.OpenWiki, CommandCategory.Help)]
    public abstract class GetHelp<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        public static void CreateHelp(HelpConfig help, ProjectConfig project)
        {
            help.Synopsis = $"Opens the {project.Name} Wiki for getting help with the {project.Name} Build Environment.";
            help.Description = $@"
The {help.Command} cmdlet opens the {project.Name} Wiki page containing detailed instructions on compiling {project.Name} and using the {project.Name} Build Environment.

Note: due to limitations of the Unix platform, when running on Linux or macOS the {help.Command} cmdlet will instead display the URL that you should navigate to instead of automatically opening the URL in your default web browser.";

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Open the {project.Name} Wiki article detailing how to compile {project.Name}.")
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}
