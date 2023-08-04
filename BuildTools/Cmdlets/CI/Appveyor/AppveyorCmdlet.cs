using System;
using System.Linq;
using System.Text;

namespace BuildTools.Cmdlets.Appveyor
{
    public abstract class AppveyorCmdlet : BuildCmdlet<AppveyorEnvironment>
    {
        protected override bool IsLegacyMode => BuildToolsSessionState.AppveyorBuildCore ?? base.IsLegacyMode;

        protected BuildConfiguration Configuration
        {
            get
            {
                var service = GetService<EnvironmentService>();

                if (service.Configuration == null)
                    return BuildConfiguration.Debug;

                return (BuildConfiguration) Enum.Parse(typeof(BuildConfiguration), service.Configuration, true);
            }
        }

        private Type environment;

        protected AppveyorCmdlet() : base(false)
        {
        }

        protected override void BeginProcessingEx()
        {
            environment = GetEnvironment();
        }

        protected void LogHeader(string message)
        {
            var configProvider = GetService<IProjectConfigProvider>();

            var builder = new StringBuilder();

            var supportsLegacy = configProvider.GetProjects(true).Any();

            builder.Append(message);

            if (supportsLegacy)
                builder.AppendFormat(" (Core: {0})", !IsLegacyMode);
        }

        protected override T GetService<T>()
        {
            var env = environment;

            //When auto-completing dynamic parameters, environment won't be set
            if (env == null)
                env = GetEnvironment();

            var serviceProvider = BuildToolsSessionState.ServiceProvider(env);

            return serviceProvider.GetService<T>();
        }

        private Type GetEnvironment()
        {
            if (BuildToolsSessionState.Environments.Length == 0)
                throw new InvalidOperationException("Cannot retrieve Appveyor environment: no session has been registered");

            if (BuildToolsSessionState.Environments.Length > 1)
                throw new InvalidOperationException("Cannot retrieve Appveyor environment: multiple sessions have been registered");

            var env = BuildToolsSessionState.Environments[0];

            return env;
        }

        public string[] GetLegacyParameterSets() => null;
    }
}
