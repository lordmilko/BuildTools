using System;

namespace BuildTools
{
    class ConfigSetting
    {
        public string Name { get; }

        public bool Required { get; }

        public IConfigValue Value { get; }

        public string Description { get; }

        public ConfigSetting(string name, bool required, Func<string, IConfigValue> value = null, string description = null)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            if (description == null)
                throw new ArgumentNullException(nameof(description));

            Name = name;
            Required = required;
            Value = value(name);
            Description = description;
        }
    }
}
