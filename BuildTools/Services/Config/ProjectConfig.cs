using System;
using System.IO;

namespace BuildTools
{
    public class ProjectConfig
    {
        //We explicitly do not enumerate all known lang types; in the future we may add more, but in that case they won't
        //necessarily exist in all projects and so shouldn't be included in the default!
        private static readonly TestType[] DefaultTestTypes = new[]{ TestType.CSharp, TestType.PowerShell};
        private static readonly PackageType[] DefaultPackageTypes = new[] { PackageType.CSharp, PackageType.PowerShell, PackageType.Redistributable };

        /// <summary>
        /// Gets the solution and csproj filename suffix used to indicate a project contains both legacy style projects and modern SDK style projects
        /// </summary>
        public const string CoreSuffix = "v17";

        #region Global
        #region Required

        /// <summary>
        /// Gets or sets the name of the project/GitHub repository.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the prefix to use for all build environment cmdlets.
        /// </summary>
        [Required]
        public string CmdletPrefix { get; set; }

        [Required]
        public string Copyright { get; set; }

        #endregion
        #region Optional

        [Optional]
        public string SolutionName { get; set; }

        [Optional]
        public string BuildFilter { get; set; }

        [Optional]
        public string DebugTargetFramework { get; set; }

        [Optional]
        public CommandKind[] ExcludedCommands { get; set; }

        private string prompt;

        [Optional]
        public string Prompt
        {
            get => prompt ?? Name;
            set => prompt = value;
        }

        /// <summary>
        /// Gets or sets the subfolder under the <see cref="ProjectConfigProvider.SolutionRoot"/> that the source files are contained in. If this is not specified,
        /// the <see cref="ProjectConfigProvider.SourceRoot"/> is the "src" subfolder under the <see cref="ProjectConfigProvider.SolutionRoot"/> (if one exists)
        /// or the <see cref="ProjectConfigProvider.SolutionRoot"/> itself.
        /// </summary>
        [Optional]
        public string SourceFolder { get; set; }

        [RequiredWith(CommandKind.Coverage)]
        public double? CoverageThreshold { get; set; }

        #endregion
        #endregion
        #region CSharp

        [Optional]
        public string[] CSharpLegacyPackageExcludes { get; set; }

        #endregion
        #region PowerShell

        [Optional]
        public bool PowerShellMultiTargeted { get; set; }

        private string powerShellModuleName;

        /// <summary>
        /// Gets or sets the name to use for the PowerShell Module. If no name is specified, <see cref="Name"/> will automatically be used.
        /// </summary>
        [Optional]
        public string PowerShellModuleName
        {
            get => powerShellModuleName ?? Name;
            set => powerShellModuleName = value;
        }

        [Optional]
        public string PowerShellProjectName { get; set; }

        [Optional]
        public Func<FileInfo, bool> PowerShellUnitTestFilter { get; set; }

        #endregion
        #region Test

        private TestType[] testTypes;

        [Optional]
        public TestType[] TestTypes
        {
            get => testTypes ?? DefaultTestTypes;
            set => testTypes = value;
        }

        [Optional]
        public string UnitTestProjectName { get; set; }

        #endregion
        #region Package

        private PackageType[] packageTypes;

        [Optional]
        public PackageType[] PackageTypes
        {
            get => packageTypes ?? DefaultPackageTypes;
            set => packageTypes = value;
        }

        [Optional]
        [HashtableConverter(typeof(PackageTestsHashtableConverter))]
        public PackageTests PackageTests { get; set; }

        [RequiredWith(CommandKind.NewPackage)]
        [HashtableConverter(typeof(PackageFilesHashtableConverter))]
        public PackageFiles PackageFiles { get; set; }

        #endregion
    }
}
