using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "GitStatus")]
    [BuildCommand(CommandKind.GitStatus, CommandCategory.Utility)]
    public abstract class GetGitStatus<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        public static void CreateHelp(HelpConfig help, ProjectConfig project, CommandService commandService)
        {
            help.Synopsis = "Gets the current \"git status\" in a PowerShell friendly format.";
            help.Description = $@"The {help.Command} cmdlet retrieves the current ""git status"" for the {project.Name} working directory, and displays it in a PowerShell friendly format.

Files with changes are flagged as having either unstaged or staged changes. Files that have both staged and unstaged statuses have had partial changes staged but still have some remaining changes unstaged.";

            help.Examples = new[]
            {
                new HelpExample(help.Command, "Gets the current git status")
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}