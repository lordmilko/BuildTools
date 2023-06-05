using System;
using System.Diagnostics;
using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class ProcessService : IProcessService
    {
        private IPowerShellService powerShell;

        public ProcessService(IPowerShellService powerShell)
        {
            this.powerShell = powerShell;
        }

        public string[] Execute(
            string fileName,
            ArgList arguments = default,
            string errorFormat = null,
            bool writeHost = false)
        {
            var writer = new ProcessOutputWriter(writeHost, powerShell);
            var executor = new ProcessExecutor(
                fileName,
                arguments,
                errorFormat,
                writeHost,
                writer
            );

            executor.Execute();

            return writer.Output;
        }

        public bool IsRunning(string processName)
        {
            var processes = Process.GetProcesses();

            return processes.Any(p => p.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
