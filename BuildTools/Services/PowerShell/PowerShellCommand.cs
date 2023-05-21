using System.Management.Automation;

namespace BuildTools.PowerShell
{
    public interface IPowerShellCommand
    {
        string Name { get; }

        string Source { get; }
    }

    class PowerShellCommand : IPowerShellCommand
    {
        private readonly CommandInfo commandInfo;

        public string Name => commandInfo.Name;

        public string Source => commandInfo.Source;

        public PowerShellCommand(CommandInfo commandInfo)
        {
            this.commandInfo = commandInfo;
        }
    }
}