using System.Management.Automation;

namespace BuildTools.Cmdlets
{
    interface ILegacyProvider : IDynamicParameters
    {
        string[] GetLegacyParameterSets();
    }
}