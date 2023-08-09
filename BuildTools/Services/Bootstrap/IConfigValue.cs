namespace BuildTools
{
    interface IConfigValue
    {
        bool IsDefault { get; }

        string Value { get; }
    }
}
