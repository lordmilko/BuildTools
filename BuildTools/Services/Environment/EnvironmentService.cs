using System;
using BuildTools.PowerShell;
using Env = BuildTools.WellKnownEnvironmentVariable;

namespace BuildTools
{
    class EnvironmentService
    {
        private readonly IPowerShellService powerShell;

        public EnvironmentService(IPowerShellService powerShell)
        {
            this.powerShell = powerShell;
        }

        public bool IsAppveyor => !string.IsNullOrEmpty(Get(Env.Appveyor));
        public bool IsCI => !string.IsNullOrEmpty(Get(Env.CI));

        public string AppveyorBuildFolder => Get(Env.AppveyorBuildFolder);

        public string Configuration => Get(Env.Configuration);

        public string Path
        {
            get => Get(Env.Path);
            private set => Set(Env.Path, value);
        }

        public string ProgramFilesx86 => Get(Env.ProgramFilesx86);

        public string ChocolateyInstall => Get(Env.ChocolateyInstall);

        private string Get(string variable) => Environment.GetEnvironmentVariable(variable);

        private void Set(string variable, string value) => Environment.SetEnvironmentVariable(variable, value);

        public void AppendPath(string item)
        {
            var delim = ':';

            if (powerShell.IsWindows)
                delim = ';';

            Path += $"{delim}{item}";
        }
    }
}
