namespace BuildTools
{
    class ConfigGroup
    {
        public string Name { get; }

        public ConfigSetting[] Settings { get; }

        public ConfigGroup(string name, ConfigSetting[] settings)
        {
            Name = name;
            Settings = settings;
        }
    }
}