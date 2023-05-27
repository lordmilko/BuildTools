namespace BuildTools
{
    interface IProjectConfigProviderFactory
    {
        IProjectConfigProvider CreateProvider(string root);
    }
}