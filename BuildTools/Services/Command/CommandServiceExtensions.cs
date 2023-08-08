namespace BuildTools
{
    internal static class CommandServiceExtensions
    {
        public static string GetCommandNameOrDefault(this ICommandService commandService, CommandKind kind) => commandService.GetOptionalCommand(kind)?.Name ?? "<COMMAND UNAVAILABLE>";
    }
}
