using System;
using System.Collections.Generic;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools
{
    class ProcessOutputWriter
    {
        private IPowerShellService powerShell;
        private bool writeHost;

        private List<string> output = new List<string>();

        public string[] Output => output.ToArray();

        public ProcessOutputWriter(bool writeHost, IPowerShellService powerShell)
        {
            this.writeHost = writeHost;
            this.powerShell = powerShell;
        }

        public void Write(ProcessOutputObject record)
        {
            if (writeHost)
            {
                if (record.Data is ErrorRecord e)
                    powerShell.WriteColor(e.Exception.Message, ConsoleColor.Red);
                else
                    powerShell.WriteColor(record.Data.ToString());
            }
            else
            {
                if (record.Data is ErrorRecord e)
                    output.Add(e.Exception.Message);
                else
                    output.Add(record.Data.ToString());
            }
        }

        public override string ToString()
        {
            return string.Join(Environment.NewLine, output);
        }
    }
}
