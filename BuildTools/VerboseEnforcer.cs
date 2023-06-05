using System;
using System.Management.Automation;
using System.Reflection;
using BuildTools.Reflection;

namespace BuildTools.Cmdlets
{
    class VerboseEnforcer : IDisposable
    {
        private PSCmdlet cmdlet;
        private PropertyInfo verbosePreferenceInfo;
        private bool originalValue;

        public VerboseEnforcer(PSCmdlet cmdlet)
        {
            this.cmdlet = cmdlet;

            verbosePreferenceInfo = cmdlet.CommandRuntime.GetInternalPropertyInfo("Verbose");

            originalValue = (bool) verbosePreferenceInfo.GetValue(cmdlet.CommandRuntime);

            if (!originalValue)
                verbosePreferenceInfo.SetValue(cmdlet.CommandRuntime, true);
        }

        public void Dispose()
        {
            if (!originalValue)
                verbosePreferenceInfo.SetValue(cmdlet.CommandRuntime, false);
        }
    }
}