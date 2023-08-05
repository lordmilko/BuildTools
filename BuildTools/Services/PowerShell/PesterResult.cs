using System.Linq;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    class PesterResult
    {
        public int FailedCount { get; }
        public int PassedCount { get; }
        public int PendingCount { get; }
        public int SkippedCount { get; }

        public PesterTestResult[] TestResult { get; }

        public PesterResult(PSObject pso)
        {
            FailedCount = (int)pso.Properties[nameof(FailedCount)].Value;
            PassedCount = (int) pso.Properties[nameof(PassedCount)].Value;
            PendingCount = (int) pso.Properties[nameof(PendingCount)].Value;
            SkippedCount = (int) pso.Properties[nameof(SkippedCount)].Value;

            TestResult = ((object[]) pso.Properties[nameof(TestResult)].Value).Cast<PSObject>().Select(v => new PesterTestResult(v)).ToArray();
        }
    }
}
