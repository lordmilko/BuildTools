using System;
using System.IO;
using BuildTools.PowerShell;

namespace BuildTools
{
    public class ScriptPackageTest : PSPackageTest
    {
        public string Result { get; }

        public ScriptPackageTest(string command, string result) : base(command)
        {
            Result = result;
        }

        internal void Test(
            IProcessService processService,
            PSEdition edition,
            string dll)
        {
            var exe = GetPowerShellExecutable(edition);

            var result = string.Join(string.Empty, processService.Execute(exe, $"-command \"Add-Type -Path '{dll}'; {Command}\""));

            if (result != Result)
                throw new InvalidOperationException($"Module {Path.GetFileName(Path.GetDirectoryName(dll))} was not loaded successfully; attempt to use module returned '{result}'");
        }
    }
}
