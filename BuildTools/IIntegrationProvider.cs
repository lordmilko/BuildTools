using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    interface IIntegrationProvider : IDynamicParameters
    {
        SwitchParameter Integration { get; set; }

        string[] GetIntegrationParameterSets();
    }
}
