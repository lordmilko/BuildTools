using System;
using System.Linq;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Log")]
    [BuildCommand(CommandKind.Log, CommandCategory.Utility, Feature.System)]
    public abstract class GetLog<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Integration)]
        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Build)]
        public string[] Pattern
        {
            get => config.Pattern;
            set => config.Pattern = value;
        }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.BuildFull)]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.IntegrationFull)]
        public SwitchParameter Full
        {
            get => config.Full;
            set => config.Full = value;
        }

        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Integration)]
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Build)]
        public int Lines
        {
            get => config.Lines;
            set => config.Lines = value;
        }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.Build)]
        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.BuildFull)]
        public SwitchParameter Build { get; set; }

        [Parameter(Mandatory = false)]
        public SwitchParameter Clear
        {
            get => config.Clear;
            set => config.Clear = value;
        }

        [Alias("Window")]
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Integration)]
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Build)]
        public SwitchParameter NewWindow
        {
            get => config.NewWindow;
            set => config.NewWindow = value;
        }

        private readonly GetLogConfig config = new GetLogConfig();

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Continuously retrieves logs emitted by the {project.Name} Build Environment";
            help.Description = $@"
The {help.Command} cmdlet continuously retrieves logs emitted by cmdlets used to build {project.Name}. If the solution contains any integration tests, by default, {help.Command} will retrieve the Integration Test log for listing verbose status details when executing long running integration tests. If the -Build parameter is specified, {help.Command} will retrieve logs emitted by all cmdlets in the {project.Name} Build Environment.

If the solution does not contain any integration tests, build logs will instead be displayed by default.

When invoked, {help.Command} will begin continuously tailing the end of the specified log file, showing any new entries in the window where {help.Command} was invoked. {help.Command} can filter the entries emitted to the console by specifying a pattern to the -Pattern parameter. If -Clear is specified, the contents of the specified log will will be removed before {help.Command} begins streaming.

If you wish to view logs in a separate window from where {help.Command} was invoked, you can do so by specifying the -NewWindow parameter. If you wish to view the entirety of the specified log, you can specify the -Full parameter to open the log file in your default text editor.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Pattern), "A pattern to use to filter log entries emitted to the console."),
                new HelpParameter(nameof(Full), "Opens the log file in your default text editor, rather than streaming the end of the file to the console."),
                new HelpParameter(nameof(Lines), $"Number of lines to display from the end of the log file when {help.Command} is first invoked. By default this value is 10"),
                new HelpParameter(nameof(Build), $"Retrieves the {project.Name} Build log instead of Integration Test log"),
                new HelpParameter(nameof(Clear), "Clear the specified log before viewing it"),
                new HelpParameter(nameof(NewWindow), "Opens the log in a new window.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, "View the default log file"),
                new HelpExample($"{help.Command} -Build", "View the Build log file"),
                new HelpExample($"{help.Command} -Full", "Open the default log file in a text editor"),
                new HelpExample($"{help.Command} -Clear", "Clear and view the default log file")
            };

            help.RelatedLinks = new[]
            {
                commandService.GetOptionalCommand(CommandKind.InvokeTest)
            };
        }

        protected override void ProcessRecordEx()
        {
            var getLogService = GetService<GetLogService>();

            var kind = GetLogKind();

            if (ParameterSetName == ParameterSet.Integration)
            {
                if (MyInvocation.BoundParameters.TryGetValue(nameof(Build), out var value))
                {
                    if (!(SwitchParameter)value)
                        throw new InvalidOperationException($"-{nameof(Build)}:$false was specified, however only build logs are supported when no integration tests exist");
                }
                else
                {
                    var configProvider = GetService<IProjectConfigProvider>();

                    if (!configProvider.GetProjects(false).Any(p => (p.Kind & ProjectKind.IntegrationTest) != 0))
                        kind = LogKind.Build;
                }
            }

            config.Kind = kind;
            getLogService.Execute(config);
        }

        private LogKind GetLogKind()
        {
            switch (ParameterSetName)
            {
                case ParameterSet.Integration:
                case ParameterSet.IntegrationFull:
                    return LogKind.Integration;

                case ParameterSet.Build:
                case ParameterSet.BuildFull:
                    return LogKind.Build;

                default:
                    throw new NotImplementedException($"Don't know how to handle parameter set '{ParameterSetName}'.");
            }
        }
    }
}
