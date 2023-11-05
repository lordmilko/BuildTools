using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Text;

namespace BuildTools
{
    class ProjectPackageConfigHashTableWriter
    {
        private StringBuilder builder = new StringBuilder();

        private string DebuggerDisplay => builder.ToString();

        private bool isNewLine; //when we do a newline, the next write should apply indentations
        private int indentation;

        public string Execute(Hashtable root, bool gapBetweenLangs, IComparer<object> itemComparer)
        {
            WriteHtOpen();
            WriteLine();
            Indent();

            //The root Hashtable is indented so actually we should indent again
            Indent();

            var keys = root.Keys.Cast<object>().OrderBy(v => v).ToArray();

            for (var i = 0; i < keys.Length; i++)
            {
                //e.g. "C#"=
                var key = keys[i];
                Write($"\"{key}\"=");

                var value = root[key];

                if (value == null)
                    throw new InvalidOperationException($"Expected key '{key}' to have a value, but it was null.");

                if (!value.GetType().IsArray)
                    throw new NotImplementedException($"Expected key '{key}' to contain an array, however it contained a value of type '{value.GetType().Name}'.");

                //e.g. "C#"=@(
                WriteArrayOpen();
                Indent();

                WriteMainArrayItems(((IEnumerable)value).Cast<object>().ToArray(), itemComparer);

                Dedent();
                WriteArrayClose();

                if (i < keys.Length - 1 && gapBetweenLangs)
                    WriteLine();
            }

            Dedent();
            Write("}"); //Don't want to add a newline because its the end of the item, we'll 

            return builder.ToString();
        }

        private void WriteMainArrayItems(object[] arr, IComparer<object> itemComparer)
        {
            Type lastType = null;

           var itemWidths = GetHashTableItemWidths(arr, itemComparer);

            foreach (var item in arr)
            {
                var currentType = item.GetType();

                if (lastType != null)
                {
                    if (lastType != currentType)
                        WriteLine();
                }

                lastType = currentType;

                if (item is string s)
                    WriteLine($"\"{s}\"");
                else if (item is Hashtable ht)
                    WriteMainArrayHashTableItem(ht, itemComparer, itemWidths);
                else
                    throw new NotImplementedException($"Don't know how to handle an array item of type '{item.GetType().Name}'");
            }
        }

        private int[] GetHashTableItemWidths(object[] arr, IComparer<object> itemComparer)
        {
            var hashTables = arr.OfType<Hashtable>().Select(h => new
            {
                HT = h,
                Keys = h.Keys.Cast<object>().OrderBy(v => v, itemComparer).ToArray()
            }).ToArray();

            var results = new List<int>();

            var maxKeys = hashTables.Length == 0 ? 0 : hashTables.Max(h => h.Keys.Length);

            for (var keyIdx = 0; keyIdx < maxKeys; keyIdx++)
            {
                var lengths = new List<int>();

                for (var htIdx = 0; htIdx < hashTables.Length; htIdx++)
                {
                    var ht = hashTables[htIdx];

                    if (keyIdx < ht.Keys.Length)
                    {
                        var key = ht.Keys[keyIdx];
                        var val = ht.HT[key];

                        var value = GetHashTableItemString(val);

                        lengths.Add(value.Length);
                    }
                }

                var max = lengths.Max();

                results.Add(max);
            }

            return results.ToArray();
        }

        private void WriteMainArrayHashTableItem(Hashtable ht, IComparer<object> itemComparer, int[] itemWidths)
        {
            var itemKeys = ht.Keys.Cast<object>().OrderBy(v => v, itemComparer).ToArray();

            WriteHtOpen();
            builder.Append(" ");

            for (var j = 0; j < itemKeys.Length; j++)
            {
                var itemKey = itemKeys[j];

                var itemValue = ht[itemKey];

                var itemStr = GetHashTableItemString(itemValue);

                if (j < itemKeys.Length - 1)
                    itemStr = itemStr.PadRight(itemWidths[j]);

                builder.AppendFormat("{0} = {1}", itemKey, itemStr);

                if (j < itemKeys.Length - 1)
                    builder.Append("; ");
            }

            builder.Append(" ");
            WriteHtClose();
        }

        private string GetHashTableItemString(object value)
        {
            if (value is string s)
                return $"\"{s}\"";

            if (value is ScriptBlock sb)
                return sb.Ast.ToString();

            throw new NotImplementedException($"Don't know how to handle a hashtable item of type '{value.GetType().Name}'.");
        }

        private void WriteHtOpen() => Write("@{");
        private void WriteHtClose() => WriteLine("}");
        private void WriteArrayOpen() => WriteLine("@(");
        private void WriteArrayClose() => WriteLine(")");

        private void Indent() => indentation++;
        private void Dedent() => indentation--;

        private void WriteLine(string value = null)
        {
            Write(value);
            builder.Append(Environment.NewLine);
            isNewLine = true;
        }

        private void Write(string value)
        {
            if (value == null)
                return;

            if (isNewLine)
            {
                for (var i = 0; i < indentation; i++)
                    builder.Append("    ");

                isNewLine = false;
            }

            builder.Append(value);
        }
    }
}
