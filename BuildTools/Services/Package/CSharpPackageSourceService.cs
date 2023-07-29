using System.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class CSharpPackageSourceService : PackageSourceService
    {
        public CSharpPackageSourceService(
            IPowerShellService powerShell,
            IFileSystemProvider fileSystem,
            Logger logger) : base(PackageType.CSharp, powerShell, fileSystem, logger)
        {
        }

        protected override string[] GetPackageSource() => PowerShell.GetPackageSource().Select(v => v.Name).ToArray();

        protected override void RegisterPackageSource() => PowerShell.RegisterPackageSource();

        protected override void UnregisterPackageSource() => PowerShell.UnregisterPackageSource();
    }
}