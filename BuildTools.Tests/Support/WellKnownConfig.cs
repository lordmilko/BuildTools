using System.Collections.Generic;
using System.Management.Automation;

namespace BuildTools.Tests
{
    static class WellKnownConfig
    {
        public static ProjectConfigBuilder PrtgAPI => ProjectConfigBuilder.Empty
            .WithName("PrtgAPI")

            .WithSolutionName("PrtgAPI.sln")

            .WithBuildFilter("PrtgAPI.*")

            .WithCmdletPrefix("Prtg")

            .WithPowerShellMultiTargeted(true)

            .WithPrompt("PrtgAPI")

            .WithTestTypes(TestType.CSharp, TestType.PowerShell)

            .WithCopyright("lordmilko, 2015")

            .WithDebugTargetFramework("netstandard2.0")

            .WithPackageTests(new Dictionary<string, IPackageTest[]>
            {
                {
                    "C#",
                    new IPackageTest[]
                    {
                        new ScriptPackageTest("[PrtgAPI.AuthMode]::Password", "Password")
                    }
                },
                {
                    "PowerShell",
                    new IPackageTest[]
                    {
                        new PSCommandPackageTest("Get-Sensor", "You are not connected to a PRTG Server. Please connect first using Connect-PrtgServer.", CommandTypes.Cmdlet),
                        new PSCommandPackageTest("(New-Credential a b).ToString()", "System.Management.Automation.PSCredential", CommandTypes.Function),
                        new PSExportPackageTest("Get-Sensor", CommandTypes.Cmdlet),
                        new PSExportPackageTest("Add-Trigger", CommandTypes.Alias)
                    }
                }
            })

            .WithPackageFiles(new Dictionary<string, object[]>
            {
                {
                    "C#",
                    new object[]
                    {
                        "lib\\net452\\PrtgAPI.dll",
                        "lib\\net452\\PrtgAPI.xml",
                        "package\\*",
                        "_rels\\*",
                        "PrtgAPI.nuspec",
                        "[Content_Types].xml",

                        new PackageFileItem("LICENSE", ctx => !ctx.IsLegacy),
                        new PackageFileItem("lib\\netstandard2.0\\PrtgAPI.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("lib\\netstandard2.0\\PrtgAPI.xml", ctx => ctx.IsMultiTargeting),
                    }
                },
                {
                    "PowerShell",
                    new object[]
                    {
                        "Functions\\New-Credential.ps1",
                        "package\\*",
                        "_rels\\*",
                        "PrtgAPI.nuspec",
                        "about_ChannelSettings.help.txt",
                        "about_ObjectSettings.help.txt",
                        "about_PrtgAPI.help.txt",
                        "about_SensorParameters.help.txt",
                        "about_SensorSettings.help.txt",
                        "PrtgAPI.Format.ps1xml",
                        "PrtgAPI.Types.ps1xml",
                        "PrtgAPI.psd1",
                        "PrtgAPI.psm1",
                        "[Content_Types].xml",

                        new PackageFileItem("coreclr\\PrtgAPI.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("coreclr\\PrtgAPI.PowerShell.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("fullclr\\PrtgAPI.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("fullclr\\PrtgAPI.PowerShell.dll", ctx => ctx.IsMultiTargeting),

                        new PackageFileItem("PrtgAPI.dll", ctx => ctx.IsDebug || ctx.IsLegacy),
                        new PackageFileItem("PrtgAPI.PowerShell.dll", ctx => ctx.IsDebug || ctx.IsLegacy),

                        new PackageFileItem("PrtgAPI.PowerShell.dll-Help.xml", ctx => ctx.DebugTargetFramework.StartsWith("net4") || ctx.IsRelease || ctx.IsLegacy),
                    }
                },
                {
                    "Redist",
                    new object[]
                    {
                        "Functions\\New-Credential.ps1",
                        "about_ChannelSettings.help.txt",
                        "about_ObjectSettings.help.txt",
                        "about_PrtgAPI.help.txt",
                        "about_SensorParameters.help.txt",
                        "about_SensorSettings.help.txt",
                        "PrtgAPI.cmd",
                        "PrtgAPI.Format.ps1xml",
                        "PrtgAPI.Types.ps1xml",
                        "PrtgAPI.psd1",
                        "PrtgAPI.psm1",
                        "PrtgAPI.sh",

                        new PackageFileItem("fullclr\\PrtgAPI.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("fullclr\\PrtgAPI.pdb", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("fullclr\\PrtgAPI.xml", ctx => ctx.IsMultiTargeting),

                        new PackageFileItem("fullclr\\PrtgAPI.PowerShell.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("fullclr\\PrtgAPI.PowerShell.pdb", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("fullclr\\PrtgAPI.PowerShell.xml", ctx => ctx.IsMultiTargeting),

                        new PackageFileItem("coreclr\\PrtgAPI.deps.json", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("coreclr\\PrtgAPI.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("coreclr\\PrtgAPI.pdb", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("coreclr\\PrtgAPI.xml", ctx => ctx.IsMultiTargeting),

                        new PackageFileItem("coreclr\\PrtgAPI.PowerShell.deps.json", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("coreclr\\PrtgAPI.PowerShell.dll", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("coreclr\\PrtgAPI.PowerShell.pdb", ctx => ctx.IsMultiTargeting),
                        new PackageFileItem("coreclr\\PrtgAPI.PowerShell.xml", ctx => ctx.IsMultiTargeting),

                        new PackageFileItem("PrtgAPI.PowerShell.deps.json", ctx => !ctx.IsLegacy && ctx.IsDebug),

                        new PackageFileItem("PrtgAPI.dll", ctx => ctx.IsDebug || ctx.IsLegacy),
                        new PackageFileItem("PrtgAPI.pdb", ctx => ctx.IsDebug || ctx.IsLegacy),
                        new PackageFileItem("PrtgAPI.xml", ctx => ctx.IsDebug || ctx.IsLegacy),
                        new PackageFileItem("PrtgAPI.PowerShell.dll", ctx => ctx.IsDebug || ctx.IsLegacy),
                        new PackageFileItem("PrtgAPI.PowerShell.pdb", ctx => ctx.IsDebug || ctx.IsLegacy),
                        new PackageFileItem("PrtgAPI.PowerShell.xml", ctx => ctx.IsDebug || ctx.IsLegacy),

                        new PackageFileItem("PrtgAPI.PowerShell.dll-Help.xml", ctx => ctx.DebugTargetFramework.StartsWith("net4") || ctx.IsRelease || ctx.IsLegacy),
                    }
                }
            });
    }
}
