﻿using System.ComponentModel;

namespace BuildTools
{
    enum VersionType
    {
        [Description("Version used when creating nupkg files")]
        Package,

        [Description("Assembly Version used with assemblies")]
        Assembly,

        [Description("Assembly File Version used with assemblies")]
        File,

        [Description("PowerShell Module version")]
        Module,

        [Description("PowerShell Module Release Tag")]
        ModuleTag,

        [Description("Version of previous GitHub Release")]
        PreviousTag
    }
}
