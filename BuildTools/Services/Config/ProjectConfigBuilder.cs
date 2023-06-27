using System;
using System.Linq;
using System.Reflection;

namespace BuildTools
{
    public class ProjectConfigBuilder
    {
        private static PropertyInfo[] properties;

        private ProjectConfig config;

        public static readonly ProjectConfigBuilder Empty = new ProjectConfigBuilder();

        static ProjectConfigBuilder()
        {
            properties = typeof(ProjectConfig).GetProperties().Where(p => CustomAttributeExtensions.GetCustomAttribute<MandatoryAttribute>((MemberInfo) p) != null || CustomAttributeExtensions.GetCustomAttribute<OptionalAttribute>((MemberInfo) p) != null).ToArray();
        }

        public ProjectConfigBuilder()
        {
            config = new ProjectConfig();
        }

        public ProjectConfigBuilder WithName(string value) => With(v => v.Name = value);

        public ProjectConfigBuilder WithSolutionName(string value) => With(v => v.SolutionName = value);

        public ProjectConfigBuilder WithSourceFolder(string value) => With(v => v.SourceFolder = value);

        public ProjectConfigBuilder WithCmdletPrefix(string value) => With(v => v.CmdletPrefix = value);

        public ProjectConfigBuilder WithPrompt(string value) => With(v => v.Prompt = value);

        public ProjectConfigBuilder WithBuildFilter(string value) => With(v => v.BuildFilter = value);

        public ProjectConfigBuilder WithTestTypes(params LangType[] value) => With(v => v.TestTypes = value);

        public ProjectConfigBuilder WithPackageTypes(params PackageType[] value) => With(v => v.PackageTypes = value);

        public ProjectConfigBuilder WithCopyrightAuthor(string value) => With(v => v.CopyrightAuthor = value);

        public ProjectConfigBuilder WithCopyrightYear(string value) => With(v => v.CopyrightYear = value);

        public ProjectConfigBuilder WithPowerShellModuleName(string value) => With(v => v.PowerShellModuleName = value);

        public ProjectConfigBuilder WithPowerShellProjectName(string value) => With(v => v.PowerShellProjectName = value);

        public ProjectConfig Build() => config;

        public static implicit operator ProjectConfig(ProjectConfigBuilder builder) => builder.config;

        private ProjectConfigBuilder With(Action<ProjectConfig> action)
        {
            var newBuilder = new ProjectConfigBuilder();

            foreach (var property in properties)
            {
                var existingValue = property.GetValue(config);

                property.SetValue(newBuilder.config, existingValue);
            }

            action(newBuilder.config);

            return newBuilder;
        }
    }
}