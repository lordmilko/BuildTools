using System;
using System.Management.Automation;

namespace BuildTools.PowerShell
{
    class PesterTestResult
    {
        public string Describe { get; }

        public string Context { get; }

        public string Name { get; }

        public string FailureMessage { get; }

        public string Result { get; }

        public TimeSpan Time { get; }

        public PesterTestResult(PSObject pso)
        {
            Describe = (string) pso.Properties[nameof(Describe)].Value;
            Context = (string) pso.Properties[nameof(Context)].Value;
            Name = (string) pso.Properties[nameof(Name)].Value;
            FailureMessage = (string) pso.Properties[nameof(FailureMessage)].Value;
            Result = (string) pso.Properties[nameof(Result)].Value;
            Time = (TimeSpan) pso.Properties[nameof(Time)].Value;
        }
    }
}
