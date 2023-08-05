using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorBeforeBuild")]
    public class InvokeAppveyorBeforeBuild : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
            LogHeader("Restoring NuGet Packages");

            var configService = GetService<IProjectConfigProvider>();
            var dependencyProvider = GetService<DependencyProvider>();
            var processService = GetService<ProcessService>();
            var setAppveyorVersion = GetService<SetAppveyorVersionService>();

            var solutionPath = configService.GetSolutionPath(IsLegacyMode);

            if (IsLegacyMode)
            {
                var args = new ArgList
                {
                    "restore",
                    solutionPath
                };

                var nuget = dependencyProvider.Install(WellKnownDependency.NuGet);

                processService.Execute(nuget.Path, args);
            }
            else
            {
                var args = new ArgList
                {
                    "restore",
                    solutionPath,
                    "-p:EnableSourceLink=true"
                };

                var dotnet = dependencyProvider.Install(WellKnownDependency.Dotnet);

                processService.Execute(dotnet.Path, args);
            }

            setAppveyorVersion.SetVersion(IsLegacyMode);
        }
    }
}
