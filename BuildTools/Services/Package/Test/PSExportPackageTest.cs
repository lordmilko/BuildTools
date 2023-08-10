using System;
using System.Linq;
using System.Management.Automation;

namespace BuildTools
{
    public class PSExportPackageTest : IPackageTest
    {
        public string Command { get; }

        public CommandTypes Type { get; }

        public PSExportPackageTest(string command, CommandTypes type)
        {
            Command = command;
            Type = type;
        }

        public void Test(object exports)
        {
            if (exports != null)
            {
                if (exports is string s)
                {
                    if (s == Command)
                        return;
                }
                else if (exports is object[] oa)
                {
                    if (oa.Cast<string>().Contains(Command))
                        return;
                }
            }

#pragma warning disable CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).
            var word = Type switch
            {
                CommandTypes.Cmdlet => "cmdlets",
                CommandTypes.Alias => "aliases"
            };
#pragma warning restore CS8509 // The switch expression does not handle all possible values of its input type (it is not exhaustive).

            var actual = exports is object[] arr ? string.Join(", ", arr) : exports?.ToString() ?? "null";

            throw new InvalidOperationException($"Module manifest was not updated to specify exported {word}. Expected {word} to contain '{Command}' however value was '{actual}'");
        }
    }
}
