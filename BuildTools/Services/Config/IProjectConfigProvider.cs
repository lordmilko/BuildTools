using System.IO;

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

        BuildProject GetUnitTestProject(bool isLegacy);

        string GetUnitTestDll(BuildConfiguration buildConfiguration, bool isLegacy);

        /// <summary>
        /// Gets the Debug or Release directory of the PowerShell project.
        /// </summary>
        /// <param name="buildConfiguration">The build configuration to get the output directory of.</param>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="buildConfiguration"/> has not been built.</exception>
        /// <returns>The path to the <paramref name="buildConfiguration"/> build directory.</returns>
        string GetPowerShellConfigurationDirectory(BuildConfiguration buildConfiguration);

        /// <summary>
        /// Gets the path to the directory containing the built PowerShell module. This may be the raw Debug/Release directory,
        /// or may be a subfolder underneath this directory containing the PowerShell module.
        /// </summary>
        /// <param name="buildConfiguration">The build configuration to get the output directory of.</param>
        /// <param name="isLegacy">Whether to get the output directory for legacy builds.</param>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="buildConfiguration"/> has not been built.</exception>
        /// <returns>The path to the directory containing the built PowerShell module.</returns>
        string GetPowerShellOutputDirectory(BuildConfiguration buildConfiguration, bool isLegacy);

        /// <summary>
        /// Gets the name of the PowerShell project.
        /// </summary>
        /// <returns>The name of the PowerShell project.</returns>
        string GetPowerShellProjectName();

        /// <summary>
        /// Gets the full path to the PowerShell Project's *.psd1 module manifest file in its original location.<para/>
        /// This does NOT retrieve the path to the *.psd1 file published to the bin folder.
        /// </summary>
        /// <returns>The path to the *.psd1 file in its original location.</returns>
        string GetSourcePowerShellModuleManifest(bool relativePath = false);

        string GetVersionAttibPath();

        string GetVersionPropsPath(bool relativePath = false);

        string GetProjectConfigurationDirectory(BuildProject project, BuildConfiguration buildConfiguration);
    }
}