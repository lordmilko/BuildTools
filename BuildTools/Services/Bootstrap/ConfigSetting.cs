using System;

namespace BuildTools
{
    class ConfigSetting
    {
        public string Name { get; }

        public bool Required { get; }

        public string Value { get; }

        public string Description { get; }

        public ConfigSetting(string name, bool required, string value = "''", string description = null)
        {
            if (description == null)
                throw new ArgumentNullException(nameof(description));

            Name = name;
            Required = required;
            Value = value;
            Description = description;
        }
    }
}
