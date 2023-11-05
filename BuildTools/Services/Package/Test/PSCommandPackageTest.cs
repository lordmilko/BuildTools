using System;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools
{
    public class PSCommandPackageTest : PSPackageTest
    {
        public CommandTypes Type { get; }

        public string Result { get; }

        public PSCommandPackageTest(string command, string result, CommandTypes type) : base(command)
        {
            Result = result;
            Type = type;
        }

        internal void Test(
            IProcessService processService,
            PSEdition edition,
            string module)
        {
            string args;

            switch (Type)
            {
                case CommandTypes.Cmdlet:
                    args = $"&{{ import-module '{module}'; try {{ {Command} }} catch [exception] {{ $_.exception.message }} }}";
                    break;

                case CommandTypes.Function:
                    args = $"&{{ import-module '{module}'; {Command} }}";
                    break;

                default:
                    throw new NotImplementedException($"Don't know how to handle '{nameof(CommandTypes)}' type '{Type}'.");
            }

            var exe = GetPowerShellExecutable(edition);
            var result = string.Join(string.Empty, processService.Execute(exe, $"-command \"{args}\""));

            if (result != Result)
            {
                if (string.IsNullOrWhiteSpace(result))
                    throw new InvalidOperationException($"Command '{Command}' returned an empty value");

                throw new InvalidOperationException($"Test command '{Command}' invoked in '{exe}' returned '{result}'");
            }
                
        }
    }
}
