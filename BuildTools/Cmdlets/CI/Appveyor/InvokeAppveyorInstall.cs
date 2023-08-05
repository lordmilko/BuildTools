using System.Management.Automation;

namespace BuildTools.Cmdlets.Appveyor
{
    [Cmdlet(VerbsLifecycle.Invoke, "AppveyorInstall")]
    class InvokeAppveyorInstall : AppveyorCmdlet
    {
        protected override void ProcessRecordEx()
        {
            //Doesn't require IsCore suffix
            var logger = GetService<Logger>();

            logger.LogHeader("Installing build dependencies");

            var dependencyProvider = GetService<DependencyProvider>();

            var dependencies = dependencyProvider.GetDependencies();

            foreach (var dependency in dependencies)
                WriteObject(dependencyProvider.Install(dependency));
        }
    }
}
