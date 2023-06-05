using System.Collections.Concurrent;
using System.Diagnostics;
using System.Management.Automation;
using System.Threading;

namespace BuildTools
{
    class ProcessOutputHandler
    {
        private Process process;
        private BlockingCollection<ProcessOutputObject> queue;
        private int refCount;
        private bool isFirstError = true;

        public ProcessOutputHandler(Process process, BlockingCollection<ProcessOutputObject> queue)
        {
            this.process = process;
            this.queue = queue;

            refCount++;
            process.OutputDataReceived += Process_OutputDataReceived;
            process.BeginOutputReadLine();

            refCount++;
            process.ErrorDataReceived += Process_ErrorDataReceived;
            process.BeginErrorReadLine();
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
                queue.Add(new ProcessOutputObject(e.Data));
            else
                Decrement();
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                ErrorRecord errorRecord;

                if (isFirstError)
                {
                    isFirstError = false;

                    errorRecord = new ErrorRecord(
                        new RemoteException(e.Data),
                        "NativeCommandError",
                        ErrorCategory.NotSpecified,
                        e.Data
                    );
                }
                else
                {
                    errorRecord = new ErrorRecord(
                        new RemoteException(e.Data),
                        "NativeCommandErrorMessage",
                        ErrorCategory.NotSpecified,
                        null
                    );
                }

                queue.Add(new ProcessOutputObject(errorRecord));
            }
            else
                Decrement();
        }

        private void Decrement()
        {
            Debug.Assert(refCount > 0);

            if (Interlocked.Decrement(ref refCount) == 0)
                queue.CompleteAdding();
        }
    }
}