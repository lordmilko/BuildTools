﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BuildTools.PowerShell;

namespace BuildTools
{
    internal class NewBuildEnvironmentService
    {
        private readonly IFileSystemProvider fileSystem;
        private readonly IProcessService processService;
        private readonly IPowerShellService powerShell;

        public NewBuildEnvironmentService(
            IFileSystemProvider fileSystem,
            IProcessService processService,
            IPowerShellService powerShell)
        {
            this.fileSystem = fileSystem;
            this.processService = processService;
            this.powerShell = powerShell;
        }

        public string[] Execute(string path, bool force, IConfigSettingValueProvider valueProvider)
        {
            CalculateSolutionPaths(path, out var solutionRoot, out var buildFolder);

            var results = new List<string>
            {
                GenerateBuildCmd(solutionRoot, force, false),
                GenerateBuildBash(solutionRoot, force),
                GenerateBootstrap(solutionRoot, buildFolder, force),
                GenerateConfig(solutionRoot, buildFolder, force, valueProvider),
                GenerateVersion(buildFolder, force),
                GenerateAppveyor(solutionRoot, force)
            };

            return results.Where(v => v != null).ToArray();
        }

        private string GenerateBuildCmd(string solutionRoot, bool force, bool core)
        {
            var exe = core ? "pwsh" : "powershell";

            var str = $@"
start {exe} -executionpolicy bypass -noexit -noninteractive -command ""ipmo psreadline; . '%~dp0Build\Bootstrap.ps1'""";

            return WriteFile(solutionRoot, "build.cmd", str, force, new UTF8Encoding(false));
        }

        private string GenerateBuildBash(string solutionRoot, bool force)
        {
            var str = @"
#!/bin/bash

BASEDIR=""$(dirname ""$BASH_SOURCE"")""
pwsh -executionpolicy bypass -noexit -noninteractive -command ""ipmo psreadline; . '$BASEDIR/build/Bootstrap.ps1'""";

            var result = WriteFile(solutionRoot, "build.sh", str, force, windows: false);

            if (result != null)
            {
                TryExecuteGit(
                    solutionRoot,
                    new ArgList
                    {
                        "-C",
                        solutionRoot,
                        "update-index",
                        "--chmod=+x",
                        "--add",
                        result
                    },
                    out _
                );
            }

            return result;
        }

