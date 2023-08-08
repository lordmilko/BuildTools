using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BuildTools.PowerShell;

namespace BuildTools
{
    class NewBuildEnvConfig
    {
        public string SolutionPath { get; set; }

        public string BuildPath { get; set; }

        public bool Force { get; set; }
    }

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

        public string[] Execute(string path, bool force)
        {
            CalculateSolutionPaths(path, out var solutionRoot, out var buildFolder);

            var results = new List<string>
            {
                GenerateBuildCmd(solutionRoot, force, false),
                GenerateBuildBash(solutionRoot, force),
                GenerateBootstrap(buildFolder, force),
                GenerateConfig(buildFolder, force),
                GenerateAppveyor(solutionRoot, force)
            };

            return results.Where(v => v != null).ToArray();
        }

        private string GenerateBuildCmd(string solutionRoot, bool force, bool core)
        {
            var exe = core ? "pwsh" : "powershell";

            var str = $@"
start {exe} -executionpolicy bypass -noexit -noninteractive -command ""ipmo psreadline; . '%~dp0Build\Bootstrap.ps1'""";

            return WriteFile(solutionRoot, "build.cmd", str, force);
        }

        private string GenerateBuildBash(string solutionRoot, bool force)
        {
            var str = @"
#!/bin/bash

BASEDIR=""$(dirname ""$BASH_SOURCE"")""
pwsh -executionpolicy bypass -noexit -noninteractive -command ""ipmo psreadline; . '$BASEDIR/build/Bootstrap.ps1'""";

            var result = WriteFile(solutionRoot, "build.sh", str, force, windows: false); //todo: chmod+x it too on write

            if (result != null)
            {
                var gitDir = Path.Combine(solutionRoot, ".git");

                if (fileSystem.DirectoryExists(gitDir))
                {
                    var git = powerShell.GetCommand("git");

                    if (git != null)
                    {
                        processService.Execute("git", new ArgList
                        {
                            "-C",
                            solutionRoot,
                            "update-index",
                            "--chmod=+x",
                            "--add",
                            result
                        });
                    }
                }
            }

            return result;
        }

        private string GenerateBootstrap(string buildFolder, bool force)
        {
            var str = @"
param(
    [Parameter(Mandatory = $false)]
    [switch]$Quiet
)

if(!$env:LORDMILKO_BUILDTOOLS_DEVELOPMENT -and !(Get-Module lordmilko.BuildTools))
{
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

    if(!(Get-Module -ListAvailable lordmilko.BuildTools))
    {
        Install-Package lordmilko.BuildTools -ForceBootstrap -Force -Source PSGallery | Out-Null
    }
    
    Import-Module lordmilko.BuildTools -Scope Local
}

Start-BuildEnvironment $PSScriptRoot -CI:(!!$env:CI) -Quiet:$Quiet";

            return WriteFile(buildFolder, "Bootstrap.ps1", str, force);
        }

