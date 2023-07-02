namespace BuildTools
{
    public class HelpConfig
    {
        public string Command { get; }

        public string Synopsis { get; set; }

        public string Description { get; set; }

        public HelpParameter[] Parameters { get; set; }

        public HelpExample[] Examples { get; set; }

        public IBuildCommand[] RelatedLinks { get; set; }

        public HelpConfig(string command)
        {
            Command = command;
        }
    }
}