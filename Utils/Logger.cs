using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DiscordBot.Utils
{
    public enum LOG_LEVEL
    {
        INFO,
        WARNING,
        ERROR
    }

    public static class Logger
    {
        private static readonly object o = new object();

        public static void Log(string message)
        {
            lock (Logger.o)
            {
                //Write message
                Console.WriteLine($"{message}");
            }
        }

        public static void Log
        (
            LOG_LEVEL severity,
            string    message,
            [CallerMemberName] string method  = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            lock (Logger.o)
            {
                //Save old foreground color
                var fc = Console.ForegroundColor;

                //Set new color based on severity
                switch (severity)
                {
                    case LOG_LEVEL.INFO:
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    } break;

                    case LOG_LEVEL.WARNING:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    } break;

                    case LOG_LEVEL.ERROR:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    } break;
                }

                //Write message
                Console.WriteLine($"[{DateTime.Now}| {severity,8}] {method},{lineNumber}: {message}");

                //Restore foreground color
                Console.ForegroundColor = fc;
            }

            //If error we also notify the owner
            if (severity == LOG_LEVEL.ERROR) DiscordBot.Core.Bot.GetBotInstance().NotifyOwner($"[{DateTime.Now}| {severity,8}] {method},{lineNumber}: {message}");
        }

        public static Task Log(Discord.LogMessage msg)
        {
            lock (Logger.o)
            {
                //Save old foreground color
                var fc = Console.ForegroundColor;

                //Set new color based on severity
                switch (msg.Severity)
                {
                    case Discord.LogSeverity.Debug:
                    case Discord.LogSeverity.Verbose:
                    case Discord.LogSeverity.Info:
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                    } break;

                    case Discord.LogSeverity.Warning:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    } break;

                    case Discord.LogSeverity.Critical:
                    case Discord.LogSeverity.Error:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    } break;
                }

                //Write message
                Console.WriteLine($"[{DateTime.Now}| {msg.Severity,8}] {msg.Source}: {msg.Message}");

                //Restore foreground color
                Console.ForegroundColor = fc;
            }

            //Return done
            return Task.CompletedTask;
        }
    }
}
