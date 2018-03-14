using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Discord.WebSocket;

using DiscordBot.Utils;

namespace DiscordBot.Core
{
    public sealed partial class Bot
    {
        private static readonly Type thistype = typeof(Bot);

        public Bot()
        {
            //Setup the client
            this.client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel          = Discord.LogSeverity.Verbose,
                DefaultRetryMode  = Discord.RetryMode.AlwaysRetry
            });

            //Initialize to null
            this.config            = null;

            //Prepare the list holding command categories
            this.commandCategories = new List<CommandCategory>();

            //Allocate queue to hold messages
            this.messageQueue      = new Queue<SocketUserMessage>();

            //Set owner to unknown
            this.ownerID           = 0;

            //Mark that the Ready event has not fired yet
            this.hasInit           = false;
        }

        public async Task Run()
        {
            //Read the config
            this.config = Config.ReadConfig() ?? new Dictionary<string, string>();

            //Get the owner
            this.ownerID = ulong.Parse(this.config["OwnerID"]);

            //Grab the command initializers
            var cmdInits = CommandInit.GetCommandInitializers<Bot>();

            //Iterate over initializers
            foreach (var init in cmdInits)
            {
                //Invoke method
                init.Invoke(this, null);
            }

            //Add logger for Discord API messages
            this.client.Log += Logger.Log;

            //Add ready handler
            this.client.Ready += () =>
            {
                //Check that it's only run once
                if (!this.hasInit)
                {
                    //Set username
                    this.client.CurrentUser.ModifyAsync((u) =>
                    {
                        u.Username = "Grand Inquisitor";
                    });
                    
                    //Start notifications checks
                    this.StartNotificationsCheck();

                    //Mark as run
                    this.hasInit = true;
                }

                //Return completed
                return Task.CompletedTask;
            };

            //Add message handler
            this.client.MessageReceived += this.Process;

            //Start message loop
            this.StartMessageLoop();

            //Login
            await client.LoginAsync(Discord.TokenType.Bot, this.config["BotToken"]);
            await client.StartAsync();

            //Wait until program exit
            await Task.Delay(-1);
        }

        private string ProcessCommand(SocketUserMessage socketMsg, string commandText)
        {
            //Iterate over the categories
            foreach (CommandCategory cc in this.commandCategories)
            {
                //Check for match
                var categoryMatch = Regex.Match(commandText, cc.NameRegex, RegexOptions.IgnoreCase);
                if (categoryMatch.Success)
                {
                    //Skip the category name
                    var substr = commandText.Substring(categoryMatch.Length);

                    //Save bestMatch outside the scope
                    MatchResult bestMatch = null;

                    //Check that the string is not empty after skipping category name
                    if (!string.IsNullOrWhiteSpace(substr))
                    {
                        //Iterate over the commands
                        foreach (Command cmd in cc.Commands)
                        {
                            //Determine how well it matches
                            var match = cmd.CheckMatch(substr);

                            //Check for complete match
                            if (match.complete)
                            {
                                //Store match and skip further processing
                                bestMatch = match;
                                break;
                            }
                            //Name matches but there are parameters missing
                            else if (match.nameMatch == NAME_MATCH.Matched)
                            {
                                //Check if this match is better than what's currently stored
                                if
                                (
                                    ((bestMatch?.nameMatch ?? NAME_MATCH.None) != NAME_MATCH.Matched) ||
                                    (match.matchedSubstring > (bestMatch?.matchedSubstring ?? 0))
                                )
                                {
                                    //Store match
                                    bestMatch = match;
                                }
                            }
                            //Too many characters provided for the command
                            else if (match.nameMatch == NAME_MATCH.Substring)
                            {
                                //Check if no other match has been found
                                if ((bestMatch?.nameMatch ?? NAME_MATCH.None) == NAME_MATCH.None)
                                {
                                    //Store match
                                    bestMatch = match;
                                }
                            }
                            //TODO: Handle more advanced spelling mistakes?
                        }
                    }

                    //Check if we got some kind of match
                    if (bestMatch != null)
                    {
                        //Check for complete match
                        if (bestMatch.complete)
                        {
                            //Try to invoke the method
                            bestMatch.cmd.TryInvoke(socketMsg, bestMatch.parameters, out string s);
                            
                            //Return result
                            return s;
                        }
                        //Partial match
                        else if (bestMatch.nameMatch == NAME_MATCH.Matched)
                        {
                            //Check if we've matched more than just the name
                            if (bestMatch.matchedSubstring > bestMatch.cmd.Name.Length)
                            {
                                //Return error message from parser
                                return "```\n" +
                                       $"{categoryMatch.Value}{substr}\n" +
                                       (new String('-', categoryMatch.Length + bestMatch.matchedSubstring)) + "^```\n" +
                                       bestMatch.msg;
                            }
                            else
                            {
                                //Return help information for command
                                return bestMatch.cmd.GetHelp(socketMsg);
                            }
                        }
                        //Substring match
                        else
                        {
                            //Return "Did you mean"
                            if (!string.IsNullOrWhiteSpace(cc.Name))
                            {
                                return "Did you mean: $" + cc.Name + " " + bestMatch.cmd.Name;
                            }
                            else
                            {
                                return "Did you mean: $" + bestMatch.cmd.Name;
                            }
                        }
                    }
                    //No match found, check if we matched the category name
                    else if (!string.IsNullOrWhiteSpace(cc.Name))
                    {
                        //Return help information for category
                        return cc.GetHelp();
                    }
                }
            }

            //Unrecognised category
            return "Didn't recognise command.\nType $help for a full list of the supported commands.";
        }

        private void ProcessMessage(SocketUserMessage socketMsg)
        {
            //Get the message string
            string msg = Utility.ReplaceEmojies(socketMsg.Content);

            //Get all lines starting with "$somecommand"
            var matches = Regex.Matches(msg, @"^[\$!](\w+)", RegexOptions.Multiline);

            //Bailout if no matches
            if (matches.Count == 0) return;

            //Iterate through our matches
            for (int i=0; i < matches.Count; i++)
            {
                //Setup response string
                string resp = socketMsg.Author.Mention + " ";

                //Send initial message
                var responseMessage = socketMsg.Channel.SendMessageAsync
                (
                    resp + "processing..."
                ).GetAwaiter().GetResult();
            
                //Get the substring to test against
                string substr;
                if (i < matches.Count - 1)
                {
                    //Get the string between current match and the next match
                    substr = msg.Substring(matches[i].Index, (matches[i + 1].Index - matches[i].Index));
                }
                else
                {
                    //Get the rest of the string
                    substr = msg.Substring(matches[i].Index);
                }

                //Skip the $
                substr = substr.Substring(1);

                //Trim it
                substr = substr.Trim();

                //Parse and execute if match is found
                resp += this.ProcessCommand(socketMsg, substr);

                //Check that the response is not too long
                if (resp.Length < 2000)
                {
                    //Update response message
                    responseMessage.ModifyAsync((prop) => prop.Content = resp);
                }
                else
                {
                    //Write error
                    responseMessage.ModifyAsync((prop) => prop.Content = socketMsg.Author.Mention + " Error: Response is too long!");

                    //Skip remaining commands
                    break;
                }
            }
        }

        private void AcknowledgeMessage(SocketUserMessage m)
        {
            //Get a lock to prevent race conditions
            lock (this.importantMessageLock)
            {
                //Check if there even are any messages we need to acknowledge
                if (this.importantMessages.Count == 0) return;

                //Iterate over messages
                foreach (QueuedMessage msg in this.importantMessages)
                {
                    //Check if the recipient matches
                    if (((m.Channel as SocketDMChannel)?.Recipient?.Id ?? 0) == msg.userID)
                    {
                        //Check if content matches
                        if (m.Content.Trim().Equals(msg.text.Trim()))
                        {
                            //Remove from queue
                            this.importantMessages.Remove(msg);

                            //Skip any further checks
                            return;
                        }
                    }
                }
            }
        }

        private void StartMessageLoop()
        {
            //Start thread to process messages
            Task.Factory.StartNew(() =>
            {
                begin:

                //Catch any errors
                try
                {
                    //Loop forever
                    while (true)
                    {
                        //Wait for signal
                        this.messageSemaphore.WaitOne();

                        //Lock to prevent race conditions
                        SocketUserMessage msg = null;
                        lock (this.messageLock)
                        {
                            //Pop message off the queue
                            msg = this.messageQueue.Dequeue();
                        }

                        //Log message
                        Logger.Log("[" + msg.Author.Username + " -> " + msg.Channel.Name + "]: " + Utility.ReplaceEmojies(msg.Content));

                        //Check if it's our own message
                        if (msg.Author.Id == client.CurrentUser.Id)
                        {
                            //Check if it's a message awaiting acknowledgement
                            this.AcknowledgeMessage(msg);

                            //Skip further work
                            continue;
                        }

                        //TODO: Run as a separate task, add timeout and shit
                        //Process message further
                        this.ProcessMessage(msg);
                    }
                }
                catch (AggregateException ae)
                {
                    //Get errors
                    ae.Handle((ex) =>
                    {
                        //Log error
                        Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                        //Mark as handled
                        return true;
                    });
                }
                catch (Exception ex)
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);
                }

                //Go back to the beginning
                goto begin;
            }, TaskCreationOptions.LongRunning)
            //Capture exit
            .ContinueWith((t) =>
            {
                //Get errors
                t?.Exception?.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                    //Mark as handled
                    return true;
                });

                //Log exit
                Logger.Log(LOG_LEVEL.ERROR, "Message processor exited!");
            });
        }

        public Task Process(SocketMessage socketMsg)
        {
            //Check that it's a user message
            if (!(socketMsg is SocketUserMessage)) return Task.CompletedTask;

            //Cast to user message
            var msg = socketMsg as SocketUserMessage;

            //Lock to prevent race conditions
            lock (this.messageLock)
            {
                //Push message onto the queue
                this.messageQueue.Enqueue(msg);
            }

            //Release semaphore
            this.messageSemaphore.Release();

            //Return success
            return Task.CompletedTask;
        }

        private DiscordSocketClient        client;
        private Dictionary<string, string> config;
        private List<CommandCategory>      commandCategories;
        private Queue<SocketUserMessage>   messageQueue;
        private ulong                      ownerID;
        private bool                       hasInit;

        private readonly object            messageLock      = new object();
        private readonly Semaphore         messageSemaphore = new Semaphore(0, int.MaxValue);
    }
}
