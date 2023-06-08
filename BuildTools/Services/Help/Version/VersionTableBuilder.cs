using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BuildTools
{
    class VersionTableBuilder : IEnumerable
    {
        public List<VersionTableRow> Records { get; } = new List<VersionTableRow>();

        public void Add(VersionType property, string source)
        {
            Records.Add(new VersionTableRow(property, source));
        }

        public override string ToString()
        {
            var items = Records.Select(r => new
            {
                r,
                d = r.Property.GetDescription()
            }).ToArray();

            var longestProperty = items.Max(i => i.r.Property.ToString().Length);
            var longestSource = items.Max(i => i.r.Source.ToString().Length);
            var longestDescription = items.Max(i => i.d.Length);

            var builder = new StringBuilder();

            void WriteRow(string property, string source, string description, char pad = ' ')
            {
                builder.Append("    | ")
                    .Append(property.PadRight(longestProperty, pad)).Append(" | ")
                    .Append(source.PadRight(longestSource, pad)).Append(" | ")
                    .Append(description.PadRight(longestDescription, pad)).AppendLine(" |");
            }

            WriteRow("Property", "Source", "Description");
            WriteRow("-", "-", "-", '-');

            foreach (var item in items)
                WriteRow(item.r.Property.ToString(), item.r.Source, item.d);

            return builder.ToString().TrimEnd();
        }

        public IEnumerator GetEnumerator() => Records.GetEnumerator();
    }
}