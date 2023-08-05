using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorBuild")]
    public class InvokeAppveyorBuild : AppveyorCmdlet, ILegacyProvider
    {
        protected override void ProcessRecordEx()
        {
            var configProvider = GetService<IProjectConfigProvider>();
            var environmentService = GetService<EnvironmentService>();
            var invokeBuildService = GetService<InvokeBuildService>();

            LogHeader($"Building {configProvider.Config.Name}");

            var additionalArgs = new ArgList();

            //.NET Core is not currently supported https://github.com/appveyor/ci/issues/2212
            if (environmentService.IsAppveyor && IsLegacyMode)
                additionalArgs.Add("/logger:\"C:\\Program Files\\AppVeyor\\BuildAgent\\Appveyor.MSBuildLogger.dll\"");

            if (!IsLegacyMode)
                additionalArgs.Add("--no-restore");

            invokeBuildService.Build(new BuildConfig
            {
                ArgumentList = additionalArgs,
                Configuration = Configuration,
                SourceLink = true
            }, IsLegacyMode);
        }
    }
}
