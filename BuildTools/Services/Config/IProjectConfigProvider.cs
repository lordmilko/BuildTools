﻿using System.IO;

namespace BuildTools
{
    public interface IProjectConfigProvider
    {
        ProjectConfig Config { get; }

        string SolutionRoot { get; }

        string SourceRoot { get; }

        bool HasLegacyProjects { get; }

        /// <summary>
        /// Gets the full path to the project's *.sln file.
        /// </summary>
        /// <param name="isLegacy">Whether to return the legacy solution path (if applicable).</param>
        /// <returns>The full path to the project's *.sln file.</returns>
        string GetSolutionPath(bool isLegacy);

        BuildProject[] GetProjects(bool isLegacy);

        BuildProject GetPrimaryProject(bool isLegacy);

        BuildProject GetTestProject(bool integration, bool isLegacy);
        BuildProject GetUnitTestProject(bool isLegacy);
        BuildProject GetIntegrationTestProject(bool isLegacy);

        string GetTestDll(bool integration, BuildConfiguration configuration, bool isLegacy);
        string GetUnitTestDll(BuildConfiguration configuration, bool isLegacy);
        string GetIntegrationTestDll(BuildConfiguration configuration, bool isLegacy);

        /// <summary>
        /// Gets the directory in a test project containing PowerShell tests.
        /// </summary>
        /// <param name="project">The project to locate PowerShell tests within.</param>
        /// <returns>The directory in a test project containing PowerShell tests.</returns>
        string GetTestPowerShellDirectory(BuildProject project);

        /// <summary>
        /// Gets the Debug or Release directory of the PowerShell project.
        /// </summary>
        /// <param name="configuration">The build configuration to get the output directory of.</param>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="configuration"/> has not been built.</exception>
        /// <returns>The path to the <paramref name="configuration"/> build directory.</returns>
        string GetPowerShellConfigurationDirectory(BuildConfiguration configuration);

        /// <summary>
        /// Gets the path to the directory containing the built PowerShell module. This may be the raw Debug/Release directory,
        /// or may be a subfolder underneath this directory containing the PowerShell module.
        /// </summary>
        /// <param name="configuration">The build configuration to get the output directory of.</param>
        /// <param name="isLegacy">Whether to get the output directory for legacy builds.</param>
        /// <exception cref="DirectoryNotFoundException">The specified <paramref name="configuration"/> has not been built.</exception>
        /// <returns>The path to the directory containing the built PowerShell module.</returns>
        string GetPowerShellOutputDirectory(BuildConfiguration configuration, bool isLegacy);

        /// <summary>
        /// Gets the name of the PowerShell project.
        /// </summary>
        /// <param name="mandatory">Whether to throw an exception if the PowerShell Project Name cannot be identified.</param>
        /// <returns>The name of the PowerShell project, or null if <paramref name="mandatory"/> is false.</returns>
        string GetPowerShellProjectName(bool mandatory = true);

        /// <summary>
        /// Gets the full path to the PowerShell Project's *.psd1 module manifest file in its original location.<para/>
        /// This does NOT retrieve the path to the *.psd1 file published to the bin folder.
        /// </summary>
        /// <param name="relativePath">Whether to retrieve the relative, rather than absolute path to the manifest file.</param>
        /// <param name="mandatory">Whether to throw an exception if the PowerShell Project Name cannot be identified.</param>
        /// <returns>The path to the *.psd1 file in its original location.</returns>
        string GetSourcePowerShellModuleManifest(bool relativePath = false, bool mandatory = true);

        bool TryGetVersionAttribPath(out string path);

        string GetVersionAttibPath();

        string GetVersionPropsPath(bool relativePath = false);

        string GetProjectConfigurationDirectory(BuildProject project, BuildConfiguration configuration);
    }
}
