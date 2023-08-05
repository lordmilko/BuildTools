using System;
using BuildTools.PowerShell;
using Env = BuildTools.WellKnownEnvironmentVariable;

namespace BuildTools
{
    class EnvironmentService
    {
        private readonly IEnvironmentVariableProvider envProvider;
        private readonly IPowerShellService powerShell;

        public EnvironmentService(
            IEnvironmentVariableProvider envProvider,
            IPowerShellService powerShell)
        {
            this.envProvider = envProvider;
            this.powerShell = powerShell;

            //DotnetDependencyInstaller depends on EnvironmentService, so we know this environment variable
            //will get initialized before we start invoking dotnet commands
            DotnetSkipFirstTimeExperience = "1";
        }

        public bool IsAppveyor => !string.IsNullOrEmpty(Get(Env.Appveyor));
        public bool IsCI => !string.IsNullOrEmpty(Get(Env.CI));

        public string AppveyorAccountName => Get(Env.AppveyorAccountName);

        public string AppveyorAPIToken => Get(Env.AppveyorAPIToken);

        public string AppveyorBuildFolder => Get(Env.AppveyorBuildFolder);

        public int AppveyorBuildNumber
        {
            get
            {
                var build = Get(Env.AppveyorBuildNumber);

                if (string.IsNullOrEmpty(build))
                    return -1;

                return Convert.ToInt32(build);
            }
        }

        public string AppveyorBuildVersion => Get(Env.AppveyorBuildVersion);

        public string AppveyorProjectSlug => Get(Env.AppveyorProjectSlug);

        public string AppveyorRepoCommitMessage => Get(Env.AppveyorRepoCommitMessage);

        public string AppveyorRepoCommitMessageExtended => Get(Env.AppveyorRepoCommitMessageExtended);

        public string Configuration => Get(Env.Configuration);

        public string Path
        {
            get => Get(Env.Path);
            private set => Set(Env.Path, value);
        }

        public string ProgramFilesx86 => Get(Env.ProgramFilesx86);

        public string ChocolateyInstall => Get(Env.ChocolateyInstall);

        public string DotnetSkipFirstTimeExperience
        {
            get => Get(Env.DotnetSkipFirstTimeExperience);
            set => Set(Env.DotnetSkipFirstTimeExperience, value);
        }

        private string Get(string variable) => envProvider.GetValue(variable);

        private void Set(string variable, string value) => envProvider.SetValue(variable, value);

        public void AppendPath(string item)
        {
            var delim = ':';

            if (powerShell.IsWindows)
                delim = ';';

            Path += $"{delim}{item}";
        }
    }
}
