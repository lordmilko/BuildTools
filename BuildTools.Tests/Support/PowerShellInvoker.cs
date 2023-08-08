using System;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using BuildTools.Cmdlets;
using SMA = System.Management.Automation;

namespace BuildTools.Tests
{
    class PowerShellInvoker
    {
        private SMA.PowerShell powerShell;

        public PowerShellInvoker()
        {
            var initial = InitialSessionState.CreateDefault();
            initial.ImportPSModule(new[] { typeof(StartBuildEnvironment).Assembly.Location });

            powerShell = SMA.PowerShell.Create(initial);
        }

        public T[] Invoke<T>(string cmdlet, object param, string inputCmdlet = null)
        {
            return InvokeInternal<T>(() =>
            {
                if (inputCmdlet != null)
                {
                    powerShell.AddCommand(inputCmdlet);
                }

                powerShell.AddCommand(cmdlet);

                AddParameters(param);
            });
        }

        public T[] InvokeScript<T>(string script) => InvokeInternal<T>(() => powerShell.AddScript(script));

        private T[] InvokeInternal<T>(Action addCommands)
        {
            try
            {
                addCommands();

                var result = powerShell.Invoke();

                object[] items;

                if (typeof(T) == typeof(PSObject))
                    items = result.Cast<object>().ToArray();
                else
                    items = result.Select(v => v.BaseObject).ToArray();

                var errors = powerShell.Streams.Error.ToArray();

                if (errors.Length > 0)
                    throw errors[0].Exception;

                return items.Cast<T>().ToArray();
            }
            finally
            {
                powerShell.Commands.Clear();
            }
        }

        private void AddParameters(object param)
        {
            if (param != null)
            {
                if (param.GetType().Name.Contains("AnonymousType"))
                {
                    var properties = param.GetType().GetProperties();

                    foreach (var prop in properties)
                    {
                        var value = prop.GetValue(param);

                        powerShell.AddParameter(prop.Name, value);
                    }
                }
                else
                {
                    //Positional
                    powerShell.AddArgument(param);
                }
            }
        }
    }
}
