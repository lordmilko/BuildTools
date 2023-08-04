using System;
using System.Collections.Generic;
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
            properties = typeof(ProjectConfig).GetProperties().Where(p => p.GetCustomAttribute<RequiredAttribute>() != null || p.GetCustomAttribute<OptionalAttribute>() != null).ToArray();
        }

        public ProjectConfigBuilder()
        {
            config = new ProjectConfig();
        }

        public ProjectConfigBuilder WithName(string value) => With(v => v.Name = value);

        public ProjectConfigBuilder WithSolutionName(string value) => With(v => v.SolutionName = value);

        public ProjectConfigBuilder WithSourceFolder(string value) => With(v => v.SourceFolder = value);

        public ProjectConfigBuilder WithCmdletPrefix(string value) => With(v => v.CmdletPrefix = value);

        public ProjectConfigBuilder WithPowerShellMultiTargeted(bool value) => With(v => v.PowerShellMultiTargeted = value);

        public ProjectConfigBuilder WithPrompt(string value) => With(v => v.Prompt = value);

        public ProjectConfigBuilder WithBuildFilter(string value) => With(v => v.BuildFilter = value);

        public ProjectConfigBuilder WithTestTypes(params TestType[] value) => With(v => v.TestTypes = value);

        public ProjectConfigBuilder WithPackageTypes(params PackageType[] value) => With(v => v.PackageTypes = value);

        public ProjectConfigBuilder WithCopyright(string value) => With(v => v.Copyright = value);

        public ProjectConfigBuilder WithPowerShellModuleName(string value) => With(v => v.PowerShellModuleName = value);

        public ProjectConfigBuilder WithPowerShellProjectName(string value) => With(v => v.PowerShellProjectName = value);

        public ProjectConfigBuilder WithDebugTargetFramework(string value) => With(v => v.DebugTargetFramework = value);

        public ProjectConfigBuilder WithPackageTests(Dictionary<string, IPackageTest[]> value)
        {
            var packageTests = new PackageTests();

            foreach (var kv in value)
            {
                switch (kv.Key)
                {
                    case "C#":
                        packageTests.CSharp = kv.Value;
                        break;

                    case "PowerShell":
                        packageTests.PowerShell = kv.Value;
                        break;

                    default:
                        throw new NotImplementedException($"Don't know how to handle package type '{kv.Key}'");
                }
            }

            return With(v => v.PackageTests = packageTests);
        }

        public ProjectConfigBuilder WithPackageFiles(Dictionary<string, object[]> value)
        {
            var packageFiles = new PackageFiles();

            PackageFileItem[] GetItems(object[] values)
            {
                var results = new List<PackageFileItem>();

                foreach (var item in values)
                {
                    if (item is PackageFileItem i)
                        results.Add(i);
                    else
                        results.Add(new PackageFileItem((string)item));
                }

                return results.ToArray();
            }

            foreach (var kv in value)
            {
                switch (kv.Key)
                {
                    case "C#":
                        packageFiles.CSharp = GetItems(kv.Value);
                        break;

                    case "PowerShell":
                        packageFiles.PowerShell = GetItems(kv.Value);
                        break;

                    case "Redist":
                        packageFiles.Redist = GetItems(kv.Value);
                        break;

                    default:
                        throw new InvalidOperationException($"PackageFiles key {kv.Key} is not valid");
                }
            }

            return With(v => v.PackageFiles = packageFiles);
        }

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
