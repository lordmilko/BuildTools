namespace BuildTools
{
    class DefaultConfigSettingValueProvider : IConfigSettingValueProvider
    {
        public static readonly DefaultConfigSettingValueProvider Instance = new DefaultConfigSettingValueProvider();

        public virtual IConfigValue String(string name) => (DefaultConfigValue) "''";

        public virtual IConfigValue Array(string name) => (DefaultConfigValue) "@()";

        public virtual IConfigValue HashTable(string name) => (DefaultConfigValue) "@{}";

        public virtual IConfigValue Null(string name) => (DefaultConfigValue) "$null";

        public virtual IConfigValue Bool(string name) => (DefaultConfigValue) "$false";
    }
}
