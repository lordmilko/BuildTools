using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Xml.Linq;
using BuildTools.PowerShell;

namespace BuildTools
{
    class GetTestResultService
    {
        internal static readonly XNamespace CSharpTestNamespace = "http://microsoft.com/schemas/VisualStudio/TeamTest/2010";

        private IProjectConfigProvider configProvider;
        private IFileSystemProvider fileSystem;
        private IPowerShellService powerShell;

        public GetTestResultService(
            IProjectConfigProvider configProvider,
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell)
        {
            this.configProvider = configProvider;
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;
        }

        public TestResult[] Execute(TestResultConfig testResultConfig)
        {
            var results = new List<TestResult>();

            //todo: filter and sort

            var name = string.IsNullOrWhiteSpace(testResultConfig.Name) ? "*" : testResultConfig.Name;
            var wildcard = new WildcardPattern(name, WildcardOptions.IgnoreCase);

            Func<IEnumerable<TestResult>, IEnumerable<TestResult>> finalize = list => list.Where(v =>
            {
                if (!wildcard.IsMatch(v.Name))
                    return false;

                if (testResultConfig.Outcome != null)
                {
                    if (v.Outcome != testResultConfig.Outcome.Value)
                        return false;
                }

                return true;
            }).OrderBy(v => v.Name);

            results.AddRange(finalize(GetCSharpTestResults(testResultConfig)));
            results.AddRange(finalize(GetPowerShellTestResults(testResultConfig)));

            return results.ToArray();
        }

        private IEnumerable<TestResult> GetCSharpTestResults(TestResultConfig testResultConfig)
        {
            if (!testResultConfig.Target.CSharp)
                yield break;

            var path = GetOrFilterPath(testResultConfig, TestType.CSharp);

            if (path == null)
                yield break;

            foreach (var item in path)
            {
                var text = fileSystem.GetFileText(item);

                var xml = XDocument.Parse(text);

                //Extract a set of test results from the items in a trx file
                var xmlItems = xml.Descendants(CSharpTestNamespace + "UnitTestResult").ToArray();

                foreach (var xmlItem in xmlItems)
                    yield return TestResult.FromCSharp(xmlItem, item);
            }
        }

        private IEnumerable<TestResult> GetPowerShellTestResults(TestResultConfig testResultConfig)
        {
            if (!testResultConfig.Target.PowerShell)
                return Array.Empty<TestResult>();

            var path = GetOrFilterPath(testResultConfig, TestType.PowerShell);

            if (path == null)
                return Array.Empty<TestResult>();

            var results = new List<TestResult>();

            foreach (var item in path)
            {
                var xml = XDocument.Load(item);

                results.AddRange(ProcessTestSuite(xml.Element("test-results"), item));
            }

            return results.ToArray();
        }

        private IEnumerable<TestResult> ProcessTestSuite(XElement testResults, string path)
        {
            foreach (var suite in testResults.Elements("test-suite").SelectMany(s => s.Elements("results")))
            {
                foreach (var item in ProcessTestSuite(suite, path))
                    yield return item;

                foreach (var testCase in suite.Elements("test-case"))
                    yield return TestResult.FromPowerShell(testCase, path);
            }
        }

        private string[] GetOrFilterPath(TestResultConfig testResultConfig, TestType type)
        {
            var filter = $"*{type.GetDescription(false)}*";

            var path = testResultConfig.Path;

            if (path == null || path.Length == 0)
            {
                var match = List(filter, testResultConfig.Integration, true).OrderByDescending(v => v.LastWriteTime).FirstOrDefault();

                if (match == null)
                    return null;

                path = new[] { match.FullName };
            }
            else
            {
                var wildcard = new WildcardPattern(filter, WildcardOptions.IgnoreCase);

                path = path.Where(v => wildcard.IsMatch(Path.GetFileName(v))).ToArray();
            }

            return path;
        }

        public FileInfo[] List(string name, bool integration, bool warnIfNoResults = false)
        {
            var projectDir = (integration ? configProvider.GetIntegrationTestProject(false) : configProvider.GetUnitTestProject(false)).DirectoryName;
            var testResultsDir = Path.Combine(projectDir, "TestResults");

            if (!fileSystem.DirectoryExists(testResultsDir))
                throw new DirectoryNotFoundException($"Unable to retrieve test results as test results folder '{testResultsDir}' does not exist");

            name = string.IsNullOrWhiteSpace(name) ? "*" : name;

            var results = fileSystem.EnumerateFiles(testResultsDir, name).ToArray();

            if (results.Length == 0)
            {
                if (warnIfNoResults)
                {
                    if (name == "*")
                        powerShell.WriteWarning("Unable to retrieve results as no test files are available.");
                    else
                        powerShell.WriteWarning($"Unable to retrieve test results as no test result files exist in folder '{testResultsDir}' that match the wildcard '{name}'");
                }
                else
                    throw new InvalidOperationException($"Unable to retrieve test results as no test result files exist in folder '{testResultsDir}' that match the wildcard '{name}'");
            }

            return results.Select(v => new FileInfo(v)).ToArray();
        }
    }
}
