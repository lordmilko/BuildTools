namespace BuildTools
{
    public interface IProjectConfigProvider
    {
        ProjectConfig Config { get; }

        string SolutionRoot { get; }

        string SourceRoot { get; }

        /// <summary>
        /// Gets the full path to the project's *.sln file.
        /// </summary>
        /// <param name="isLegacy">Whether to return the legacy solution path (if applicable).</param>
        /// <returns>The full path to the project's *.sln file.</returns>
        string GetSolutionPath(bool isLegacy);

        BuildProject[] GetProjects(bool isLegacy);

        BuildProject GetPrimaryProject(bool isLegacy);

        string GetPowerShellConfigurationDirectory(BuildConfiguration buildConfiguration);

        string GetPowerShellOutputDirectory(BuildConfiguration buildConfiguration, bool isLegacy);

        string GetPowerShellProjectName();

        /// <summary>
        /// Gets the full path to the PowerShell Project's *.psd1 module manifest file in its original location.<para/>
        /// This does NOT retrieve the path to the *.psd1 file published to the bin folder.
        /// </summary>
        /// <returns>The path to the *.psd1 file in its original location.</returns>
        string GetSourcePowerShellModuleManifest(bool relativePath = false);

        string GetVersionAttibPath();

        string GetVersionPropsPath(bool relativePath = false);
    }
}