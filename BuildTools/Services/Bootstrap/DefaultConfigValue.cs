namespace BuildTools
{
    class DefaultConfigValue : IConfigValue
    {
        public bool IsDefault => true;

        public string Value { get; }

        public DefaultConfigValue(string value)
        {
            Value = value;
        }

        public static implicit operator DefaultConfigValue(string value) => new DefaultConfigValue(value);
    }
}
