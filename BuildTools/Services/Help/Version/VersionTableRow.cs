namespace BuildTools
{
    class VersionTableRow
    {
        public VersionType Property { get; }

        public string Source { get; }

        public VersionTableRow(VersionType property, string source)
        {
            Property = property;
            Source = source;
        }
    }
}