namespace BuildTools
{
    class CustomConfigValue : IConfigValue
    {
        public bool IsDefault => false;
        public string Value { get; }

        public CustomConfigValue(string value)
        {
            Value = value;
        }

        public static implicit operator CustomConfigValue(string value) => new CustomConfigValue(value);
    }
}
