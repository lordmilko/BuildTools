using BuildTools.PowerShell;

namespace BuildTools
{
    public abstract class PSPackageTest : IPackageTest
    {
        public string Command { get; }

        protected PSPackageTest(string command)
        {
            Command = command;
        }

        internal static string GetPowerShellExecutable(PSEdition edition)
        {
            if (edition == PSEdition.Core)
                return "pwsh";

            return "powershell";
        }
    }
}
