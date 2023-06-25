using System.Linq;

namespace BuildTools
{
    struct PackageTarget
    {
        private PackageType[] types;

        public bool CSharp => HasType(PackageType.CSharp);

        public bool PowerShell => HasType(PackageType.PowerShell);

        public bool Redist => HasType(PackageType.Redistributable);

        public PackageTarget(PackageType[] types)
        {
            this.types = types;
        }

        private bool HasType(PackageType type) => types.Length == 0 || types.Contains(type);
    }
}