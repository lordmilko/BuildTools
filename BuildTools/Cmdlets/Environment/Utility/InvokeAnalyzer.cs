using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Management.Automation;
using BuildTools.PowerShell;
using BuildTools.Reflection;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsLifecycle.Invoke, "Analyzer", SupportsShouldProcess = true)]
    [BuildCommand(CommandKind.InvokePSAnalyzer, CommandCategory.Utility)]
    public abstract class InvokeAnalyzer<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        private static readonly string rules = string.Join(",", new[]
        {
            "PSAvoidUsingCmdletAliases",
            "PSAvoidDefaultValueForMandatoryParameter",
            "PSAvoidGlobalAliases",
            "PSAvoidGlobalFunctions",
            "PSReservedParams",
            "PSPossibleIncorrectComparisonWithNull",
            "PSPossibleIncorrectUsageOfAssignmentOperator",
            "PSUseBOMForUnicodeEncodedFile",
            "PSUseConsistentIndentation",
            "PSUseConsistentWhitespace",
            "PSUseCorrectCasing",
            "PSUseLiteralInitializerForHashtable",
            "PSUseUTF8EncodingForHelpFile"
        });

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
            var dependencyProvider = GetService<DependencyProvider>();
            var configProvider = GetService<IProjectConfigProvider>();
            var powerShell = GetService<IPowerShellService>();
            var commandService = GetService<CommandService>();
            var fileSystem = GetService<IFileSystemProvider>();
            var commandName = commandService.GetCommand(CommandKind.InvokePSAnalyzer).Name;

            dependencyProvider.Install(WellKnownDependency.PSScriptAnalyzer);

            FixDiagnosticFormat();

            var root = configProvider.SolutionRoot;

            powerShell.WriteProgress(
                commandName,
                "Identifying all script files..."
            );

            try
            {
                var packages = Path.Combine(root, "packages");

                var extensions = new[] { "*.ps1", "*.psm1", "*.psd1" };

                var files = extensions.SelectMany(e => fileSystem.EnumerateFiles(root, e, SearchOption.AllDirectories).Where(p =>
                    !p.StartsWith(packages, StringComparison.OrdinalIgnoreCase)
                )).ToArray();

                for (var i = 0; i < files.Length; i++)
                {
                    powerShell.WriteProgress(
                        commandName,
                        $"Analyzing '{Path.GetFileName(files[i])}' ({i + 1}/{files.Length})",
                        percentComplete: (int)((double)(i + 1) / files.Length * 100)
                    );

                    bool whatIf = MyInvocation.BoundParameters.TryGetValue("WhatIf", out var whatIfValue) && (bool)(SwitchParameter)whatIfValue;

                    var args = new[]
                    {
                        $"-Path '{files[i]}'",
                        $"-IncludeRule {rules}",
                        $"-WhatIf:{(whatIf ? "$true" : "$false")}",
                        $"-Fix:{(Fix ? "$true" : "$false")}"
                    };

                    var results = powerShell.InvokeWithArgs("Invoke-ScriptAnalyzer", args);

                    foreach (var result in results)
                        WriteObject(result);
                }
            }
            finally
            {
                powerShell.CompleteProgress();
            }
        }

        private void FixDiagnosticFormat()
        {
            //The default format of DiagnosticRecord applies a ridiculously small width to the ScriptName and Message columns, such that they always wrap
            //even when your PowerShell window is maximized. As such, we hack the relevant ps1xml format to remove all fixed column widths,
            //allowing the output to natively expand to fill our screen

            if (BuildToolsSessionState.ScriptAnalyzerRepaired)
                return;

            if (InvokeCommand.GetCommand("Invoke-PSScriptAnalyzer", CommandTypes.All) == null)
                InvokeCommand.InvokeScript("Import-Module PSScriptAnalyzer");

            var database = this.GetInternalProperty("Context").GetInternalProperty("FormatDBManager").GetInternalProperty("Database");

            var viewDefinitionList = ((IList) database.GetInternalField("viewDefinitionsSection").GetInternalField("viewDefinitionList")).Cast<object>();

            var diagnosticView = viewDefinitionList.Single(view =>
            {
                if ((string) view.GetInternalField("name") != "PSScriptAnalyzerView")
                    return false;

                var referenceList = ((IList) view.GetInternalField("appliesTo").GetInternalField("referenceList")).Cast<object>();

                return referenceList.Any(r => (string) r.GetInternalField("name") == "Microsoft.Windows.PowerShell.ScriptAnalyzer.Generic.DiagnosticRecord");
            });

            var headerList = (IList) diagnosticView.GetInternalField("mainControl").GetInternalField("header").GetInternalField("columnHeaderDefinitionList");

            foreach (var header in headerList)
            {
                var widthInfo = header.GetType().GetInternalFieldInfo("width");

                widthInfo.SetValue(header, 0);
            }

            BuildToolsSessionState.ScriptAnalyzerRepaired = true;
        }
    }
}