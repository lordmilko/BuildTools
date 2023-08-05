using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace BuildTools
{
    class ProcessExecutor
    {
        public string FileName { get; }

        public ArgList Arguments { get; }

        public string ErrorFormat { get; }

        public bool WriteHost { get; }

        private bool shellExecute;

        private Process process;
        private ProcessOutputWriter writer;
        private BlockingCollection<ProcessOutputObject> queue = new BlockingCollection<ProcessOutputObject>();

        public ProcessExecutor(
            string fileName,
            ArgList arguments,
            string errorFormat,
            bool writeHost,
            bool shellExecute,
            ProcessOutputWriter writer)
        {
            FileName = fileName;
            Arguments = arguments;
            ErrorFormat = errorFormat;
            WriteHost = writeHost;
            this.shellExecute = shellExecute;

            this.writer = writer;
        }

        public void Execute()
        {
            process = new Process
            {
                StartInfo =
                {
                    FileName = FileName,
                    Arguments = Arguments,
                    CreateNoWindow = false,
                    RedirectStandardOutput = !shellExecute,
                    RedirectStandardError = !shellExecute,
                    UseShellExecute = shellExecute
                }
            };

            var fileName = Path.GetFileNameWithoutExtension(FileName).ToLower();

            if (!shellExecute)
            {
                if (fileName == "powershell" || fileName == "pwsh")
                {
                    process.StartInfo.EnvironmentVariables.Remove("PSModulePath");
                }
            }

            if (!process.Start())
                throw new InvalidOperationException($"Failed to start process '{FileName}'.");

            if (!shellExecute)
            {
                _ = new ProcessOutputHandler(process, queue);

                ProcessQueue(blocking: false);
                FinalizeProcess();
            }
        }

        private void ProcessQueue(bool blocking)
        {
            ProcessOutputObject record;

            while ((record = DequeueProcessOutput(blocking)) != null)
                writer.Write(record);
        }

        private ProcessOutputObject DequeueProcessOutput(bool blocking)
        {
            if (blocking)
            {
                if (!queue.IsCompleted)
                {
                    //queue.Take() internally calls TryTake and can throw if it returns false
                    //when CompleteAdding() is called after we've already started waiting. As we wait
                    //infinitely here just like Take() does, we achieve the same behavior, without the
                    //exception when the race occurs
                    queue.TryTake(out var record, -1);
                    return record;
                }

                return null;
            }
            else
            {
                queue.TryTake(out var record);
                return record;
            }
        }

        private void FinalizeProcess()
        {
            ProcessQueue(blocking: true);

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                var errorMsg = ErrorFormat ?? $"Process '{FileName}' exited with error code '{{0}}'.";

                errorMsg = string.Format(errorMsg, process.ExitCode);

                if (!WriteHost)
                    errorMsg = $"{errorMsg}{Environment.NewLine}{Environment.NewLine}{writer}";

                throw new ProcessException(errorMsg);
            }
        }
    }
}
