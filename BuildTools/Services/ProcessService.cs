﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BuildTools
{
    interface IProcessService
    {
        void Execute(string fileName, string arguments = null, string errorFormat = null);

        void Execute(string fileName, IEnumerable<string> arguments = null, string errorFormat = null);
    }

    class ProcessService : IProcessService
    {
        public void Execute(string fileName, string arguments = null, string errorFormat = null)
        {
            var psi = new ProcessStartInfo(fileName, arguments);

            var process = Process.Start(psi);

            if (process == null)
                throw new InvalidOperationException($"Failed to start process '{fileName}'.");

            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    string.Format(
                        errorFormat ?? $"Process '{fileName}' exited with error code '{{0}}'.",
                        process.ExitCode
                    )
                );
            }
        }

        public void Execute(string fileName, IEnumerable<string> arguments = null, string errorFormat = null) =>
            Execute(fileName, arguments == null ? null : string.Join(" ", arguments), errorFormat);
    }
}