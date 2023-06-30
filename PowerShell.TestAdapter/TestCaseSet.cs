using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using SMA = System.Management.Automation;

namespace PowerShell.TestAdapter
{
    public class TestCaseSet
    {
        public TestCaseSet(string fileName, string describe)
        {
            File = fileName;
            Describe = describe;
            TestCases = new List<TestCase>();
        }

        public string File { get; }

        public string Describe { get; }

        public List<TestCase> TestCases { get; }

        public List<TestResult> TestResults { get; private set; }

        public void ProcessTestResults(Array results)
        {
            TestResults = new List<TestResult>();

            if (results == null)
                return;

            foreach (var obj in results)
            {
                var psobject = (SMA.PSObject)obj;
                var describe = psobject.Properties["Describe"].Value as string;

                if (!HandleParseError(psobject, describe))
                    break;

                var context = psobject.Properties["Context"].Value as string;
                var name = psobject.Properties["Name"].Value as string;

                if (string.IsNullOrEmpty(context))
                    context = "No Context";

                var testCase = TestCases.FirstOrDefault(m => m.FullyQualifiedName == $"{describe}.{context}.{name}");

                if (testCase != null)
                {
                    var testResult = new TestResult(testCase)
                    {
                        Outcome = GetOutcome(psobject.Properties["Result"].Value as string),
                        ErrorStackTrace = psobject.Properties["StackTrace"].Value as string,
                        ErrorMessage = psobject.Properties["FailureMessage"].Value as string
                    };

                    TestResults.Add(testResult);
                }
            }
        }

        private bool HandleParseError(SMA.PSObject result, string describe)
        {
            if (describe.Contains($"Error in {File}"))
            {
                var errorStackTrace = result.Properties["StackTrace"].Value as string;
                var errorMessage = result.Properties["FailureMessage"].Value as string;

                foreach (var testCase in TestCases)
                {
                    var testResult = new TestResult(testCase)
                    {
                        Outcome = TestOutcome.Failed,
                        ErrorMessage = errorMessage,
                        ErrorStackTrace = errorStackTrace
                    };

                    TestResults.Add(testResult);
                }

                return false;
            }
            return true;
        }

        private TestOutcome GetOutcome(string testResult)
        {
            switch (testResult?.ToLower())
            {
                case null:
                case "":
                    return TestOutcome.NotFound;
                case "passed":
                    return TestOutcome.Passed;
                case "skipped":
                case "pending":
                    return TestOutcome.Skipped;
                default:
                    return TestOutcome.Failed;
            }
        }
    }
}
