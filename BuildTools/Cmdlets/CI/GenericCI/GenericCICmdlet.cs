namespace BuildTools.Cmdlets.CI
{
    public abstract class GenericCICmdlet<TService> : GenericCICmdlet where TService : ICIService
    {
        protected sealed override void ProcessRecordEx()
        {
            var service = (ICIService) GetService<TService>();

            service.Execute(Configuration);
        }
    }

    public abstract class GenericCICmdlet : BaseCICmdlet<CIEnvironment>
    {
    }
}
