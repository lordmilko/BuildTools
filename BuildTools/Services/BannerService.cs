using System;
using System.Collections.Generic;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class CommandLine
    {
        public string Message { get; set; }

        public string Command { get; }

        public CommandLine(string message, string command)
        {
            Message = message;
            Command = command;
        }
    }

    class BannerService
    {
        private IProjectConfigProvider configProvider;
        private ICommandService commandService;
        private IPowerShellService powerShell;

        private List<CommandLine> lines = new List<CommandLine>();

        public BannerService(
            IProjectConfigProvider configProvider,
            ICommandService commandService,
            IPowerShellService powerShell)
        {
            this.configProvider = configProvider;
            this.commandService = commandService;
            this.powerShell = powerShell;
        }

        public void DisplayBanner()
        {
            lines.Clear();

            PrintWelcome();

            PrintCommand($"Build the latest version of {configProvider.Config.Name}:", CommandKind.InvokeBuild);
            PrintCommand("To find out what commands are available, type:", CommandKind.CommandList);
            PrintCommand($"Open a {configProvider.Config.Name} prompt with:", CommandKind.LaunchModule);
            PrintCommand($"If you need more help, visit the {configProvider.Config.Name} Wiki:", CommandKind.OpenWiki);

            PrintCommandLines();

            PrintCopyright();
        }

        private void PrintWelcome()
        {
            var pad = string.Empty.PadLeft(10);
            powerShell.WriteColor($"{pad}Welcome to the {configProvider.Config.Name} Build Environment!{Environment.NewLine}");
        }

        private void PrintCommand(string text, CommandKind kind)
        {
            var command = commandService.GetCommand(kind);

            lines.Add(new CommandLine(text, command.Name));
        }

        private void PrintCommandLines()
        {
            var longest = lines.Max(l => l.Message.Length);

            //Pad each line 2 to the left, and the longest line + 7

            var right = longest + 7;

            foreach (var line in lines)
            {
                line.Message = "  " + line.Message.PadRight(right);

                powerShell.WriteColor(line.Message, newLine: false);
                powerShell.WriteColor(line.Command, ConsoleColor.Yellow);
            }
        }

        private void PrintCopyright()
        {
            var nl = Environment.NewLine;
            var pad = string.Empty.PadLeft(10);
            powerShell.WriteColor($"{nl}{pad}Copyright (C) {configProvider.Config.CopyrightAuthor}, {configProvider.Config.CopyrightYear}{nl}{nl}");
        }
    }
}
