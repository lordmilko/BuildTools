using System;

namespace BuildTools.Cmdlets.CI
{
    public abstract class BaseCICmdlet<TEnvironment> : BuildCmdlet<TEnvironment>
    {
        public virtual BuildConfiguration Configuration
        {
            get
            {
                var service = GetService<EnvironmentService>();

                if (service.Configuration == null)
                    return BuildConfiguration.Debug;

                return (BuildConfiguration) Enum.Parse(typeof(BuildConfiguration), service.Configuration, true);
            }
            set => throw new NotSupportedException();
        }

        private Type environment;

        protected BaseCICmdlet() : base(false)
        {
        }

        protected override void BeginProcessingEx()
        {
            environment = GetEnvironment();
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
            var name = typeof(TEnvironment).Name.Replace("Environment", string.Empty);

            if (BuildToolsSessionState.Environments.Length == 0)
                throw new InvalidOperationException($"Cannot retrieve {name} environment: no session has been registered");

            if (BuildToolsSessionState.Environments.Length > 1)
                throw new InvalidOperationException($"Cannot retrieve {name} environment: multiple sessions have been registered");

            var env = BuildToolsSessionState.Environments[0];

            return env;
        }
    }
}
