using System.Text;

namespace BuildTools
{
    class BuildToolsConfigWriter
    {
        private StringBuilder builder;

        private string DebuggerDisplay => builder.ToString();

        public BuildToolsConfigWriter(StringBuilder builder)
        {
            this.builder = builder;
        }

        public void WriteGroups(ConfigGroup[] groups)
        {
            builder.AppendLine("@{");

            for (var i = 0; i < groups.Length; i++)
            {
                WriteGroup(groups[i]);

                if (i < groups.Length - 1)
                    builder.AppendLine();
            }

            builder.AppendLine("}");
        }

        public void WriteGroup(ConfigGroup group)
        {
            WriteHeader(group.Name);

            builder.AppendLine();

            for (var i = 0; i < group.Settings.Length; i++)
            {
                WriteSetting(group.Settings[i]);

                if (i < group.Settings.Length - 1)
                    builder.AppendLine();
            }
        }

        public void WriteHeader(string name)
        {
            var topBorder = new string('#', 20);
            var sideBorder = new string('#', 4);

            WriteLine(topBorder);

            builder.Append("    ").Append(sideBorder);

            var remaining = topBorder.Length - (sideBorder.Length * 2) - name.Length;

            var div = remaining / 2;
            var odd = remaining % 2;

            builder.Append(new string(' ', odd == 0 ? div : div + 1));
            builder.Append(name);
            builder.Append(new string(' ', div));

            builder.AppendLine(sideBorder);

            WriteLine(topBorder);
        }

        public void WriteSetting(ConfigSetting setting)
        {
            WriteLineFormat("# {0}. {1}", setting.Required ? "Required" : "Optional", setting.Description);
            WriteLineFormat("{0}{1} = {2}", setting.Required || !setting.Value.IsDefault ? string.Empty : "# ", setting.Name, setting.Value.Value);
        }

        private void WriteLine(string value)
        {
            builder.Append("    ").AppendLine(value);
        }

        private void WriteLineFormat(string format, params object[] args) => WriteLine(string.Format(format, args));
    }
}
