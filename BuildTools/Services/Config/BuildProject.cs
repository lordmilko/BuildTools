using System;
using System.IO;

namespace BuildTools
{
    [Flags]
    public enum ProjectKind
    {
        Normal = 0,
        PowerShell = 1,
        Test = 2,
        UnitTest = 4,
        IntegrationTest = 8,
        Tool = 16
    }

    public class BuildProject
    {
        /// <summary>
        /// Gets the name of the project file (without file extension).
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the project (after ignoring any potential <see cref="ProjectConfig.CoreSuffix"/>).
        /// </summary>
        public string NormalizedName { get; }

        /// <summary>
        /// Gets the full path to the project file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Gets the name of the project file (excluding directory).
        /// </summary>
        public string FileName { get; }

        public ProjectKind Kind { get; }

        public bool IsLegacy { get; }

        public BuildProject(string filePath, string normalizedName, bool isLegacy)
        {
            Name = Path.GetFileNameWithoutExtension(filePath);
            NormalizedName = normalizedName;
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Kind = GetProjectKind();
            IsLegacy = isLegacy;
        }

        private ProjectKind GetProjectKind()
        {
            if (Name.Contains("PowerShell"))
                return ProjectKind.PowerShell;

            if (Name.Contains("Test"))
            {
                if (Name.Contains("Unit"))
                    return ProjectKind.UnitTest | ProjectKind.Test;

                if (Name.Contains("Integration"))
                    return ProjectKind.IntegrationTest | ProjectKind.Test;

                //If they didn't say what kind it is, default to assuming unit tests, i.e. Foo.Tests.csproj
                return ProjectKind.UnitTest | ProjectKind.Test;
            }

            return ProjectKind.Normal;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}