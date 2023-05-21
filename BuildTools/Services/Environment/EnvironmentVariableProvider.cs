using System;

namespace BuildTools
{
    interface IEnvironmentVariableProvider
    {
        string GetValue(string variable);

        void SetValue(string variable, string value);
    }

    class EnvironmentVariableProvider : IEnvironmentVariableProvider
    {
        public string GetValue(string variable) => Environment.GetEnvironmentVariable(variable);

        public void SetValue(string variable, string value) => Environment.SetEnvironmentVariable(variable, value);
    }
}