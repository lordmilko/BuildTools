using System;
using System.Collections.Generic;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    class GitFileStatus
    {
        public string Path { get; set; }
        public GitChangeType? StagedStatus { get; set; }
        public GitChangeType? UnstagedStatus { get; set; }
    }

    enum GitChangeType
    {
        Added,
        Modified,
        Renamed,
        Deleted
    }

    [Cmdlet(VerbsCommon.Get, "GitStatus")]
    [BuildCommand(CommandKind.GitStatus, CommandCategory.Utility)]
    public abstract class GetGitStatus<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        public static void CreateHelp(HelpConfig help, ProjectConfig project, ICommandService commandService)
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
            var configProvider = GetService<IProjectConfigProvider>();
            var process = GetService<IProcessService>();

            var lines = process.Execute(
                "git",
                new ArgList
                {
                    "-C",
                    configProvider.SolutionRoot,
                    "status",
                    "--porcelain"
                }
            );

            var results = new List<GitFileStatus>();

            foreach (var line in lines)
            {
                var code = line.Substring(0, 2);
                var path = line.Substring(3);

                var status = new GitFileStatus
                {
                    Path = path
                };

                switch (code)
                {
                    case " M":
                        //The file has been modified and no part of it has been staged
                        status.UnstagedStatus = GitChangeType.Modified;
                        break;

                    case "M ":
                        //The file has been modified and all parts of it have been staged
                        status.StagedStatus = GitChangeType.Modified;
                        break;

                    case "MM":
                        status.StagedStatus = GitChangeType.Modified;
                        status.UnstagedStatus = GitChangeType.Modified;
                        break;

                    case "A ":
                        //A new file has been added and has been staged
                        status.StagedStatus = GitChangeType.Added;
                        break;

                    case "R ":
                        //A rename of a file has been staged
                        status.StagedStatus = GitChangeType.Renamed;
                        break;

                    case "D ":
                        //A deletion of a file has been staged
                        status.StagedStatus = GitChangeType.Deleted;
                        break;

                    case "??":
                        status.UnstagedStatus = GitChangeType.Added;
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle code '{code}' for item '{line}'");
                }

                results.Add(status);
            }

            foreach (var result in results)
                WriteObject(result);
        }
    }
}