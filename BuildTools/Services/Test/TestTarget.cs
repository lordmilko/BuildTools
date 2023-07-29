using System.Linq;

namespace BuildTools
{
    struct TestTarget
    {
        private TestType[] types;

        public bool CSharp => HasType(TestType.CSharp);

        public bool PowerShell => HasType(TestType.PowerShell);

        public TestTarget(TestType[] types)
        {
            this.types = types;
        }

        private bool HasType(TestType type) => types.Length == 0 || types.Contains(type);
    }
}