namespace BuildTools
{
    interface IProjectConfigProvider
    {
        ProjectConfig Config { get; }

        string SolutionRoot { get; }
    }
}