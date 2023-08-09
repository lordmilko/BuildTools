namespace BuildTools.Cmdlets
{
    public abstract class GlobalBuildCmdlet : BuildCmdlet<object>
    {
        protected GlobalBuildCmdlet() : base(false)
        {
        }

        protected override T GetService<T>() => BuildToolsSessionState.GlobalServiceProvider.GetService<T>();
    }
}
