using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using BuildTools.PowerShell;

namespace BuildTools.Cmdlets
{
    public abstract class BuildCmdlet<TEnvironment> : PSCmdlet
    {
        protected const string LegacyParameterName = "Legacy";

        protected bool IsLegacyMode
        {
            get
            {
                if (MyInvocation.BoundParameters.TryGetValue(LegacyParameterName, out var value))
                    return (bool)value;

                return false;
            }
        }

        protected sealed override void ProcessRecord()
        {
            WithActiveCmdlet(_ =>
            {
                using (new VerboseEnforcer(this))
                    ProcessRecordEx();
            });
        }

        protected abstract void ProcessRecordEx();

        protected virtual T GetService<T>()
        {
            return BuildToolsSessionState.ServiceProvider<TEnvironment>().GetService<T>();
        }

        //If you touch this property, implicitly you're a cmdlet that CAN support legacy parameters, the only question is WILL you
        internal static Delegate NeedLegacyParameter => (Func<IPowerShellService, bool>) (powerShell => NeedLegacyParameterInternal(true, powerShell));

        private static bool NeedLegacyParameterInternal(bool isLegacyProvider, IPowerShellService powerShell)
        {
            if (isLegacyProvider && powerShell.IsWindows)
                return true;

            return false;
        }

        public object GetDynamicParameters()
        {
            if (!typeof(IEnvironmentIdentifier).IsAssignableFrom(typeof(TEnvironment)))
                return null;

            var dict = new RuntimeDefinedParameterDictionary();

            WithActiveCmdlet(powerShell =>
            {
                if (NeedLegacyParameterInternal(this is ILegacyProvider, powerShell))
                {
                    dict.Add(LegacyParameterName, new RuntimeDefinedParameter(LegacyParameterName, typeof(SwitchParameter), new Collection<Attribute>
                    {
                        new ParameterAttribute { Mandatory = false }
                    }));
                }
            });

            return dict;
        }

        internal void WithActiveCmdlet(Action<IPowerShellService> action)
        {
            var powerShell = (PowerShellService) GetService<IPowerShellService>();

            powerShell.Push(this);

            try
            {
                action(powerShell);
            }
            finally
            {
                powerShell.Pop();
            }
        }
    }
}
