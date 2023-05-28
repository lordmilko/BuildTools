using System;
using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "TestResult")]
    [BuildCommand(CommandKind.TestResult, CommandCategory.Version)]
    public abstract class GetTestResult<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        protected override void ProcessRecordEx()
        {
            throw new NotImplementedException();
        }
    }
}