        private string GenerateConfig(string buildFolder, bool force)
        {
            var name = "Config.psd1";

            if (fileSystem.DirectoryExists(buildFolder))
            {
                //Check if there's already a config file with an alternate name, e.g. PrtgAPI.psd1 instead of Config.psd1
                var existing = fileSystem.EnumerateFiles(buildFolder, "*.psd1").ToArray();

                if (existing.Length == 1 && Path.GetFileNameWithoutExtension(existing[0]) == Path.GetFileName(Path.GetDirectoryName(buildFolder)))
                    name = Path.GetFileName(existing[0]);
            }

            var groups = new[]
            {
                new ConfigGroup("Global", new[]
                {
                    new ConfigSetting("Name",                 required: true,                description: "The name of the project/GitHub repository"),
                    new ConfigSetting("CmdletPrefix",         required: true,                description: "The prefix to use for all build environment cmdlets"),
                    new ConfigSetting("Copyright",            required: true,                description: "The copyright author and year to display in the build environment"),
                    new ConfigSetting("SolutionName",         required: false,               description: "The name of the Visual Studio Solution. Required when a project contains multiple solutions"),
                    new ConfigSetting("BuildFilter",          required: false,               description: "A wildcard expression indicating the projects that should be built in CI"),
                    new ConfigSetting("DebugTargetFramework", required: false,               description: "The target framework that is used in debug mode when the project conditionally multi-targets only on Release"),
                    new ConfigSetting("Features",             required: false,               description: $"Features to enable in the build environment. By default all features are allowed, and can be negated with ~. Valid values include: {string.Join(", ", Enum.GetNames(typeof(Feature)).Where(n => n != Feature.System.ToString()))}"),
                    new ConfigSetting("Commands",             required: false, value: "@()", description: $"Commands to enable in the build environment. By default all commands are allowed, and can be negated with ~. Valid values include: {string.Join(", ", Enum.GetNames(typeof(CommandKind)))}"),
                    new ConfigSetting("Prompt",               required: false,               description: "The value to use for the prompt in the build environment. If not specified, Name will be used"),
                    new ConfigSetting("SourceFolder",         required: false,               description: "The name of the folder that the source code is contained in. If not specified, will automatically be calculated"),
                    new ConfigSetting("CoverageThreshold",    required: false,               description: "The minimum coverage threshold that must be met under CI"),
                }),
                new ConfigGroup("CSharp", new[]
                {
                    new ConfigSetting("CSharpLegacyPackageExcludes", required: false, value: "@()", description: "Files to exclude from the C# NuGet Package when building legacy packages")
                }),
                new ConfigGroup("PowerShell", new[]
                {
                    new ConfigSetting("PowerShellMultiTargeted", required: false, value: "$false", description: "Indicates that a PowerShell package should be built containing both coreclr and fullclr subfolders"),
                    new ConfigSetting("PowerShellModuleName", required: false, description: "The name of the PowerShell module. If not specified, Name will be used"),
                    new ConfigSetting("PowerShellProjectName", required: false, description: "The name of the PowerShell project. If not specified, will automatically be calculated"),
                    new ConfigSetting("PowerShellUnitTestFilter", required: false, value: "$null", description: "A ScriptBlock that takes a FileInfo/DirectoryInfo as $_ and returns whether or not to process unit tests for that file/folder")
                }),
                new ConfigGroup("Test", new[]
                {
                    new ConfigSetting("TestTypes", required: false, value: "@()", description: "The languages to perform unit tests for. If not specified, CSharp and PowerShell will be tested"),
                    new ConfigSetting("UnitTestProjectName", required: false, description: "The name of the Unit Test project. If not specified, will automatically be calculated")
                }),
                new ConfigGroup("Package", new[]
                {
                    new ConfigSetting("PackageTypes", required: false, value: "@()", description: "The types of packages to produce. If not specified, C#/PowerShell *.nupkg and Redist *.zip files will be produced"),
                    new ConfigSetting("PackageTests", required: false, value: "@{}", description: ""),
                    new ConfigSetting("PackageFiles", required: false, value: "@{}", description: "")
                })
            };

            var builder = new StringBuilder();
            var writer = new BuildToolsConfigWriter(builder);
            writer.WriteGroups(groups);

            var str = builder.ToString();

            return WriteFile(buildFolder, name, str, force);
        }

        private string GenerateAppveyor(string solutionRoot, bool force)
        {
            var str = @"
version: 'Build #{build}'
image: Visual Studio 2017
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
            path = path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)).Replace("//", "/");

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

        private string WriteFile(string path, string name, string content, bool force, bool windows = true)
        {
            if (!fileSystem.DirectoryExists(path))
                fileSystem.CreateDirectory(path);

            var filePath = Path.Combine(path, name);

            bool write = !fileSystem.FileExists(filePath) || force;

            if (write)
            {
                fileSystem.WriteFileText(filePath, CleanText(content, windows: windows));
                return filePath;
            }

            powerShell.WriteColor($"File '{filePath}' already exists. Specify -Force to overwrite", ConsoleColor.Yellow);
            return null;
        }

        private string CleanText(string str, bool windows = true)
        {
            str = str.TrimStart().Replace("\r", string.Empty);

            if (windows)
                return str.Replace("\r", "\r\n");

            return str;
        }
    }
}
