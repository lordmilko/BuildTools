namespace BuildTools
{
    interface IConfigSettingValueProvider
    {
        IConfigValue String(string name);

        IConfigValue Array(string name);

        IConfigValue HashTable(string name);

        IConfigValue Null(string name);

        IConfigValue Bool(string name);
    }
}
