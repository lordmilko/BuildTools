using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BuildTools.PowerShell;

namespace BuildTools
{
    class GetLogConfig
    {
        public LogKind Kind { get; set; }

        public string[] Pattern { get; set; }

        public int Lines { get; set; } = 10;

        public bool Full { get; set; }

        public bool NewWindow { get; set; }

        public bool Clear { get; set; }
    }

    class GetLogService
    {
        private IFileSystemProvider fileSystem;
        private IFileLogger fileLogger;
        private IProcessService processService;
        private IPowerShellService powerShell;

        public GetLogService(
            IFileSystemProvider fileSystem,
            IFileLogger fileLogger,
            IProcessService processService,
            IPowerShellService powerShell)
        {
            this.fileSystem = fileSystem;
            this.fileLogger = fileLogger;
            this.processService = processService;
            this.powerShell = powerShell;
        }

        public void Execute(GetLogConfig config)
        {
            var logFile = fileLogger.GetLogFile(config.Kind);

            if (fileSystem.FileExists(logFile))
            {
                if (config.Clear)
                    fileSystem.WriteFileText(logFile, string.Empty);

                if (config.Full)
                    processService.Execute(logFile, shellExecute: true);
                else
                {
                    void BuildCommand(StringBuilder builder)
                    {
                        builder.Append($"gc '{logFile}' -Tail {config.Lines} -Wait");

                        if (config.Pattern != null && config.Pattern.Length > 0)
                            builder.Append($" | sls {string.Join(", ", config.Pattern.Select(v => $"'{v}'"))}");

                        //We won't emit to Out-Default since we're invoking our script internally, so we need to
                        //force Write-Host manually
                        builder.Append("| foreach { Write-Host $_ }");
                    }

                    if (config.NewWindow)
                    {
                        var fileName = Process.GetCurrentProcess().MainModule.FileName;

                        var builder = new StringBuilder();

                        builder.Append("-Command \"");
                        builder.Append($"$Host.UI.RawUI.WindowTitle = '{logFile}'; ");

                        BuildCommand(builder);

                        builder.Append("\"");

                        processService.Execute(fileName, builder.ToString(), shellExecute: true);
                    }
                    else
                    {
                        //Clear-Host works differently cross-platform; on Windows we can fiddle
                        //with the console buffer, but on Unix the "clear" application is invoked,
                        //so we're better off just dispatching to Clear-Host directly
                        powerShell.SetWindowTitle(logFile);

                        //The logic of waiting for changes is pretty complicated, so we just defer to PowerShell's builtin system

                        var builder = new StringBuilder();
                        BuildCommand(builder);

                        builder.Insert(0, "cls; ");

                        powerShell.InvokeWithArgs(builder.ToString());
                    }
                }
            }
            else
                powerShell.WriteColor($"{logFile} does not exist", ConsoleColor.Red);
        }
    }
}
