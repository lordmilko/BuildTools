using System;
using BuildTools.PowerShell;

namespace BuildTools
{
    class Logger
    {
        private readonly EnvironmentService environmentService;
        private readonly IPowerShellService powerShell;

        private readonly IConsoleLogger consoleLogger;
        private readonly IFileLogger fileLogger;

        public Logger(
            EnvironmentService environmentService,
            IPowerShellService powerShell,
            IConsoleLogger consoleLogger,
            IFileLogger fileLogger)
        {
            this.environmentService = environmentService;
            this.powerShell = powerShell;
            this.consoleLogger = consoleLogger;
            this.fileLogger = fileLogger;
        }

        public void LogHeader(string message)
        {
            if (environmentService.IsAppveyor)
                LogInformation(message);
            else
                LogInformation(message, ConsoleColor.Cyan);
        }

        public void LogSubHeader(string message)
        {
            LogInformation(message, ConsoleColor.Magenta);
        }

        public void LogWarning(string message)
        {
            powerShell.WriteWarning(message);
        }

        /// <summary>
        /// Logs a <see cref="ConsoleColor.Yellow"/> message to bring the users attention to an important event.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void LogAttention(string message)
        {
            LogInformation(message, ConsoleColor.Yellow);
        }

        public void LogInformation(string message, ConsoleColor? color = null)
        {
            message = $"\t{message}".Replace("\t", "    ");

            if (powerShell.IsProgressEnabled)
            {
                powerShell.WriteProgress(currentOperation: message);
            }
            else
            {
                consoleLogger.Log(message, color);
            }

            fileLogger.LogBuild(message);
        }

        public void LogVerbose(string message)
        {
            powerShell.WriteVerbose(message);
        }
    }
}
