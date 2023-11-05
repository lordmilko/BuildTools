using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace BuildTools
{
    class HashTableConfigSettingValueProvider : DefaultConfigSettingValueProvider
    {
        private Hashtable hashTable;

        public HashTableConfigSettingValueProvider(Hashtable hashTable)
        {
            this.hashTable = hashTable;
        }

        public override IConfigValue String(string name)
        {
            if (!TryGetValue(name, out var value) || value == null)
                return base.String(name);

            if (value is string s)
                return (CustomConfigValue) $"'{s}'";

            if (value.GetType().IsArray)
                return Array(name);

            throw GetUnknownTypeException(name, value);
        }

        public override IConfigValue Array(string name)
        {
            if (!TryGetValue(name, out var value) || value == null)
                return base.Array(name);

            var type = value.GetType();

            if (!type.IsArray)
            {
                value = new[] { value };
                type = value.GetType();
            }

            var arr = (Array) value;

            if (arr.Length == 0)
                return (CustomConfigValue)"@()";

            var elementType = type.GetElementType();

            var enumTryParse = typeof(Enum).GetMethods().Single(m => m.Name == "TryParse" && m.IsGenericMethod && m.GetParameters().Length == 3).MakeGenericMethod(elementType);

            bool isEnumValue(object v)
            {
                if (v == null)
                    return false;

                if (v.GetType() == elementType)
                    return true;

                var args = new[] { v, true, null };

                if ((bool)enumTryParse.Invoke(null, args))
                    return true;

                return false;
            }

            string convertToEnum(object v)
            {
                if (v.GetType() == elementType)
                    return v.ToString();

                return Enum.Parse(elementType, v.ToString(), true).ToString();
            }

            string[] stringArray;

            var arrayTypes = new[]
            {
                typeof(object),
                typeof(Feature),
                typeof(CommandKind),
                typeof(TestType),
                typeof(PackageType)
            };

            if (elementType == typeof(string))
                stringArray = (string[])value;
            else if (arrayTypes.Contains(elementType))
            {
                var objectArray = ((IEnumerable)value).Cast<object>().ToArray();

                if (objectArray.All(o => o is string))
                    stringArray = objectArray.Cast<string>().ToArray();
                else if (elementType.IsEnum && objectArray.All(isEnumValue))
                    stringArray = objectArray.Select(convertToEnum).ToArray();
                else
                    throw new NotImplementedException($"Don't know how to parse array containing values of type {(string.Join(", ", objectArray.Select(v => v?.GetType().Name ?? "null").Distinct()))}");
            }
            else
                throw new NotImplementedException($"Don't know how to handle array with element type '{elementType.Name}' for config property '{name}'.");

            return (CustomConfigValue)string.Join(",", stringArray.Select(v => $"'{v}'"));
        }

        public override IConfigValue HashTable(string name)
        {
            if (!TryGetValue(name, out var value) || value == null)
                return base.HashTable(name);

            if (value is Hashtable ht)
            {
                var lower = name.ToLower();

                if (lower.Equals(nameof(ProjectConfig.PackageTests), StringComparison.OrdinalIgnoreCase))
                    return (CustomConfigValue) BuildPackageHashTable(ht, false, PackageTestHashTableComparer.Instance);

                if (lower.Equals(nameof(ProjectConfig.PackageFiles), StringComparison.OrdinalIgnoreCase))
                    return (CustomConfigValue) BuildPackageHashTable(ht, true, PackageFileHashTableComparer.Instance);

                throw new NotImplementedException($"Don't know how to handle {nameof(Hashtable)} for property '{name}'");
            }

            throw GetUnknownTypeException(name, value);
        }

        private string BuildPackageHashTable(Hashtable ht, bool gapBetweenLangs, IComparer<object> itemComparer)
        {
            var writer = new ProjectPackageConfigHashTableWriter();

            return writer.Execute(ht, gapBetweenLangs, itemComparer);
        }

        public override IConfigValue Null(string name)
        {
            if (!TryGetValue(name, out var value) || value == null)
                return base.Null(name);

            if (value is ScriptBlock sb)
                return (CustomConfigValue) sb.Ast.ToString();

            throw GetUnknownTypeException(name, value);
        }

        public override IConfigValue Bool(string name)
        {
            if (!TryGetValue(name, out var value) || value == null)
                return base.Bool(name);

            if (value is bool b)
                return (CustomConfigValue) (b ? "$true" : "$false");

            throw GetUnknownTypeException(name, value);
        }

        private Exception GetUnknownTypeException(string name, object value)
        {
            return new NotImplementedException($"Don't know how to serialize a value of type '{value.GetType().Name}' for config property '{name}'");
        }

        private bool TryGetValue(string name, out object value)
        {
            if (!hashTable.ContainsKey(name))
            {
                value = null;
                return false;
            }

            value = hashTable[name];
            return true;
        }
    }
}
