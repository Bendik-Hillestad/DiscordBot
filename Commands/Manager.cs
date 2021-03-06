﻿using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Linq;

using DiscordBot.Core;
using DiscordBot.Utils;

namespace DiscordBot.Commands
{
    public static class StringExtension
    {
        public static IEnumerable<string> ReadLines(this string str)
        {
            //Wrap in a string reader
            using (var sr = new StringReader(str))
            {
                //Read the lines
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    //Yield the line
                    yield return line;
                }
            }
        }
    }

    public sealed class Manager
    {
        private static List<CommandModuleBase> commandModules = null;

        public static bool InitialiseManager()
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Check that we have no already loaded the modules
                if (Manager.commandModules == null)
                {
                    //Load the modules
                    Manager.commandModules = ModuleManager.GetAllModules(Assembly.GetExecutingAssembly());
                }
            });
        }

        public static void ProcessCommand(Context ctx, string message)
        {
            //Go through each module and check for matching commands
            var results = Manager.commandModules.Select(m => CommandProcessor.ProcessCommand(m, message))
                                                .ToList();

            //Select the best module
            var bestModule = results.OrderByDescending(matches => matches.Max(m => m.signatureMatch))
                                    .First();

            //Check if we have a full match
            if (bestModule.Exists(m => m.signatureMatch == m.signatureLength))
            {
                //Grab it
                var fullMatch = bestModule.Where(m => m.signatureMatch == m.signatureLength)
                                          .OrderByDescending(m => m.signatureMatch).First();

                //Check if there are additional characters after the matched string
                if (message.Length > fullMatch.inputMatch)
                {
                    //Extract the remaining bit and read the first line
                    var line = message.Substring(fullMatch.inputMatch)
                                      .ReadLines().First();

                    //Check if they are all whitespace
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        //Send to further processing
                        ProcessFullMatch(ctx, message, fullMatch);
                        return;
                    }
                }
                else
                {
                    //Send to further processing
                    ProcessFullMatch(ctx, message, fullMatch);
                    return;
                }
            }
            
            //Find the length of the best match
            var filter = bestModule.Max(cm => cm.signatureMatch);

            //Get the best matches and sort by length
            var bestMatches = bestModule.Where(cm => cm.signatureMatch == filter)
                                        .OrderByDescending(cm => cm.signatureLength)
                                        .ToList();

            //Check that we matched at least one character (TODO: Improve this in the future)
            if (filter > 0)
            {
                //Send to further processing
                ProcessPartialMatch(ctx, message, bestMatches);
            }
        }

        private static void ProcessFullMatch(Context ctx, string message, CommandMatch match)
        {
            //Insert the context into the match
            match.extractedParams[0] = ctx;

            //Catch any failed preconditions
            try
            {
                //Catch exceptions thrown by the target and rethrow them
                try
                {
                    //Invoke the command
                    match.cmd.Invoke(match.module, match.extractedParams);
                }
                catch (TargetInvocationException ex)
                {
                    //Rethrow the actual exception
                    ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                }
            }
            catch (PreconditionException ex)
            {
                //Send error
                Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel, "Error:", ex.Message);
            }
        }

        private static void ProcessPartialMatch(Context ctx, string message, IEnumerable<CommandMatch> matches)
        {
            //Get the part we match
            var matched = message.Substring(0, matches.First().inputMatch);

            //Generate all the missing parts
            var missing = matches.Select(cm => CommandManager.FormatCommandSignature(cm.cmd, cm.signatureMatch));

            //Generate the full expected strings
            var expected = string.Join("\n", missing.Select(m => matched + m));

            //Send the message
            Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel, "Expected:", expected);

            //TODO: Inject ModuleManager.GetModuleInstance(matches.First().cmd.DeclaringType).HelpMessage(ctx); into description?
        }
    }
}
