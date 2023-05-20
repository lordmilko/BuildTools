using System.Collections.Generic;

namespace BuildTools.Tests
{
    class MockFileLogger : IFileLogger
    {
        public List<string> BuildLogs { get; } = new List<string>();

        public void LogBuild(string message)
        {
            BuildLogs.Add(message);
        }
    }
}