using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class PowerShellPackageSourceService : PackageSourceService
    {
        public PowerShellPackageSourceService(
            IPowerShellService powerShell,
            IFileSystemProvider fileSystem,
            Logger logger) : base(PackageType.PowerShell, powerShell, fileSystem, logger)
        {
        }

        protected override string[] GetPackageSource() => PowerShell.GetPSRepository().Select(v => v.Name).ToArray();

        protected override void RegisterPackageSource() => PowerShell.RegisterPSRepository();

        protected override void UnregisterPackageSource() => PowerShell.UnregisterPSRepository();
    }
}