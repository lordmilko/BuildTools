namespace BuildTools
{
    interface IProjectConfigProviderFactory
    {
        IProjectConfigProvider CreateProvider(string buildRoot, string file);
    }
}