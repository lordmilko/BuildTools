using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Runtime.ExceptionServices;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    public abstract class BuildCmdlet<TEnvironment> : PSCmdlet
    {
        protected const string LegacyParameterName = "Legacy";
        protected const string IntegrationParameterName = "Integration";

        private static readonly MethodInfo getService;

        private bool forceVerbose;

        static BuildCmdlet()
        {
            getService = typeof(BuildCmdlet<TEnvironment>).GetMethod(nameof(GetService), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }

        protected BuildCmdlet(bool forceVerbose = true)
        {
            this.forceVerbose = forceVerbose;
        }

        protected virtual bool IsLegacyMode
        {
            get
            {
                if (MyInvocation.BoundParameters.TryGetValue(LegacyParameterName, out var value))
                    return (SwitchParameter) value;

                return false;
            }
        }

        protected sealed override void ProcessRecord()
        {
            WithActiveCmdlet(() =>
            {
                var verboseEnforcer = forceVerbose ? new VerboseEnforcer(this) : null;

                try
                {
                    ProcessRecordEx();
                }
                finally
                {
                    verboseEnforcer?.Dispose();
                }
            });
        }

        protected sealed override void BeginProcessing()
        {
            Environment.CurrentDirectory = SessionState.Path.CurrentLocation.Path;

            if (this is IIntegrationProvider provider)
            {
                if (MyInvocation.BoundParameters.TryGetValue(IntegrationParameterName, out var value))
                    provider.Integration = (SwitchParameter) value;
            }

            BeginProcessingEx();
        }

        protected virtual void BeginProcessingEx()
        {
        }

        protected abstract void ProcessRecordEx();

        protected virtual T GetService<T>()
        {
            return BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<T>();
        }

        //If you touch this property, implicitly you're a cmdlet that CAN support legacy parameters, the only question is WILL you
        internal static Delegate NeedLegacyParameter => (Func<IPowerShellService, IProjectConfigProvider, bool>) ((powerShell, configProvider) => NeedLegacyParameterInternal(true, powerShell, configProvider));

        private static bool NeedLegacyParameterInternal(bool isLegacyProvider, IPowerShellService powerShell, IProjectConfigProvider configProvider)
        {
            if (isLegacyProvider && powerShell.IsWindows && configProvider.GetProjects(true).Any())
                return true;

            return false;
        }

        //If you touch this property, implicitly you're a cmdlet that CAN support integration parameters, the only question is WILL you
        internal static Delegate NeedIntegrationParameter => (Func<IProjectConfigProvider, bool>) (configProvider => NeedIntegrationParameterInternal(true, configProvider));

        private static bool NeedIntegrationParameterInternal(bool isIntegrationProvider, IProjectConfigProvider configProvider)
        {
            if (isIntegrationProvider)
            {
                if (configProvider.GetProjects(false).Any(a => (a.Kind & ProjectKind.IntegrationTest) != 0))
                    return true;
            }

            return false;
        }

        public object GetDynamicParameters()
        {
            if (!typeof(IEnvironmentIdentifier).IsAssignableFrom(typeof(TEnvironment)))
                return null;

            var dict = new RuntimeDefinedParameterDictionary();

            WithActiveCmdlet((IPowerShellService powerShell, IProjectConfigProvider configProvider) =>
            {
                if (NeedLegacyParameterInternal(this is ILegacyProvider, powerShell, configProvider))
                    AddDynamicParameters(LegacyParameterName, ((ILegacyProvider) this).GetLegacyParameterSets(), dict);

                if (NeedIntegrationParameterInternal(this is IIntegrationProvider, configProvider))
                    AddDynamicParameters(IntegrationParameterName, ((IIntegrationProvider) this).GetIntegrationParameterSets(), dict);
            });

            return dict;
        }

        private void AddDynamicParameters(string parameterName, string[] parameterSets, RuntimeDefinedParameterDictionary dict)
        {
            //Setting the ParameterSetName to null or empty causes the set to revert to AllParameterSets
            if (parameterSets == null || parameterSets.Length == 0)
                parameterSets = new string[] { null };

            foreach (var set in parameterSets)
            {
                dict.Add(parameterName, new RuntimeDefinedParameter(parameterName, typeof(SwitchParameter), new Collection<Attribute>
                {
                    new ParameterAttribute { Mandatory = false, ParameterSetName = set }
                }));
            }
        }

        private void WithActiveCmdlet(Action action) => WithActiveCmdlet((IPowerShellService powerShell) => action());

        private void WithActiveCmdlet<T1>(Action<T1> action) => WithActiveCmdlet((Delegate) action);

        private void WithActiveCmdlet<T1, T2>(Action<T1, T2> action) => WithActiveCmdlet((Delegate) action);

        private void WithActiveCmdlet(Delegate action)
        {
            var parametersTypes = action.Method.GetParameters();

            var parameters = parametersTypes.Select(p => getService.MakeGenericMethod(p.ParameterType).Invoke(this, Array.Empty<object>())).ToArray();

            var powerShell = (PowerShellService) parameters.Single(p => p is IPowerShellService);

            powerShell.Push(this);

            try
            {
                action.DynamicInvoke(parameters);
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            }
            finally
            {
                powerShell.Pop();
            }
        }
    }
}
