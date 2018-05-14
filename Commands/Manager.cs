﻿using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DiscordBot.Core;

namespace DiscordBot.Commands
{
    public sealed class Manager
    {
        private static List<CommandModuleBase> commandModules = null;

        public static void ProcessCommand(Context ctx, int commandOffset)
        {
            //Grab the message
            var message = ctx.message.Content.Substring(commandOffset);

            //Get the modules
            if (Manager.commandModules == null)
                Manager.commandModules = ModuleManager.GetAllModules(Assembly.GetExecutingAssembly());

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
                var fullMatch = bestModule.First(m => m.signatureMatch == m.signatureLength);

                //Send to further processing
                ProcessFullMatch(ctx, message, fullMatch);
            }
            else
            {
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
        }

        private static void ProcessFullMatch(Context ctx, string message, CommandMatch match)
        {
            //Insert the context into the match
            match.extractedParams[0] = ctx;

            //Invoke the command
            match.cmd.Invoke(match.module, match.extractedParams);
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
