using BuildTools.PowerShell;

namespace BuildTools
{
    interface IHelpService
    {
        void RegisterHelp(IPowerShellModule module);
    }
}