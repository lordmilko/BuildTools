using System;
using System.Collections.Generic;

namespace BuildTools
{
    public class ProjectConfigResolutionContext
    {
        private Dictionary<string, object> dict = new Dictionary<string, object>();

        //We only set the fields we need for certain contexts. Attempting to access a property we haven't set will generate an exception

        public bool? IsLegacy
        {
            get => GetSetting<bool>(nameof(IsLegacy));
            set => SetSetting(nameof(IsLegacy), value);
        }

        private T GetSetting<T>(string name)
        {
            if (dict.TryGetValue(name, out var value))
                return (T)value;

            throw new InvalidOperationException($"Property '{name}' is not set.");
        }

        private void SetSetting(string name, object value)
        {
            dict[name] = value;
        }
    }
}