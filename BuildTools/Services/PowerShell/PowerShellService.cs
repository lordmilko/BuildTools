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

            if (color == null || BuildToolsSessionState.HeadlessUI)
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

        public void WriteProgress(
            string activity = null,
            string status = null,
            string currentOperation = null,
            int? percentComplete = null)
        {
            activity = activity?.Trim();
            status = status?.Trim();
            currentOperation = currentOperation?.Trim();

            if (progressRecord == null)
                progressRecord = new ProgressRecord(1, activity, status);
            else
            {
                if (activity != null)
                    progressRecord.Activity = activity;

                if (status != null)
                    progressRecord.StatusDescription = status;
            }

            if (currentOperation != null)
                progressRecord.CurrentOperation = currentOperation;

            if (percentComplete != null)
                progressRecord.PercentComplete = percentComplete.Value;

            ActiveCmdlet.WriteProgress(progressRecord);
        }

        public void WriteWarning(string message) =>
            ActiveCmdlet.WriteWarning(message);

        public void CompleteProgress()
        {
            if (progressRecord == null)
                return;

            progressRecord.RecordType = ProgressRecordType.Completed;
            ActiveCmdlet.WriteProgress(progressRecord);
            progressRecord = null;
        }

        public IPowerShellModule[] GetInstalledModules(string name)
        {
            return Invoke<IPowerShellModule>(
                $"Get-Module -ListAvailable '{name}'",
                o => new PowerShellModule((PSModuleInfo)UnwrapPSObject(o))
            );
        }

        public IPowerShellModule GetModule(string name)
        {
            return Invoke(
                "Get-Module",
                new[] {$"-Name '{name}'"},
                o => new PowerShellModule((PSModuleInfo)UnwrapPSObject(o))
            ).FirstOrDefault();
        }

        public IPowerShellModule ImportModule(string name, bool global)
        {
            var args = new List<string>
            {
                $"-Name '{name}'"
            };

            if (global)
                args.Add("-Global");

            return Invoke("Import-Module", args, o => new PowerShellModule((PSModuleInfo)UnwrapPSObject(o))).First();
        }

        public IPowerShellModule RegisterModule(string name, IList<Type> cmdletTypes)
        {
            if (cmdletTypes == null)
                throw new ArgumentNullException(nameof(cmdletTypes));

            if (cmdletTypes.Count == 0)
                throw new InvalidOperationException("At least one cmdlet type should be specified");

            var module = (PSModuleInfo) InvokeAndUnwrap($"New-Module {name}.Build {{}}");

            var cmdletInfoModule = typeof(CmdletInfo).GetProperty(nameof(CmdletInfo.Module));

            foreach (var type in cmdletTypes)
            {
                var attrib = type.GetCustomAttribute<CmdletAttribute>();

                var cmdletName = $"{attrib.VerbName}-{attrib.NounName}";
                var info = new CmdletInfo(cmdletName, type);

                addExportedCmdletMethod.Invoke(module, new object[] {info});
                cmdletInfoModule.GetSetMethod(true).Invoke(info, new object[] {module});
            }

            var result = (PSModuleInfo) InvokeAndUnwrap("$input | Import-Module -PassThru", module);

            if (result == null)
                throw new InvalidOperationException("Dynamic module could not be created.");

            return new PowerShellModule(result);
        }

        public void PublishModule(string path)
        {
            var script = $@"
$original = $global:ProgressPreference

$global:ProgressPreference = 'SilentlyContinue'

try
{{
    Publish-Module -Path '{path}' -Repository '{PackageSourceService.RepoName}' -WarningAction SilentlyContinue
}}
finally
{{
    $global:ProgressPreference = $original
}}";

            InvokeWithArgs(script);
        }

        public IPowerShellPackage InstallPackage(string name, Version requiredVersion = null, Version minimumVersion = null, bool skipPublisherCheck = false)
        {
            var args = new List<string>
            {
                $"-Name {name}",
                "-Force",
                "-ForceBootstrap",
                "-AllowClobber",
                "-ProviderName PowerShellGet"
            };

            if (requiredVersion != null)
                args.Add($"-RequiredVersion {requiredVersion}");

            if (minimumVersion != null)
                args.Add($"-MinimumVersion {minimumVersion}");

            if (skipPublisherCheck)
                args.Add("-SkipPublisherCheck");

            return Invoke("Install-Package", args, o => new PowerShellPackage(o)).First();
        }

        #region PackageProvider

        public IPackageProvider GetPackageProvider(string name)
        {
            //It's faster to filter all package providers for the one we're after than ask for
            //the target provider directly. If it doesn't exist, Get-PackageProvider will hang!
            return Invoke($"Get-PackageProvider | where Name -eq '{name}'", o => new PackageProvider(o)).FirstOrDefault();
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

            return Invoke("Install-PackageProvider", args, o => new PackageProvider(o)).First();
        }

        #endregion
        #region PackageSource

        public IPackageSource[] GetPackageSource()
        {
            return Invoke<IPackageSource>("Get-PackageSource", o => new PackageSource(o));
        }

        public void RegisterPackageSource()
        {
            var args = new[]
            {
                $"-Name '{PackageSourceService.RepoName}'",
                $"-Location '{PackageSourceService.RepoLocation}'",
                "-ProviderName 'NuGet'",
                "-Trusted"
            };

            InvokeWithArgs("Register-PackageSource", args);
        }

        public void UnregisterPackageSource()
        {
            var args = new[]
            {
                $"-Name '{PackageSourceService.RepoName}'",
                $"-Location '{PackageSourceService.RepoLocation}'",
                $"-ProviderName 'NuGet'",
                "-Force"
            };

            InvokeWithArgs("Unregister-PackageSource", args);
        }

        #endregion
        #region PSRepository

        public IPSRepository[] GetPSRepository()
        {
            return Invoke<IPSRepository>("Get-PSRepository", o => new PSRepository(o));
        }

        public void RegisterPSRepository()
        {
            var args = new[]
            {
                $"-Name '{PackageSourceService.RepoName}'",
                $"-SourceLocation '{PackageSourceService.RepoLocation}'",
                $"-PublishLocation '{PackageSourceService.RepoLocation}'",
                "-InstallationPolicy 'Trusted'"
            };

            InvokeWithArgs("Register-PSRepository", args);
        }

        public void UnregisterPSRepository()
        {
            InvokeWithArgs("Unregister-PSRepository", PackageSourceService.RepoName);
        }

        #endregion

        public PesterResult[] InvokePester(string path, string[] additionalArgs)
        {
            var args = new List<string>
            {
                $"-Script '{path}'",
                "-PassThru"
            };

            if (additionalArgs != null)
                args.AddRange(additionalArgs);

            return Invoke("Invoke-Pester", args, o => new PesterResult(o));
        }

        public object InvokeAndUnwrap(string script, params object[] input)
        {
            var result = ActiveCmdlet.InvokeCommand.InvokeScript(script, false, PipelineResultTypes.None, input);

            if (result == null || result.Count == 0)
                return null;

            var arr = result.Select(UnwrapPSObject).ToArray();

            if (arr.Length == 1)
                return arr[0];

            return arr;
        }

        public object[] InvokeWithArgs(string cmdlet, params string[] args) =>
            Invoke<object>(cmdlet, args, null);

        private T[] Invoke<T>(string cmdlet, Func<PSObject, T> makeResult) =>
            Invoke<T>(cmdlet, null, makeResult);

        private T[] Invoke<T>(string cmdlet, IList<string> args, Func<PSObject, T> makeResult)
        {
            var script = cmdlet;

            if (args != null && args.Count > 0)
                script += $" {string.Join(" ", args)}";

            var raw = ActiveCmdlet.InvokeCommand.InvokeScript(script);

            if (raw == null || raw.Count == 0)
                return new T[0];

            if (makeResult == null)
                return raw.Cast<T>().ToArray();

            var results = raw.Select(r =>
            {
                if (Equals(r, default(T)))
                    return default(T);

                return makeResult(r);
            }).ToArray();

            return results;
        }

        public void InitializePrompt(ProjectConfig config)
        {
            if (BuildToolsSessionState.HeadlessUI)
                return;

            ActiveCmdlet.Host.UI.RawUI.WindowTitle = $"{config.Name} Build Environment";

            if (!IsISE)
            {
                InvokeWithArgs($@"
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
