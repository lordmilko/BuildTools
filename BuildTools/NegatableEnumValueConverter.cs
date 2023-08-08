using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace BuildTools
{
    class NegatableEnumValueConverter<T> : IValueConverter where T : struct, Enum
    {
        public static readonly NegatableEnumValueConverter<T> Instance = new NegatableEnumValueConverter<T>();

        public object Convert(object value)
        {
            if (value == null)
                return null;

            if (value is string)
                value = new[] { value };

            if (value.GetType().IsArray)
            {
                var arr = ((IEnumerable)value).Cast<object>().Select(v => v.ToString()).Where(v => !string.IsNullOrWhiteSpace(v)).ToArray();

                if (arr.Length == 0)
                    return new T[0];

                var toInclude = new List<T>();
                var toExclude = new List<T>();

                var allValues = Enum.GetValues(typeof(T)).Cast<T>().ToArray();

                foreach (var item in arr)
                {
                    if (item.StartsWith("~"))
                    {
                        //negated

                        if (item.Length == 1)
                            throw new InvalidOperationException($"Value '{item}' does not specify a {typeof(T).Name} to negate");

                        if (item[1] == '~')
                            throw new InvalidOperationException($"Cannot process {typeof(T).Name} value '{item}': '~' was specified multiple times");

                        var str = item.Substring(1);

                        var normal = (T) LanguagePrimitives.ConvertTo(str, typeof(T));

                        if (typeof(T) == typeof(Feature) && (Feature) (object) normal == Feature.System)
                            throw new InvalidOperationException($"{typeof(T).Name} '{normal}' cannot be excluded");

                        toExclude.Add(normal);
                    }
                    else
                    {
                        var normal = (T) LanguagePrimitives.ConvertTo(item, typeof(T));

                        toInclude.Add(normal);
                    }
                }

                if (toInclude.Count == 0)
                {
                    if (toExclude.Count == 0)
                        return null; //Include everything then!

                    var remaining = allValues.Except(toExclude).ToArray();

                    return remaining;
                }
                else //toInclude.Count > 0
                {
                    //We have at least 1 item to include. We may have excluded some of these or all of these

                    if (toExclude.Count == 0)
                        return toInclude;

                    return toInclude.Except(toExclude).ToArray();
                }
            }
            else
                return value; //Don't knoww what it is, can't handle it
        }
    }
}
