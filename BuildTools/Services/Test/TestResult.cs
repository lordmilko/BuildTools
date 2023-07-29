using System;
using System.Xml.Linq;

namespace BuildTools
{
    class TestResult
    {
        public string Name { get; private set; }

        public TestOutcome Outcome { get; private set; }

        public TimeSpan Duration { get; private set; }

        public string Type { get; private set; }

        public string Message { get; private set; }

        public string StackTrace { get; private set; }

        public string File { get; }

        public static TestResult FromCSharp(XElement elm, string path)
        {
            var ns = GetTestResultService.CSharpTestNamespace;

            var errorInfo = elm.Element(ns + "Output")?.Element(ns + "ErrorInfo");

            var result = new TestResult(path)
            {
                Name = elm.Attribute("testName").Value,
                Outcome = GetTestOutcome(elm.Attribute("outcome").Value),
                Duration = TimeSpan.FromMilliseconds((int)TimeSpan.Parse(elm.Attribute("duration").Value).TotalMilliseconds),
                Type = "C#",

                Message = errorInfo?.Element(ns + "Message")?.Value,
                StackTrace = errorInfo?.Element(ns + "StackTrace")?.Value
            };

            return result;
        }

        public static TestResult FromPowerShell(XElement elm, string path)
        {
            var name = elm.Attribute("name").Value;

            var period = name.IndexOf('.');

            if (period != -1)
                name = name.Remove(period, 1).Insert(period, ": ");

            var failure = elm.Element("failure");

            var result = new TestResult(path)
            {
                Name = name,
                Outcome = GetTestOutcome(elm.Attribute("result").Value),
                Duration = TimeSpan.FromSeconds(Math.Round(Convert.ToDouble(elm.Attribute("time").Value), 3)),
                Type = "PS",
                Message = failure?.Element("message")?.Value,
                StackTrace = failure?.Element("stack-trace")?.Value
            };

            return result;
        }

        private TestResult(string path)
        {
            File = path;
        }

        private static TestOutcome GetTestOutcome(string outcome)
        {
            switch (outcome)
            {
                case "Success":
                case "Passed":
                    return TestOutcome.Success;

                case "Failure":
                case "Failed":
                    return TestOutcome.Failed;

                case "NotExecuted":
                    return TestOutcome.Skipped;

                default:
                    throw new NotImplementedException($"Don't know how to format outcome '{outcome}'");
            }
        }
    }
}
