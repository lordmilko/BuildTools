using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;
using BuildTools.Reflection;

namespace BuildTools.PowerShell
{
    class PowerShellService : IPowerShellService
    {
        private Stack<PSCmdlet> cmdletStack = new Stack<PSCmdlet>();

        private static MethodInfo addExportedCmdletMethod;

        static PowerShellService()
        {
            addExportedCmdletMethod = typeof(PSModuleInfo).GetInternalMethod("AddExportedCmdlet");
        }

        public void Push(PSCmdlet cmdlet)
        {
            cmdletStack.Push(cmdlet);
        }

        public void Pop()
        {
            cmdletStack.Pop();
        }

        private object GetVariable(string name) => ActiveCmdlet.GetVariableValue(name);

        public IPowerShellCommand GetCommand(string name)
        {
            var command = ActiveCmdlet.InvokeCommand.GetCommand(name, CommandTypes.All);

            if (command == null)
                return null;

            return new PowerShellCommand(command);
        }

        public void WriteColor(string message, ConsoleColor? color = null, bool newline = true)
        {
            var ui = ActiveCmdlet.Host.UI;

            if (color == null)
            {
                if (newline)
                    ui.WriteLine(message);
                else
                    ui.Write(message);
            }
            else
            {
                var bg = ui.RawUI.BackgroundColor;

                if (newline)
                    ui.WriteLine(color.Value, bg, message);
                else
                    ui.Write(color.Value, bg, message);
            }
        }

        public void WriteError(ErrorRecord errorRecord) =>
            ActiveCmdlet.WriteError(errorRecord);

        public void WriteVerbose(string message) =>
            ActiveCmdlet.WriteVerbose(message);
        {
        }

        public void WriteWarning(string message) =>
            ActiveCmdlet.WriteWarning(message);

        public IPowerShellModule[] GetInstalledModules(string name)
        {
            var results = ActiveCmdlet.InvokeCommand.InvokeScript($"Get-Module -ListAvailable '{name}'");

            if (results == null || results.Count == 0)
                return new IPowerShellModule[0];

            return results.Select(r => (IPowerShellModule) new PowerShellModule((PSModuleInfo) UnwrapPSObject(r))).ToArray();
        }
        public IPowerShellPackage InstallPackage(string name, Version requiredVersion = null, Version minimumVersion = null, bool skipPublisherCheck = false)
        {
            var args = new List<string>
            {
                $"-Name {name}",
                "-Force",
                "-ForceBootstrap",
                "-ProviderName PowerShellGet"
            };

            if (requiredVersion != null)
                args.Add($"-RequiredVersion {requiredVersion}");

            if (minimumVersion != null)
                args.Add($"-MinimumVersion {minimumVersion}");

            if (skipPublisherCheck)
                args.Add("-SkipPublisherCheck");

            var result = ActiveCmdlet.InvokeCommand.InvokeScript($"Install-Package {string.Join(" ", args)}").First();

            return new PowerShellPackage(result);
        }

        public IPackageProvider GetPackageProvider(string name)
        {
            //It's faster to filter all package providers for the one we're after than ask for
            //the target provider directly. If it doesn't exist, Get-PackageProvider will hang!
            var result = ActiveCmdlet.InvokeCommand.InvokeScript($"Get-PackageProvider | where Name -eq '{name}'").FirstOrDefault();

            if (result == null)
                return null;

            return new PackageProvider(result);
        }

        public IPackageProvider InstallPackageProvider(string name, Version minimumVersion = null)
        {
            var args = new List<string>
            {
                $"-Name {name}",
                "-Force"
            };

            if (minimumVersion != null)
                args.Add($"-MinimumVersion {minimumVersion}");

            var result = ActiveCmdlet.InvokeCommand.InvokeScript($"Install-PackageProvider {string.Join(" ", args)}").First();

            return new PackageProvider(result);
        }

        public object Invoke(string script, params object[] input)
        {
            var result = ActiveCmdlet.InvokeCommand.InvokeScript(script, false, PipelineResultTypes.None, input);

            if (result == null || result.Count == 0)
                return null;

            var arr = result.Select(UnwrapPSObject).ToArray();

            if (arr.Length == 1)
                return arr[0];

            return arr;
        }

        public void InitializePrompt(ProjectConfig config)
        {
            ActiveCmdlet.Host.UI.RawUI.WindowTitle = $"{config.Name} Build Environment";

            if (!IsISE)
            {
                Invoke($@"
function global:Prompt
{{
    Write-Host ""{config.Prompt} "" -NoNewLine -ForegroundColor Green
    Write-Host ((Get-Location).Path + "">"") -NoNewLine
    return "" ""
}}");
            }
        }

        private object UnwrapPSObject(object obj)
        {
            if (obj is PSObject pso)
                return UnwrapPSObject(pso.BaseObject);

            return obj;
        }
    }
}
