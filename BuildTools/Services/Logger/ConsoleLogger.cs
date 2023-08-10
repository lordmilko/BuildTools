using System;
using BuildTools.PowerShell;

namespace BuildTools
{
    interface IConsoleLogger
    {
        void Log(string message, ConsoleColor? color);
    }

    class ConsoleLogger : IConsoleLogger
    {
        private IPowerShellService powerShell;

        public ConsoleLogger(IPowerShellService powerShell)
        {
            this.powerShell = powerShell;
        }

        public void Log(string message, ConsoleColor? color)
        {
            //Appveyour has a custom PSHostUserInterface in Host.UI.externalUI,
            //which I imagine captures all output and displays it in the log. As such,
            //if you Console.WriteLine(), it will bypass their PSHostUserInterface
            powerShell.WriteColor(message, color);
        }
    }
}
