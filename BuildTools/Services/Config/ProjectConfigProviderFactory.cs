using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BuildTools.PowerShell;

namespace BuildTools
{
    class ProjectConfigProviderFactory : IProjectConfigProviderFactory
    {
        private static List<ProjectConfig> existingConfigs = new List<ProjectConfig>();

        private IFileSystemProvider fileSystem;
        private IPowerShellService powerShell;

        public ProjectConfigProviderFactory(
            IFileSystemProvider fileSystem,
            IPowerShellService powerShell)
        {
            this.fileSystem = fileSystem;
            this.powerShell = powerShell;
        }

        public IProjectConfigProvider CreateProvider(string root)
        {
            var configFile = Path.Combine(root, "Config.psd1");

            if (!fileSystem.FileExists(configFile))
                throw new FileNotFoundException($"Could not find build environment config file '{configFile}'", configFile);

            var contents = fileSystem.GetFileText(configFile);

            var hashTable = (Hashtable) powerShell.Invoke(contents);

            var config = BuildConfig(hashTable);

            ValidateConfig(config);

            return new ProjectConfigProvider(config);
        }

        private ProjectConfig BuildConfig(Hashtable hashTable)
        {
            var config = new ProjectConfig();
            var props = typeof(ProjectConfig).GetProperties().ToDictionary(p => p.Name, p => p);

            foreach (string key in hashTable.Keys)
            {
                var value = hashTable[key];

                if (!props.TryGetValue(key, out var prop))
                {
                    var str = string.Join(", ", props.Keys);

                    throw new InvalidOperationException($"Property '{key}' is not a valid setting on type '{nameof(ProjectConfig)}'. Valid properties: {str}.");
                }

                if (!string.IsNullOrWhiteSpace(value?.ToString()))
                {
                    prop.SetValue(config, value);
                }
            }

            foreach (var prop in props.Values)
            {
                var value = prop.GetValue(config);

                var attrib = prop.GetCustomAttribute<MandatoryAttribute>();

                if (attrib != null)
                {
                    if (string.IsNullOrWhiteSpace(value?.ToString()))
                        throw new InvalidOperationException("Property '' is required however no value was specified");
                }
                else
                {
                    var optional = prop.GetCustomAttribute<OptionalAttribute>();

                    if (optional == null)
                        throw new InvalidOperationException($"Property '{nameof(ProjectConfig)}.{prop.Name}' does not specify either a '{nameof(MandatoryAttribute)}' or '{nameof(OptionalAttribute)}'.");
                }
                
            }

            return config;
        }

        private void ValidateConfig(ProjectConfig config)
        {
            foreach (var existing in existingConfigs)
            {
                if (existing.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Cannot initialize build environment: a build environment with name '{config.Name}' has already been loaded.");

                if (existing.CmdletPrefix.Equals(config.CmdletPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Cannot initialize build environment: a build environment with cmdlet prefix '{config.CmdletPrefix}' has already been loaded.");
            }

            existingConfigs.Add(config);
        }
    }
}