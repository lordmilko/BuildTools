using System.ComponentModel;

namespace BuildTools
{
    public enum PackageType
    {
        [Description("C#")]
        CSharp,

        PowerShell,

        [Description("Redist")]
        Redistributable
    }
}