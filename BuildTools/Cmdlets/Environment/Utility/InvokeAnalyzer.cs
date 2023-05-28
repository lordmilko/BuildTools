using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Install, "Analyze")]
    [BuildCommand(CommandKind.InvokePSAnalyzer, CommandCategory.Utility)]
    public abstract class InvokeAnalyzer<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Fix { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, CommandService commandService)
        {
            help.Synopsis = $"Analyzes best practice rules on {project.Name} PowerShell files";
            help.Description = $@"
The {help.Command} cmdlet analyzes best practice rules on the {project.Name} repository on all PowerShell script files. By default, {help.Command} will report violations on all files in the {project.Name} repository. This can be limited to a particular subset of files by specifying a wildcard to the -Name parameter.

For certain rule violations, {help.Command} can automatically apply the recommended fixes for you by specifying the -Fix parameter. To view the changes that will be applied it is recommended to also apply the -WhatIf parameter, and to have a clean Git working directory so that you may undo all of the changes or apply them in a single commit as desired.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "A wildcard expression used to limit the files that should be analyzed."),
                new HelpParameter(nameof(Fix), "Automatically fix any rule violations where possible.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"Report on violations across all PowerShell files in the {project.Name} repository."),
                new HelpExample($"{help.Command} *foo*", "Report on all violations across all PowerShell files whose name contains the word 'foo'."),
                new HelpExample($"{help.Command} -Fix -WhatIf", "View corrections that will be performed on rule violations."),
                new HelpExample($"{help.Command} -Fix", "Automatically fix all rule violations where possible.")
            };
        }

        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}