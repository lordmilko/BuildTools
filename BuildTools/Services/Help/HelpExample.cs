namespace BuildTools
{
    public class HelpExample
    {
        public string Command { get; set; }

        public string Description { get; set; }

        public HelpExample(string command, string description)
        {
            Command = command;
            Description = description;
        }
    }
}