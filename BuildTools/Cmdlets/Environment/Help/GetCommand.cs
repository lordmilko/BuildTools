using System;
using System.Linq;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "Command")]
    [BuildCommand(CommandKind.CommandList, CommandCategory.Help, Feature.System)]
    public abstract class GetCommand<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string Name { get; set; }

        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
        {
            help.Synopsis = $"Retrieves commands that are available in the {project.Name} Build Environment.";
            help.Description = $"The {help.Command} cmdlet retrieves all commands that are available for use within the {project.Name} Build Environment. Each command contains a description outlining what exactly that command does. The results from {help.Command} can be filtered by specifying a wildcard expression matching part of the command's name you wish to find.";

            help.Parameters = new[]
            {
                new HelpParameter(nameof(Name), "Wildcard used to filter results to a specific command.")
            };

            help.Examples = new[]
            {
                new HelpExample(help.Command, $"List all commands supported by the {project.Name} Build Module"),
                new HelpExample($"{help.Command} *build*", "List all commands whose name contains \"build\"")
            };
        }

        protected override void ProcessRecordEx()
        {
            var service = GetService<ICommandService>();

            var commands = service.GetCommands();

            if (MyInvocation.BoundParameters.ContainsKey(nameof(Name)))
            {
                var wildcard = new WildcardPattern(Name, WildcardOptions.IgnoreCase);

                commands = commands.Where(v => wildcard.IsMatch(v.Name)).ToArray();
            }

            foreach (var command in commands.OrderBy(c => c.Category).ThenBy(c => c.Name))
            {
                var pso = new PSObject();
                pso.Properties.Add(new PSNoteProperty("Name", command.Name));
                pso.Properties.Add(new PSNoteProperty("Category", command.Category.ToString())); //If this is an enum it will be right aligned
                pso.Properties.Add(new PSNoteProperty("Description", command.Description));

                WriteObject(pso);
            }
        }
    }
}
