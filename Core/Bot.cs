using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

using DiscordBot.Utils;

namespace DiscordBot.Core
{
    public sealed partial class Bot
    {
        private static readonly Type thistype = typeof(Bot);
        private static          Bot  instance;

        private Bot()
        {
            //Setup the client
            this.client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel            = Discord.LogSeverity.Verbose,
                DefaultRetryMode    = Discord.RetryMode.AlwaysRetry,
                AlwaysDownloadUsers = true
            });

            //Allocate queue to hold messages
            this.messageQueue      = new Queue<SocketUserMessage>();

            //Set owner to unknown
            this.ownerID           = 0;

            //Set the context to unknown
            this.context           = null;

            //Initialise our command manager
            bool e = Commands.Manager.InitialiseManager();
            Debug.Assert(e, "Couldn't initialise manager!");
        }

        public static Bot GetBotInstance(bool initialiseIfNull = true)
        {
            //Check if already created
            if (instance == null && initialiseIfNull)
            {
                //Create a new Bot instance
                instance = new Bot();
            }

            //Return the instance
            return instance;
        }

        public void SetStatus(string status)
        {
            this.client.SetGameAsync(status).GetAwaiter().GetResult();
        }

        public void SendErrorMessage(ISocketMessageChannel channel, string title, string description, string footer = null, DateTimeOffset? offset = null)
        {
            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Red);

            //Add Footer
            if (footer != null) builder = builder.WithFooter(footer);

            //Add offset (TODO: The ToUniversalTime can be omitted in a later version of the API)
            if (offset.HasValue) builder = builder.WithTimestamp(offset.Value.ToUniversalTime());

            //Add the field
            var embed = builder.AddField(title, description).Build();

            //Send the message
            channel.SendMessageAsync("", false, embed).GetAwaiter().GetResult();
        }

        public void SendSuccessMessage(ISocketMessageChannel channel, string title, string description, string footer = null, DateTimeOffset? offset = null)
        {
            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);

            //Add Footer
            if (footer != null) builder = builder.WithFooter(footer);

            //Add offset (TODO: The ToUniversalTime can be omitted in a later version of the API)
            if (offset.HasValue) builder = builder.WithTimestamp(offset.Value.ToUniversalTime());

            //Add the field
            var embed = builder.AddField(title, description).Build();

            //Send the message
            channel.SendMessageAsync("", false, embed).GetAwaiter().GetResult();
        }

        public string GetUserName(ulong userID)
        {
            //Check if we have a context
            if (this.context != null)
            {
                //Try to get the user
                var user = this.context.GetUser(userID);

                //Check that it's not null
                if (user != null)
                {
                    //Return the Nickname or Username
                    return !string.IsNullOrEmpty(user.Nickname) ? user.Nickname
                                                                : user.Username;
                }    
            }

            //Just return the userID as a string
            return $"Unknown #{userID}";
        }

        public async Task Run()
        {
            //Get the owner
            this.ownerID = (ulong)(long)BotConfig.Config["discord_owner_id"];

            //Add logger for Discord API messages
            this.client.Log += Logger.Log;

            //Add ready handler
            this.client.Ready += () =>
            {
                //Set username
                this.client.CurrentUser.ModifyAsync(u =>
                {
                    u.Username = "Grand Inquisitor";
                });

                //Return completed
                return Task.CompletedTask;
            };

            //Capture Discord.NET failing to reconnect
            this.cts = new CancellationTokenSource();
            this.client.Connected += () =>
            {
                //Cancel any reconnects
                this.cts.Cancel();
                this.cts = new CancellationTokenSource();

                //Done
                return Task.CompletedTask;
            };
            this.client.Disconnected += ex =>
            {
                //Allow 30 seconds before we get suspicious
                _ = Task.Delay(TimeSpan.FromSeconds(30), this.cts.Token).ContinueWith(_ =>
                {
                    //Check the connection state
                    if (this.client.ConnectionState == ConnectionState.Connected) return;

                    //Log the issue
                    Logger.Log(LOG_LEVEL.ERROR, "Failed to reconnect within 30 seconds. Resetting...");

                    //Attempt to reset, giving us 30 seconds to reconnect
                    var timeout = Task.Delay(TimeSpan.FromSeconds(30));
                    var connect = this.client.StartAsync();
                    var task    = Task.WhenAny(timeout, connect).GetAwaiter().GetResult();

                    //Check if it was successful
                    if (task == connect && connect.IsCompletedSuccessfully)
                    {
                        //Log that we're fine
                        Logger.Log(LOG_LEVEL.INFO, "Successfully reconnected!");
                    }
                    else
                    {
                        //Check if we timed out
                        if (task == timeout)
                        {
                            //Log the error
                            Logger.Log(LOG_LEVEL.ERROR, "Failed to reset within 30 seconds. Killing process...");
                        }
                        //Check if it failed to reconnect
                        else if (connect.IsFaulted)
                        {
                            //Log the error
                            Logger.Log(LOG_LEVEL.ERROR, "Reset faulted. Killing process...");
                        }
                        //Something else
                        else
                        {
                            //Log the error
                            Logger.Log(LOG_LEVEL.ERROR, "Unknown issue. Killing process...");
                        }

                        //Kill the process fast
                        Environment.Exit(1);
                    }
                });

                //Done
                return Task.CompletedTask;
            };

            //Add message handler
            this.client.MessageReceived += this.Process;

            //Start message loop
            this.StartMessageLoop();

            //Login
            await client.LoginAsync(Discord.TokenType.Bot, (string)BotConfig.Config["discord_bot_token"]);
            await client.StartAsync();

            //Wait until program exit
            await Task.Delay(-1);
        }

        private void SendDM(ulong userID, string text)
        {
            try
            {
                //Get the user
                var user = client.GetUser(userID);

                //Create DM channel
                var channel = user.GetOrCreateDMChannelAsync().GetAwaiter().GetResult();

                //Send message
                channel.SendMessageAsync(text);
            }
            catch { }
        }

        public void NotifyOwner(string text)
        {
            //Send DM to owner
            this.SendDM(this.ownerID, text);
        }

        private void StartMessageLoop()
        {
            //Start thread to process messages
            Task.Factory.StartNew(() =>
            {
                //Catch any errors
                begin: Debug.Try(() =>
                {
                    //Loop forever
                    while (true)
                    {
                        //Reset the context
                        this.context = null;

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
                        Logger.Log("[" + msg.Author.Username + " -> " + msg.Channel.Name + "]: " + msg.Content);

                        //Skip our own messages
                        if (msg.Author.Id == client.CurrentUser.Id) continue;

                        //Save the context
                        this.context = (msg.Channel as SocketGuildChannel)?.Guild;

                        //Get all lines starting with one of !#$@ followed by a command
                        var matches = Regex.Matches(msg.Content, @"^[\!\#\$\@](\w+)", RegexOptions.Multiline);

                        //Iterate through the matches
                        for (int i = 0; i < matches.Count; i++)
                        {
                            //Get the substring to test against
                            string substr = null;
                            if (i < matches.Count - 1)
                            {
                                //Get the string between current match and the next match
                                substr = msg.Content.Substring(matches[i].Index, (matches[i + 1].Index - matches[i].Index));
                            }
                            else
                            {
                                //Get the rest of the string
                                substr = msg.Content.Substring(matches[i].Index);
                            }

                            //TODO: Run as a separate task, add timeout and shit
                            //Process the command
                            var e = Debug.Try(() => Commands.Manager.ProcessCommand(new Commands.Context { message = msg }, substr.Substring(1)));

                            //Check if it failed
                            if (!e)
                            {
                                //Send an error
                                this.SendErrorMessage(msg.Channel, "Error", "Command failed.");
                            }
                        }
                    }
                }, true);

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
        private Queue<SocketUserMessage>   messageQueue;
        private ulong                      ownerID;
        private SocketGuild                context;

        private CancellationTokenSource    cts;

        private readonly object            messageLock      = new object();
        private readonly Semaphore         messageSemaphore = new Semaphore(0, int.MaxValue);
    }
}
