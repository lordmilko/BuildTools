using System;
using System.Linq;

namespace BuildTools
{
    public class ProjectConfig
    {
        private static readonly LangType[] AllLangTypes;

        static ProjectConfig()
        {
            AllLangTypes = Enum.GetValues(typeof(LangType)).Cast<LangType>().ToArray();
        }

        [Mandatory]
        public string Name { get; set; }

        [Mandatory]
        public Either<Func<ProjectConfigResolutionContext, string>, string> SolutionName { get; set; }

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

        #endregion
        #region TestTypes

        private LangType[] testTypes;

        [Optional]
        public LangType[] TestTypes
        {
            get => testTypes ?? AllLangTypes;
            set => testTypes = value;
        }

        #endregion
        #region PackageTypes

        private LangType[] packageTypes;

        [Optional]
        public LangType[] PackageTypes
        {
            get => packageTypes ?? AllLangTypes;
            set => packageTypes = value;
        }

        #endregion

        [Mandatory]
        public string CopyrightAuthor { get; set; }

        [Mandatory]
        public string CopyrightYear { get; set; }
    }
}