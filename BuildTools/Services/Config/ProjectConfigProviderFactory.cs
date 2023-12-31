﻿using System;
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

        public IProjectConfigProvider CreateProvider(string buildRoot, string file = null)
        {
            powerShell.WriteVerbose($"Reading config from directory '{buildRoot}'{(file != null ? $" (File: {file})" : string.Empty)}");

            var originalBuildRoot = buildRoot;
            buildRoot = Path.GetFullPath(buildRoot);

            if (originalBuildRoot != buildRoot)
                powerShell.WriteVerbose($"Resolved build root to '{buildRoot}'");

            var originalFile = file;

            if (file == null)
                file = "Build.psd1";
            else
            {
                var ext = Path.GetExtension(file);

                if (!file.EndsWith(".psd1", StringComparison.OrdinalIgnoreCase) && ext.Length <= 3)
                    file += ".psd1";
            }

            var configFile = Path.Combine(buildRoot, file);
            powerShell.WriteVerbose($"Using config file {configFile}");

            if (!fileSystem.FileExists(configFile))
            {
                powerShell.WriteVerbose($"Could not find config file '{file}'");

                bool found = false;

                if (originalFile == null)
                {
                    powerShell.WriteVerbose("Searching for alternate config files");
                    var candidates = fileSystem.EnumerateFiles(buildRoot, "*.psd1").ToArray();

                    if (candidates.Length == 1)
                    {
                        configFile = candidates[0];
                        powerShell.WriteVerbose($"Using config file '{configFile}'");
                        found = true;
                    }
                }

                if (!found)
                    throw new FileNotFoundException($"Could not find build environment config file '{configFile}'", configFile);
            }

            var contents = fileSystem.ReadFileText(configFile);

            var hashTable = (Hashtable) powerShell.InvokeAndUnwrap(contents);

            var config = BuildConfig(hashTable);

            ValidateFinalConfig(config);

            return new ProjectConfigProvider(config, buildRoot, fileSystem, powerShell);
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
                        if (prop.PropertyType == typeof(Func<FileInfo, bool>))
                            value = ConvertScriptBlockToFunc<FileInfo, bool>(sb);
                        else
                            throw new NotImplementedException($"Deserializing a property of type {prop.PropertyType} is not implemented");
                    }

                    var valueConverterAttrib = prop.GetCustomAttribute<ValueConverterAttribute>();

                    if (valueConverterAttrib != null)
                    {
                        var instanceField = valueConverterAttrib.Type.GetField("Instance");

                        if (instanceField == null)
                            throw new MissingMemberException(valueConverterAttrib.Type.Name, "Instance");

                        value = ((IValueConverter) instanceField.GetValue(null)).Convert(value);
                    }

                    if (value != null && value.GetType() != prop.PropertyType)
                    {
                        if (value is string s)
                        {
                            if (prop.PropertyType.IsEnum)
                            {
                                if (s.TryParseDescriptionToEnum(prop.PropertyType, out var val))
                                    value = val;
                            }
                            else if (prop.PropertyType.IsArray)
                            {
                                var elm = prop.PropertyType.GetElementType();

                                if (elm.IsEnum)
                                {
                                    if (s.TryParseDescriptionToEnum(elm, out var val))
                                        value = val;
                                }
                            }
                        }

                        value = LanguagePrimitives.ConvertTo(value, prop.PropertyType);
                    }

                    prop.SetValue(config, value);
                }
            }

            ValidateRequired(config, props.Values.ToArray());

            return config;
        }

        private Func<T, TResult> ConvertScriptBlockToFunc<T, TResult>(ScriptBlock sb)
        {
            Func<T, TResult> func = arg =>
            {
                var result = sb.InvokeWithContext(null,
                    new List<PSVariable>
                    {
                        new PSVariable("_", arg)
                    }
                );

                var val = result?.FirstOrDefault()?.BaseObject;

                if (val == null)
                    throw new InvalidOperationException($"Expected {nameof(ScriptBlock)} '{sb}' to return a value.");

                if (val is TResult)
                    return (TResult)val;

                throw new InvalidOperationException($"Expected a return value of type {typeof(TResult).Name} however got return value '{val}' of type {val.GetType().Name}");
            };

            return func;
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

            if (value is Array arr)
                return arr.Length == 0;

            return string.IsNullOrWhiteSpace(value.ToString());
        }

        private void ValidateFinalConfig(ProjectConfig config)
        {
            foreach (var existing in existingConfigs)
            {
                if (existing.Name.Equals(config.Name, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Cannot initialize build environment: a build environment with name '{config.Name}' has already been loaded.");

                if (existing.CmdletPrefix.Equals(config.CmdletPrefix, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Cannot initialize build environment: a build environment with cmdlet prefix '{config.CmdletPrefix}' has already been loaded.");
            }

            RegisterConfig(config);
        }

        protected virtual void RegisterConfig(ProjectConfig config) => existingConfigs.Add(config);

        private void ValidateRequired(ProjectConfig config, PropertyInfo[] properties)
        {
            foreach (var prop in properties)
            {
                var value = prop.GetValue(config);

                var required = prop.GetCustomAttribute<RequiredAttribute>();

                if (required != null)
                {
                    if (required is RequiredWithAttribute rw)
                    {
                        if (IsMissingValue(value))
                        {
                            if (config.HasFeature(rw.Feature))
                                throw new InvalidOperationException($"Property '{prop.Name}' is required when feature '{rw.Feature}' is used however no value was specified");
                        }
                    }
                    else
                    {
                        if (IsMissingValue(value))
                            throw new InvalidOperationException($"Property '{prop.Name}' is required however no value was specified");
                    }                    
                }
                else
                {
                    var optional = prop.GetCustomAttribute<OptionalAttribute>();

                    if (optional == null)
                        throw new InvalidOperationException($"Property '{nameof(ProjectConfig)}.{prop.Name}' does not specify either a '{nameof(RequiredAttribute)}' or '{nameof(OptionalAttribute)}'.");
                }

                ValidatePackageTypes(value, prop, config);
            }
        }

        private void ValidatePackageTypes(object value, PropertyInfo parentProperty, ProjectConfig config)
        {
            //If the value must be provided, we would have checked this above with things such as RequiredWith
            if (value == null)
                return;

            var properties = parentProperty.PropertyType.GetProperties();

            foreach (var prop in properties)
            {
                var attrib = prop.GetCustomAttribute<PackageTypeAttribute>();

                if (attrib != null)
                {
                    var propertyValue = prop.GetValue(value);

                    if (config.PackageTypes.Contains(attrib.Type) && IsMissingValue(propertyValue))
                        throw new InvalidOperationException($"No '{parentProperty.Name}' for '{prop.Name}' have been defined");
                }
            }
        }
    }
}