        private bool TryExecuteGit(string solutionRoot, ArgList args, out string[] result)
        {
            if (solutionRoot == null)
                throw new ArgumentNullException(nameof(solutionRoot));

            var gitDir = Path.Combine(solutionRoot, ".git");

            if (fileSystem.DirectoryExists(gitDir))
            {
                var git = powerShell.GetCommand("git");

                if (git != null)
                {
                    result = processService.Execute("git", args);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private string GenerateBootstrap(string solutionRoot, string buildFolder, bool force)
        {
            var items = new List<string>();

            var location = GetType().Assembly.Location;

            string devSwitch = string.Empty;

            if (location.StartsWith(solutionRoot, StringComparison.OrdinalIgnoreCase))
            {
                items.Add(GenerateSelfBootstrap());

                var devBootstrap = new StringBuilder();

                for (var i = 0; i < items.Count; i++)
                {
                    devBootstrap.Append("        ").Append(items[i].TrimStart());

                    if (i < items.Count - 1)
                        devBootstrap.AppendLine().AppendLine();
                }

                devSwitch = $@"if($env:LORDMILKO_BUILDTOOLS_DEVELOPMENT)
{{
    switch($env:LORDMILKO_BUILDTOOLS_DEVELOPMENT)
    {{
{devBootstrap}
    }}
}}
else";
            }

            var str = $@"
param(
    [Parameter(Mandatory = $false)]
    [switch]$Quiet
)

{devSwitch}if(!(Get-Module lordmilko.BuildTools))
{{
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    if(!(Get-Module -ListAvailable lordmilko.BuildTools))
    {{
        Write-Host ""Installing lordmilko.BuildTools..."" -NoNewline -ForegroundColor Magenta

        Register-PackageSource -Name AppveyorBuildToolsNuGet -Location https://ci.appveyor.com/nuget/buildtools-j7nyox2i4tis -ProviderName PowerShellGet | Out-Null

        Install-Package lordmilko.BuildTools -ForceBootstrap -Force -Source AppveyorBuildToolsNuGet -ErrorAction Stop | Out-Null

        Unregister-PackageSource -Name AppveyorBuildToolsNuGet

        Write-Host ""Done!"" -ForegroundColor Magenta
    }}
    
    Import-Module lordmilko.BuildTools -Scope Local
}}

Start-BuildEnvironment $PSScriptRoot -CI:(!!$env:CI) -Quiet:$Quiet";

            return WriteFile(buildFolder, "Bootstrap.ps1", str, force);
        }

        private string GenerateSelfBootstrap()
        {
            return @"
        ""SelfBootstrap"" {
            dotnet build $PSScriptRoot\..\BuildTools\BuildTools.csproj -c Release

            $fileStream = [IO.File]::OpenRead(""$PSScriptRoot\..\BuildTools\bin\Release\net461\lordmilko.BuildTools\BuildTools.dll"")

            try
            {
                $memoryStream = New-Object System.IO.MemoryStream
                $fileStream.CopyTo($memoryStream)

                $dllBytes = $memoryStream.ToArray()

                $assembly = [System.Reflection.Assembly]::Load($dllBytes)

                Import-Module $assembly
            }
            finally
            {
                $fileStream.Dispose()
            }
        }";
        }

        private string GenerateAppveyorArtifactBootstrap()
        {
            return @"
        ""AppveyorArtifact"" {
            if(!(Get-Command New-BuildEnvironment -ErrorAction SilentlyContinue))
            {
                $bytes = (Invoke-WebRequest https://ci.appveyor.com/api/projects/lordmilko/buildtools/artifacts/lordmilko.BuildTools.zip -UseBasicParsing).Content

                $stream = New-Object System.IO.MemoryStream (,$bytes)

                if($PSEdition -eq ""Desktop"")
                {
                    Add-Type -AssemblyName System.IO.Compression
                }

                $archive = [System.IO.Compression.ZipArchive]::new($stream)

                $dllEntry = $archive.Entries|where Fullname -eq ""fullclr/BuildTools.dll""

                $entryStream = $dllEntry.Open()

                try
                {
                    $entryMemStream = New-Object System.IO.MemoryStream
                    $entryStream.CopyTo($entryMemStream)

                    $dllBytes = $entryMemStream.ToArray()

                    $assembly = [System.Reflection.Assembly]::Load($dllBytes)

                    Import-Module $assembly
                }
                finally
                {
                    $entryStream.Dispose()
                }
            }
        }";
        }

        private string GenerateConfig(string solutionRoot, string buildFolder, bool force, IConfigSettingValueProvider valueProvider)
        {
            var name = "Build.psd1";

            if (fileSystem.DirectoryExists(buildFolder))
            {
                //Check if there's already a config file with an alternate name, e.g. PrtgAPI.psd1 instead of Build.psd1
                var existing = fileSystem.EnumerateFiles(buildFolder, "*.psd1").ToArray();

                if (existing.Length == 1 && Path.GetFileNameWithoutExtension(existing[0]) == Path.GetFileName(Path.GetDirectoryName(buildFolder)))
                    name = Path.GetFileName(existing[0]);
            }

            var str = CreateConfigContents(valueProvider, solutionRoot);

            return WriteFile(buildFolder, name, str, force);
        }

        private string GenerateVersion(string buildFolder, bool force)
        {
            var str = @"
<!-- This code was generated by a tool. Any changes made manually will be lost -->
<!-- the next time this code is regenerated. -->

<Project>
  <PropertyGroup>
    <Version>0.1.0</Version>
    <AssemblyVersion>0.1.0.0</AssemblyVersion>
    <FileVersion>0.1.0.0</FileVersion>
    <InformationalVersion>0.1.0</InformationalVersion>
  </PropertyGroup>
</Project>
";
            return WriteFile(buildFolder, "Version.props", str, force);
        }

        public string CreateConfigContents(IConfigSettingValueProvider valueProvider, string solutionRoot = null)
        {
            Func<string, IConfigValue> stringValue = valueProvider.String;
            Func<string, IConfigValue> arrayValue = valueProvider.Array;
            Func<string, IConfigValue> hashTableValue = valueProvider.HashTable;
            Func<string, IConfigValue> nullValue = valueProvider.Null;
            Func<string, IConfigValue> boolValue = valueProvider.Bool;

            string GetEnumNames<T>(params T[] exclude) where T : Enum
            {
                var values = Enum.GetValues(typeof(T)).Cast<T>().Where(v => !exclude.Contains(v)).ToArray();

                var strs = values.Select(v => v.GetDescription(false));

                return string.Join(", ", strs);
            }

            IConfigValue nameValue(string v)
            {
                var proposedValue = stringValue(v);

                if (proposedValue.IsDefault && solutionRoot != null)
                {
                    var candidates = fileSystem.EnumerateFiles(solutionRoot, "*.sln").ToArray();

                    if (candidates.Length == 1)
                    {
                        var result = Path.GetFileNameWithoutExtension(candidates[0]);

                        return new CustomConfigValue($"'{result}'");
                    }
                }

                return proposedValue;
            }

            IConfigValue copyrightValue(string v)
            {
                var proposedValue = stringValue(v);

                if (proposedValue.IsDefault && !string.IsNullOrEmpty(solutionRoot))
                {
                    var args = new ArgList {"config", "--get", "user.name"};

                    if (TryExecuteGit(solutionRoot, args, out var result) && result.Length == 1)
                        return new CustomConfigValue($"'{result[0]}, {DateTime.Now.Year}'");
                }

                return proposedValue;
            }

            var groups = new[]
            {
                new ConfigGroup("Global", new[]
                {
                    new ConfigSetting("Name",                        required: true,  value: nameValue,      description: "The name of the project/GitHub repository"),
                    new ConfigSetting("CmdletPrefix",                required: true,  value: stringValue,    description: "The prefix to use for all build environment cmdlets"),
                    new ConfigSetting("Copyright",                   required: true,  value: copyrightValue, description: "The copyright author and year to display in the build environment"),
                    new ConfigSetting("SolutionName",                required: false, value: stringValue,    description: "The name of the Visual Studio Solution. Required when a project contains multiple solutions"),
                    new ConfigSetting("BuildFilter",                 required: false, value: stringValue,    description: "A wildcard expression indicating the projects that should be built in CI"),
                    new ConfigSetting("DebugTargetFramework",        required: false, value: stringValue,    description: "The target framework that is used in debug mode when the project conditionally multi-targets only on Release"),
                    new ConfigSetting("Features",                    required: false, value: stringValue,    description: $"Features to enable in the build environment. By default all features are allowed, and can be negated with ~. Valid values include: {GetEnumNames<Feature>(Feature.System)}"),
                    new ConfigSetting("Commands",                    required: false, value: stringValue,    description: $"Commands to enable in the build environment. By default all commands are allowed, and can be negated with ~. Valid values include: {GetEnumNames<CommandKind>()}"),
                    new ConfigSetting("Prompt",                      required: false, value: stringValue,    description: "The value to use for the prompt in the build environment. If not specified, Name will be used"),
                    new ConfigSetting("SourceFolder",                required: false, value: stringValue,    description: "The name of the folder that the source code is contained in. If not specified, will automatically be calculated"),
                    new ConfigSetting("CoverageThreshold",           required: false, value: stringValue,    description: "The minimum coverage threshold that must be met under CI"),
                }),
                new ConfigGroup("CSharp", new[]
                {
                    new ConfigSetting("CSharpLegacyPackageExcludes", required: false, value: arrayValue,     description: "Files to exclude from the C# NuGet Package when building legacy packages")
                }),
                new ConfigGroup("PowerShell", new[]
                {
                    new ConfigSetting("PowerShellMultiTargeted",     required: false, value: boolValue,      description: "Indicates that a PowerShell package should be built containing both coreclr and fullclr subfolders"),
                    new ConfigSetting("PowerShellModuleName",        required: false, value: stringValue,    description: "The name of the PowerShell module. If not specified, Name will be used"),
                    new ConfigSetting("PowerShellProjectName",       required: false, value: stringValue,    description: "The name of the PowerShell project. If not specified, will automatically be calculated"),
                    new ConfigSetting("PowerShellUnitTestFilter",    required: false, value: nullValue,      description: "A ScriptBlock that takes a FileInfo/DirectoryInfo as $_ and returns whether or not to process unit tests for that file/folder")
                }),
                new ConfigGroup("Test", new[]
                {
                    new ConfigSetting("TestTypes",                   required: false, value: arrayValue,     description: "The languages to perform unit tests for. If not specified, C# and PowerShell will be tested"),
                    new ConfigSetting("UnitTestProjectName",         required: false, value: stringValue,    description: "The name of the Unit Test project. If not specified, will automatically be calculated")
                }),
                new ConfigGroup("Package", new[]
                {
                    new ConfigSetting("PackageTypes",                required: false, value: arrayValue,     description: "The types of packages to produce. If not specified, C#/PowerShell *.nupkg and Redist *.zip files will be produced"),
                    new ConfigSetting("PackageTests",                required: false, value: hashTableValue, description: "The tests to perform for each type of package"),
                    new ConfigSetting("PackageFiles",                required: false, value: hashTableValue, description: "The files that are expected to exist in each tyoe of package")
                })
            };

            var builder = new StringBuilder();
            var writer = new BuildToolsConfigWriter(builder);
            writer.WriteGroups(groups);

            var str = builder.ToString();

            return str;
        }

        private string GenerateAppveyor(string solutionRoot, bool force)
        {
            var str = @"
version: 'Build #{build}'
image: Visual Studio 2019
configuration: Release
environment:
  # Don't bother setting up a package cache
  DOTNET_SKIP_FIRST_TIME_EXPERIENCE: 1
  DOTNET_CLI_TELEMETRY_OPTOUT: 1
cache:
  - '%userprofile%\.nuget\packages -> appveyor.yml, **\*.csproj'
install:
  # Install NuGet Provider, Chocolatey dependencies
  - ps: |
      . .\build\Bootstrap.ps1
      Invoke-AppveyorInstall
before_build:
  # Restore NuGet packages
  # Set Appveyor build from project version
  - ps: Invoke-AppveyorBeforeBuild
build_script:
  # Build all target frameworks
  - ps: Invoke-AppveyorBuild
after_build:
  - ps: Invoke-AppveyorAfterBuild
before_test:
  # Build NuGet packages
  - ps: Invoke-AppveyorBeforeTest
test_script:
  # Test all target frameworks
  - ps: Invoke-AppveyorTest
after_test:
  # Calculate .NET coverage
  - ps: Invoke-AppveyorAfterTest
#on_finish:
#  - ps: $blockRdp = $true; iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/appveyor/ci/master/scripts/enable-rdp.ps1'))
artifacts:
  - path: '*.zip'
  - path: '*.nupkg'
  - path: '*.snupkg'
skip_commits:
  files:
    - '**/*.md'
    - '**/*.yml'
    - '**/*.nuspec'
    - assets/*
    - tools/*
skip_tags: true
";

            return WriteFile(solutionRoot, "appveyor.yml", str, force);
        }

        private void CalculateSolutionPaths(string path, out string solutionRoot, out string buildFolder)
        {
            path = Path.GetFullPath(path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).Replace("//", "/"));

            if (!fileSystem.DirectoryExists(path))
            {
                var name = Path.GetFileName(path);

                if (name.Equals("build", StringComparison.OrdinalIgnoreCase))
                {
                    solutionRoot = ProjectConfigProvider.CalculateSolutionRoot(path, null, fileSystem, powerShell);
                    buildFolder = path;
                }
                else
                    throw new InvalidOperationException($"Cannot determine the build directory to use for path '{path}': directory '{name}' does not exist and is not 'build'");
            }
            else
            {
                var name = Path.GetFileName(path);

                var slnRoot = ProjectConfigProvider.CalculateSolutionRoot(path, null, fileSystem, powerShell);

                if (name.Equals("build", StringComparison.OrdinalIgnoreCase))
                {
                    buildFolder = path;
                    solutionRoot = slnRoot;
                }
                else
                {
                    var slnDirName = Path.GetFileName(slnRoot);

                    if (ProjectConfigProvider.SrcFolders.Any(f => f.Equals(slnDirName, StringComparison.OrdinalIgnoreCase)))
                        throw new InvalidOperationException($"Cannot determine the build directory to use for path '{path}': solution is in source directory '{slnDirName}' and specified path does not end in 'build'");

                    buildFolder = Path.Combine(slnRoot, "build");
                    solutionRoot = slnRoot;
                }
            }
        }

        private string WriteFile(string path, string name, string content, bool force, Encoding encoding = null, bool windows = true)
        {
            if (!fileSystem.DirectoryExists(path))
                fileSystem.CreateDirectory(path);

            var filePath = Path.Combine(path, name);

            bool write = !fileSystem.FileExists(filePath) || force;

            if (write)
            {
                fileSystem.WriteFileText(filePath, CleanText(content, windows: windows), encoding);
                return filePath;
            }

            powerShell.WriteColor($"File '{filePath}' already exists. Specify -Force to overwrite", ConsoleColor.Yellow);
            return null;
        }

        private string CleanText(string str, bool windows = true)
        {
            str = str.TrimStart().Replace("\r", string.Empty);

            if (windows)
                return str.Replace("\n", "\r\n");

            return str;
        }
    }
}
