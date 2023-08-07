using System.Linq;

namespace BuildTools
{
    struct PackageTarget
    {
        private PackageType[] userTypes;
        private PackageType[] projectTypes;

        public bool CSharp => HasType(PackageType.CSharp);

        public bool PowerShell => HasType(PackageType.PowerShell);

        public bool Redist => HasType(PackageType.Redistributable);

        public PackageTarget(PackageType[] userTypes, PackageType[] projectTypes)
        {
            this.userTypes = userTypes;
            this.projectTypes = projectTypes;
        }

        private bool HasType(PackageType type)
        {
            if (userTypes != null && userTypes.Length > 0)
                return userTypes.Contains(type);

            return projectTypes == null || projectTypes.Length == 0 || projectTypes.Contains(type);
        }
    }
}
