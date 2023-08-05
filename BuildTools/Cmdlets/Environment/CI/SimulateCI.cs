using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    public enum AppveyorTask
    {
        Install,
        Restore,
        Build,
        Package,
        Test,
        Coverage
    }

    [Alias("Simulate-CI")]
    [Cmdlet(VerbsDiagnostic.Test, "CI", DefaultParameterSetName = ParameterSet.Appveyor)]
    [BuildCommand(CommandKind.SimulateCI, CommandCategory.CI)]
    public abstract class SimulateCI<TEnvironment> : BuildCmdlet<TEnvironment>, ILegacyProvider
    {
        [Parameter(Mandatory = false, ParameterSetName = ParameterSet.Appveyor)]
        public SwitchParameter Appveyor { get; set; }

        [Parameter(Mandatory = true, ParameterSetName = ParameterSet.Travis)]
        public SwitchParameter Travis { get; set; }

        [Parameter(Mandatory = false, Position = 0, ParameterSetName = ParameterSet.Appveyor)]
        public AppveyorTask[] Task { get; set; }

        [Parameter(Mandatory = false)]
        public BuildConfiguration Configuration { get; set; } = BuildConfiguration.Debug;

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Simulates building {project.Name} under a Continuous Integration environment";

            help.Description = $"The {help.Command} cmdlet simulates the entire workflow of building {project.Name} under either Appveyor or Travis CI. By default, {help.Command} will invoke all steps that would normally be performed as part of the CI process. This can be limited by specifying a specific list of tasks that should be simulated via the -{nameof(Task)} parameter.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Appveyor), "Specifies to simulate Appveyor CI"),
                new HelpParameter(nameof(Travis), "Specifies to simulate Travis CI"),
                new HelpParameter(nameof(Task), "CI task to execute. If no value is specified, all CI tasks will be executed."),
                new ConditionalHelpParameter(NeedLegacyParameter, LegacyParameterName, "Specifies whether to use .NET Core CLI or legacy .NET infrastructure when simulating CI tasks"),
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, "Simulate Appveyor CI"),
                new HelpExample($"{help.Command} -Travis", "Simulate Travis CI"),
                new HelpExample($"{help.Command} -Task Test", "Simulate Appveyor CI tests")
            };
        }

        protected SimulateCI() : base(false)
        {
        }

        protected override void ProcessRecordEx()
        {
            switch (ParameterSetName)
            {
                case ParameterSet.Appveyor:
                    ProcessAppveyor();
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle parameter set '{ParameterSetName}'.");
            }
        }

        private void ProcessAppveyor()
        {
            var simulateAppveyorService = GetService<SimulateAppveyorService>();

            if (Task == null)
                simulateAppveyorService.Execute(Configuration, IsLegacyMode);
            else
            {
                var services = new Dictionary<AppveyorTask, Type>
                {
                    { AppveyorTask.Install, typeof(InvokeAppveyorInstallService) },
                    { AppveyorTask.Restore, typeof(InvokeAppveyorBeforeBuildService) },
                    { AppveyorTask.Build, typeof(InvokeAppveyorBuildService) },
                    { AppveyorTask.Package, typeof(InvokeAppveyorBeforeTestService) },
                    { AppveyorTask.Test, typeof(InvokeTestService) },
                    { AppveyorTask.Coverage, typeof(InvokeAppveyorAfterTestService) },
                };

                simulateAppveyorService.SimulateEnvironment(() =>
                {
                    foreach (var kv in services)
                    {
                        if (Task.Contains(kv.Key))
                        {
                            var service = (IAppveyorService)BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService(kv.Value);

                            service.Execute(Configuration, IsLegacyMode);
                        }
                    }
                }, Configuration);
            }
        }

        public string[] GetLegacyParameterSets() => null;
    }
}
