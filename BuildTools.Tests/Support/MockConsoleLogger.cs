using System;
using System.Collections.Generic;

namespace BuildTools.Tests
{
    class MockConsoleLogger : IConsoleLogger
    {
        public List<string> Logs { get; } = new List<string>();

        public void Log(string message, ConsoleColor? color)
        {
            Logs.Add(message);
        }
    }
}
