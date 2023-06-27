namespace BuildTools
{
    public class ProjectConfig
    {
        //We explicitly do not enumerate all known lang types; in the future we may add more, but in that case they won't
        //necessarily exist in all projects and so shouldn't be included in the default!
        private static readonly LangType[] DefaultTestTypes = new[]{LangType.CSharp, LangType.PowerShell};
        private static readonly PackageType[] DefaultPackageTypes = new[] { PackageType.CSharp, PackageType.PowerShell, PackageType.Redistributable };

        /// <summary>
        /// Gets the solution and csproj filename suffix used to indicate a project contains both legacy style projects and modern SDK style projects
        /// </summary>
        public const string CoreSuffix = "v17";

        [Mandatory]
        public string Name { get; set; }

        [Mandatory]
        public string SolutionName { get; set; }

        /// <summary>
        /// Gets or sets the subfolder under the <see cref="ProjectConfigProvider.SolutionRoot"/> that the source files are contained in. If this is not specified,
        /// the <see cref="ProjectConfigProvider.SourceRoot"/> is the "src" subfolder under the <see cref="ProjectConfigProvider.SolutionRoot"/> (if one exists)
        /// or the <see cref="ProjectConfigProvider.SolutionRoot"/> itself.
        /// </summary>
        [Optional]
        public string SourceFolder { get; set; }

        [Mandatory]
        public string CmdletPrefix { get; set; }

        #region Prompt

        private string prompt;

        [Optional]
        public string Prompt
        {
            get => prompt ?? Name;
            set => prompt = value;
        }

        [Optional]
        public string BuildFilter { get; set; }

        #endregion
        #region TestTypes

        private LangType[] testTypes;

        [Optional]
        public LangType[] TestTypes
        {
            get => testTypes ?? DefaultTestTypes;
            set => testTypes = value;
        }

        #endregion
        #region PackageTypes

        private PackageType[] packageTypes;

        [Optional]
        public PackageType[] PackageTypes
        {
            get => packageTypes ?? DefaultPackageTypes;
            set => packageTypes = value;
        }

        #endregion

        [Mandatory]
        public string CopyrightAuthor { get; set; }

        [Mandatory]
        public string CopyrightYear { get; set; }

        #region PowerShellModuleName

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

        #endregion

        [Optional]
        public string PowerShellProjectName { get; set; }
    }
}