using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace BuildTools
{
    static class EnumExtensions
    {
        public static string GetDescription<T>(this T element, bool mandatory = true) where T : Enum
        {
            var memberInfo = element.GetType().GetMember(element.ToString());

            if (memberInfo.Length > 0)
            {
                var attributes = memberInfo.First().GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attributes.Length > 0)
                {
                    return ((DescriptionAttribute)attributes.First()).Description;
                }
            }

            if (!mandatory)
                return element.ToString();

            throw new InvalidOperationException($"{element} is missing a {nameof(DescriptionAttribute)}");
        }

        public static TEnum DescriptionToEnum<TEnum>(this string value, bool toStringFallback = true)
        {
            TEnum enumValue;

            if (TryParseDescriptionToEnum(value, out enumValue))
                return enumValue;

            if (!toStringFallback)
                throw new ArgumentException($"'{value}' is not a description for any value in {typeof(TEnum)}.", nameof(value));

            return (TEnum) Enum.Parse(typeof(TEnum), value, true);
        }

        public static bool TryParseDescriptionToEnum<TEnum>(this string str, out TEnum value)
        {
            if (TryParseDescriptionToEnum(str, typeof(TEnum), out var val))
            {
                value = (TEnum) val;
                return true;
            }

            value = default(TEnum);
            return false;
        }

        public static bool TryParseDescriptionToEnum(this string str, Type type, out object value)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            foreach (var field in fields)
            {
                var attribute = field.GetCustomAttribute<DescriptionAttribute>();

                if (attribute != null)
                {
                    if (attribute.Description.Equals(str, StringComparison.OrdinalIgnoreCase))
                    {
                        value = field.GetValue(null);
                        return true;
                    }
                }
                else
                {
                    if (field.Name == str)
                    {
                        value = field.GetValue(null);
                        return true;
                    }
                }
            }

            value = null;
            return false;
        }
    }
}
