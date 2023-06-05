namespace BuildTools
{
    interface IProjectConfigProvider
    {
        ProjectConfig Config { get; }

        string SolutionRoot { get; }

        /// <summary>
        /// Gets the full path to the project's *.sln file.
        /// </summary>
        /// <param name="isLegacy">Whether to return the legacy solution path (if applicable).</param>
        /// <returns>The full path to the project's *.sln file.</returns>
        string GetSolutionPath(bool isLegacy);
    }
}