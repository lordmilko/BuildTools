using System;
using System.Collections.Concurrent;
using System.Diagnostics;

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
                    try
                    {
                        //If we call queue.CompleteAdding() after we've started waiting, we'll throw, so we need to catch this
                        return queue.Take();
                    }
                    catch (InvalidOperationException)
                    {
                    }
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

                if (!WriteHost)
                {
                    errorMsg = $"{errorMsg}{Environment.NewLine}{Environment.NewLine}{writer}";
                }

                throw new ProcessException(
                    string.Format(
                        errorMsg,
                        process.ExitCode
                    )
                );
            }
        }
    }
}