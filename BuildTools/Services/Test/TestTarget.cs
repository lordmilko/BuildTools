using System.Linq;

namespace BuildTools
{
    struct TestTarget
    {
        private TestType[] userTypes;
        private TestType[] projectTypes;

        public bool CSharp => HasType(TestType.CSharp);

        public bool PowerShell => HasType(TestType.PowerShell);

        public TestTarget(TestType[] userTypes, TestType[] projectTypes)
        {
            this.userTypes = userTypes;
            this.projectTypes = projectTypes;
        }

        private bool HasType(TestType type)
        {
            if (userTypes != null && userTypes.Length > 0)
                return userTypes.Contains(type);

            return projectTypes == null || projectTypes.Length == 0 || projectTypes.Contains(type);
        }
    }
}
