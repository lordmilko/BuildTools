using System;
using System.Collections.Generic;

namespace BuildTools.Tests
{
    class MockEnvironmentVariableProvider : IEnvironmentVariableProvider
    {
        private Dictionary<string, string> dict = new Dictionary<string, string>();

        public MockEnvironmentVariableProvider()
        {
            dict[WellKnownEnvironmentVariable.ProgramFilesx86] = "C:\\Program Files (x86)";
        }

        public string GetValue(string variable)
        {
            if (dict.TryGetValue(variable, out var value))
                return value;

            throw new InvalidOperationException($"Environment Variable '{variable}' has not been set");
        }

        public void SetValue(string variable, string value)
        {
            dict[variable] = value;
        }
    }
}