using System;
using System.Collections.Generic;

namespace BuildTools
{
    class EnvironmentScope : IDisposable
    {
        private Dictionary<string, string> originalValues = new Dictionary<string, string>();

        public void SetValue(string variable, string value)
        {
            if (!originalValues.TryGetValue(variable, out _))
            {
                var existing = Environment.GetEnvironmentVariable(variable);
                originalValues[variable] = existing;
            }

            Environment.SetEnvironmentVariable(variable, value);
        }

        public void Dispose()
        {
            foreach (var kv in originalValues)
                Environment.SetEnvironmentVariable(kv.Key, kv.Value);
        }
    }
}
