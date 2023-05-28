namespace BuildTools
{
    public class HelpParameter
    {
        public string Name { get; set; }

        public string Description { get; set; }

        public HelpParameter(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}