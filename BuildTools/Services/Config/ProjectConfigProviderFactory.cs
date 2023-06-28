using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management.Automation;
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

        public IProjectConfigProvider CreateProvider(string buildRoot, string file)
        {
            buildRoot = Path.GetFullPath(buildRoot);

            var originalFile = file;

            if (file == null)
                file = "Config.psd1";
            else
            {
                var ext = Path.GetExtension(file);

                if (!file.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase) && ext.Length <= 3)
                    file += ".psd1";
            }

            var configFile = Path.Combine(buildRoot, file);

            if (!fileSystem.FileExists(configFile))
            {
                bool found = false;

                if (originalFile == null)
                {
                    var candidates = fileSystem.EnumerateFiles(buildRoot, "*.psd1").ToArray();

                    if (candidates.Length == 1)
                    {
                        configFile = candidates[0];
                        found = true;
                    }
                }

                if (!found)
                    throw new FileNotFoundException($"Could not find build environment config file '{configFile}'", configFile);
            }

            var contents = fileSystem.GetFileText(configFile);

            var hashTable = (Hashtable) powerShell.InvokeAndUnwrap(contents);

            var config = BuildConfig(hashTable);

            ValidateConfig(config);

            return new ProjectConfigProvider(config, buildRoot, fileSystem);
        }

        private ProjectConfig BuildConfig(Hashtable hashTable)
        {
            if (hashTable == null)
                throw new InvalidOperationException($"Config file did not contain a {nameof(hashTable)}");

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
                    if (value is ScriptBlock sb)
                    {
                        Func<ProjectConfigResolutionContext, string> func = ctx =>
                        {
                            var result = sb.InvokeWithContext(null,
                                new List<PSVariable>
                                {
                                    new PSVariable("_", ctx)
                                }
                            );

                            var str = result?.FirstOrDefault()?.BaseObject.ToString();

                            if (str == null)
                                throw new InvalidOperationException($"Expected {nameof(ScriptBlock)} '{sb}' to return a value.");

                            return str;
                        };

                        value = func;
                    }

                    if (value.GetType() != prop.PropertyType)
                        value = LanguagePrimitives.ConvertTo(value, prop.PropertyType);

                    prop.SetValue(config, value);
                }
            }

            foreach (var prop in props.Values)
            {
                var value = prop.GetValue(config);

                var attrib = prop.GetCustomAttribute<MandatoryAttribute>();

                if (attrib != null)
                {
                    if (IsMissingValue(value))
                        throw new InvalidOperationException($"Property '{prop.Name}' is required however no value was specified");
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

        private bool IsMissingValue(object value)
        {
            if (value == null)
                return true;

            var type = value.GetType();

            if (type.IsValueType)
            {
                var @default = Activator.CreateInstance(type);

                return value.Equals(@default);
            }

            return string.IsNullOrWhiteSpace(value.ToString());
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