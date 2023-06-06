using System;

namespace BuildTools
{
    interface IConsoleLogger
    {
        void Log(string message, ConsoleColor? color);
    }

    class ConsoleLogger : IConsoleLogger
    {
        public void Log(string message, ConsoleColor? color)
        {
            if (color != null)
            {
                var original = Console.ForegroundColor;

                try
                {
                    Console.ForegroundColor = color.Value;

                    Console.WriteLine(message);
                }
                finally
                {
                    Console.ForegroundColor = original;
                }
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }
}