using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

using Discord;
using Discord.WebSocket;

using DiscordBot.Raids;
using DiscordBot.Utils;

namespace DiscordBot.Core
{
    public sealed partial class Bot
    {
        private static readonly string RAID_CALENDAR_FILE = "raid_calendar.xml";

        private struct QueuedMessage
        {
            public ulong  userID;
            public string text;
        }

        [CommandInit]
        private void ConstructRaidCommands()
        {
            //Register our Raid command category + commands
            this.commandCategories.Add
            (
                new CommandCategory("raid", thistype, "CmdRaidHelp")
                .RegisterCommand
                (
                    new Command
                    (
                        "create", this, "CmdRaidCreate", "CmdRaidCreateHelp",
                        "Day/Month",    @"(3[01]|[12][0-9]|0?[1-9])/(1[0-2]|0?[1-9])(?:$|\s)",
                        "HH:MM",        @"(2[0-3]|[01]?[0-9]):([0-5][0-9])(?:$|\s)",
                        "UTC timezone", @"UTC\s*([+-])\s*(1[0-2]|0?[0-9])(?:$|\s)",
                        "Description",  @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "create", this, "CmdRaidCreateTodayOrTomorrow", "CmdRaidCreateHelp",
                        "today or tomorrow", @"(today|tomorrow)(?:$|\s)",
                        "HH:MM",             @"(2[0-3]|[01]?[0-9]):([0-5][0-9])(?:$|\s)",
                        "UTC timezone",      @"UTC\s*([+-])\s*(1[0-2]|0?[0-9])(?:$|\s)",
                        "Description",       @"(.+?)$"
                    )
                )
                //TODO: Raid complete stuff, gw2raidheroes? GitHub page etc?
                //Upload logs to bot, store in raids/{id} folder?
                /*.RegisterCommand
                (
                    new Command
                    (
                        "complete", this, "CmdRaidComplete", "CmdRaidCompleteHelp",
                        "ID", @"(\d+)(?:$|\s)",
                        "HH:MM", @"(2[0-3]|[01]?[0-9]):([0-5][0-9])(?:$|\s)",
                        "Notes", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "complete", this, "CmdRaidCompleteSimple", "CmdRaidCompleteHelp",
                        "HH:MM", @"(2[0-3]|[01]?[0-9]):([0-5][0-9])(?:$|\s)",
                        "Notes", @"(.+?)$"
                    )
                )*/
                .RegisterCommand
                (
                    new Command
                    (
                        "delete", this, "CmdRaidDelete", "CmdRaidDeleteHelp",
                        "ID", @"(\d+)(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "roster", this, "CmdRaidRosterFilter", "CmdRaidRosterHelp",
                        "ID",     @"(\d+)(?:$|\s)",
                        "Filter", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "roster", this, "CmdRaidRoster", "CmdRaidRosterHelp",
                        "ID", @"(\d+)(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "list", this, "CmdRaidList", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "join", this, "CmdRaidJoin", "CmdRaidJoinHelp",
                        "ID",    @"(\d+)(?:$|\s)",
                        "roles", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "join", this, "CmdRaidJoinSimple", "CmdRaidJoinHelp",
                        "roles", @"(\D.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "leave", this, "CmdRaidLeave", "CmdRaidLeaveHelp",
                        "ID", @"(\d+)(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "notify", this, "CmdRaidNotifyRemove", "CmdRaidNotifyHelp",
                        "remove", @"remove(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "notify", this, "CmdRaidNotify", "CmdRaidNotifyHelp",
                        "times", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "make", this, "CmdRaidMakeComp", "CmdRaidMakeCompHelp",
                        "comp", @"comp(?:$|\s)",
                        "name", @"(\w+)(?:$|\s)",
                        "ID",   @"(\d+)(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "make", this, "CmdRaidMakeCompSimple", "CmdRaidMakeCompHelp",
                        "comp", @"comp(?:$|\s)",
                        "name", @"(\w+)(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "make", this, "CmdRaidMakeCompSimplest", "CmdRaidMakeCompHelp",
                        "comp", @"comp(?:$|\s)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "create", this, "CmdRaidCreateComp", "CmdRaidCreateCompHelp",
                        "comp",  @"comp(?:$|\s)",
                        "name",  @"(\w+)(?:$|\s)",
                        "roles", @"([\S\s]+)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "help", this, "CmdRaidHelp", null
                    )
                )
            );

            //Create list to hold important messages
            this.importantMessages = new List<QueuedMessage>();

            //Load raid config
            this.raidConfig = RaidConfig.ReadConfig();

            //Compile
            this.raidConfig.GenerateSolverLibrary();

            //Search for the saved raid calendar
            if (File.Exists(RAID_CALENDAR_FILE))
            {
                //Catch any errors
                try
                {
                    //Try to load from file
                    this.raidCalendar = RaidCalendar.LoadFromFile(RAID_CALENDAR_FILE);
                }
                catch (Exception ex)
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, "Couldn't load " + RAID_CALENDAR_FILE + "!\n" + ex.Message);

                    //Load default
                    this.raidCalendar = new RaidCalendar();
                }
            }
            else
            {
                //Load default
                this.raidCalendar = new RaidCalendar();
            }
        }

        /* Spooky unsafe C++ stuff */

        [DllImport("libherrington")]
        private static extern void solve(uint idx, IntPtr users, int length, IntPtr output);

        /* End of spooky unsafe C++ stuff */

        /* Nice C# wrapper for spooky unsafe C++ stuff */

        private Raider[] MakeRaidComp(List<Raider> raiders, int compIdx)
        {
            //Get the roles
            var roles     = this.raidConfig.GetRoles();

            //Get the size of the composition
            var compSize  = this.raidConfig.Compositions[compIdx].Layout.Count;

            //Allocate an array to hold the data that we feed into the solver
            var userSize  = this.raidConfig.GetUserSizeInBytes();
            var input     = Marshal.AllocHGlobal(userSize * raiders.Count);

            //Allocate an array to hold the output data
            var blockSize = this.raidConfig.GetOutputBlockSizeInBytes();
            var output    = Marshal.AllocHGlobal(blockSize * compSize);

            //Iterate through the raiders
            var offset = input; var idx = 0;
            raiders.ForEach((r) =>
            {
                //Write the id into the array
                Marshal.WriteInt64(offset, (long)r.ID);
                var next = offset + userSize;
                offset  += sizeof(ulong);

                //Calculate a bias by squashing the join index into the [0, 1] range
                float bias = 1.0f - (idx / (idx + 2.0f * compSize));

                //Iterate through the roles
                roles.ForEach((role) =>
                {
                    //Get the weight for this role
                    float weight = r.GetRoleWeight(role);

                    //Adjust the weight
                    weight = weight * bias;

                    //Write the weight into the array
                    Marshal.WriteInt32(offset, BitConverter.SingleToInt32Bits(weight));
                    offset += sizeof(float);
                });

                //Update offset and index
                offset = next; idx++;
            });

            //Feed values to the solver
            solve((uint)compIdx, input, raiders.Count, output);

            //Prepare result
            Raider[] result = new Raider[compSize];

            //Iterate over the output array
            offset = output;
            for (int i = 0; i < compSize; i++)
            {
                //Get the id
                var id = (ulong)Marshal.ReadInt64(output + (i * blockSize));

                //Check that it's not zero
                if (id != 0)
                {
                    //Insert the right raider
                    result[i] = raiders.Find((r) => id == r.ID);
                }
                else result[i] = null;
            }

            //Release our unmanaged memory
            Marshal.FreeHGlobal(input);
            Marshal.FreeHGlobal(output);

            //Return the result
            return result;
        }

        private void SendDM(ulong userID, string text)
        {
            //Create DM channel
            client.GetUser(userID).GetOrCreateDMChannelAsync()
            .ContinueWith((t) =>
            {
                //Send message
                t.Result.SendMessageAsync(text).GetAwaiter().GetResult();
            }, TaskContinuationOptions.OnlyOnRanToCompletion).GetAwaiter().GetResult();
        }

        private void QueueImportantMessage(ulong userID, string text)
        {
            //Lock to prevent race conditions
            lock (this.importantMessageLock)
            {
                //Insert message into list
                this.importantMessages.Add(new QueuedMessage { userID = userID, text = text });
            }

            //Catch any exceptions
            try
            {
                //Launch a thread that tries to send the message within 5 seconds
                var tokenSource = new CancellationTokenSource();
                var token       = tokenSource.Token;
                var task        = Task.Run(() =>
                {
                    //Grab the thread handle
                    var thread = Thread.CurrentThread;

                    //Set it to abort when cancellation is requested
                    using (token.Register(thread.Abort))
                    {
                        //Check if we're connected
                        while (this.client.ConnectionState != ConnectionState.Connected) Thread.Sleep(100);

                        //Send the message
                        this.SendDM(userID, text);
                    }
                }, token);

                //Allow 5 seconds for the message to be sent
                tokenSource.CancelAfter(5000);
                task.Wait(token);
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
        }

        private void ResendImportantMessages()
        {
            //Acquire a lock to prevent race conditions
            QueuedMessage[] messages = null;
            lock (this.importantMessageLock)
            {
                //Just stop if there are no messages
                if (this.importantMessages.Count == 0) return;

                //Make a local copy of the messages
                messages = new QueuedMessage[this.importantMessages.Count];
                this.importantMessages.CopyTo(messages);
            }

            //Catch any exceptions
            try
            {
                //Launch a thread that attempts to send as many as possible within 5 seconds
                var tokenSource = new CancellationTokenSource();
                var token       = tokenSource.Token;
                var task        = Task.Factory.StartNew(() =>
                {
                    //Grab the thread handle
                    var thread = Thread.CurrentThread;

                    //Set it to abort when cancellation is requested
                    using (token.Register(thread.Abort))
                    {
                        //Iterate over messages
                        for (int i = 0; i < messages.Length; i++)
                        {
                            //Send the message
                            this.SendDM(messages[i].userID, messages[i].text);
                        }
                    }
                }, token);

                //Allow 5 seconds for the messages to be sent
                tokenSource.CancelAfter(5000);
                task.Wait(token);
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
        }

        public void StartNotificationsCheck()
        {
            //Launch monitor thread
            var monitor = Task.Factory.StartNew(async () =>
            {
                begin:

                //TODO: Remove all of this crap and rely on Discord.NET?

                //Catch any errors
                try
                {
                    //Launch work thread
                    var thread = await Task.Factory.StartNew(async () =>
                    {
                        //Begin heartbeat
                        var heartbeat        = true;
                        var heartbeatChannel = await client.GetUser(this.ownerID).GetOrCreateDMChannelAsync();
                        var heartbeatMsg     = await heartbeatChannel.SendMessageAsync("Heartbeat: .");

                        //Define our delta time
                        var dt = TimeSpan.FromSeconds(30);

                        //Get current time
                        var now = DateTime.UtcNow;

                        //Get the time for next check
                        var next = now + dt;

                        //Loop forever
                        while (true)
                        {
                            //Resend any important messages
                            this.ResendImportantMessages();

                            //TODO: Thread safety
                            //Check if we need to notify anyone
                            var list = this.raidCalendar.CheckReminders(now, next);
                            if ((list?.Count ?? 0) > 0)
                            {
                                Logger.Log(LOG_LEVEL.INFO, "NOTIFYING " + list.Count + " USER(S).");

                                //Iterate over notifications
                                foreach (var notification in list)
                                {
                                    Logger.Log(LOG_LEVEL.INFO, "LAUNCHING THREAD!");

                                    //TODO: Just have one thread, use a semaphore, sort notifications and just iterate through them
                                    //Launch a thread that notifies the user at the right time
                                    var task = Task.Run(async () =>
                                    {
                                        await this.NotifyUser
                                        (
                                            notification.raidID, notification.userID,
                                            notification.time,   notification.hours,
                                            notification.minutes
                                        );
                                    });
                                }
                            }

                            //Refresh channel
                            heartbeatChannel = await client.GetUser(this.ownerID).GetOrCreateDMChannelAsync();

                            //Edit message
                            heartbeat = !heartbeat;
                            await heartbeatMsg.ModifyAsync((prop) => prop.Content = "Heartbeat: " + (heartbeat ? "." : ". ."));

                            //Wait
                            var wait = (next - DateTime.UtcNow);
                            if (wait > TimeSpan.Zero) await Task.Delay(wait);

                            //Update current time
                            now = next;

                            //Update time for next check
                            next = DateTime.UtcNow + dt;
                        }
                    }, TaskCreationOptions.LongRunning);

                    //Wait
                    thread.Wait();
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

                //Wait one second
                Thread.Sleep(1000);

                //Restart
                Logger.Log(LOG_LEVEL.INFO, "Restarting notification checker.");
                goto begin;
            }, TaskCreationOptions.LongRunning).GetAwaiter().GetResult()
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
                Logger.Log(LOG_LEVEL.ERROR, "Notification checker exited!");
            });
        }

        private async Task NotifyUser(int raidID, ulong userID, DateTime alarm, int hours, int minutes)
        {
            Logger.Log(LOG_LEVEL.INFO, "Notifying user #" + userID + " in " + (int)(alarm - DateTime.UtcNow).TotalSeconds + " seconds.");
            
            //Try to find a recommended role
            string role = null;
            try
            {
                //Generate comp
                Raider[] comp = this.GenerateComp(raidID, 0, out var unused);
                
                //Iterate over comp
                for (int i = 0; i < 10; i++)
                {
                    //Check if it's the right user
                    if ((comp[i]?.ID ?? 0) == userID)
                    {
                        //Save the role
                        role = (i < 2) ? "MES" : ((i < 4) ? "HEAL" : ((i < 8) ? "DPS" : "SLAVE"));
                    }
                }
            }
            catch { }

            //Format hours and minutes
            var time = Utility.FormatTime(hours, minutes);

            //Wait for right time
            var wait = alarm - DateTime.UtcNow;
            if (wait > TimeSpan.Zero) await Task.Delay(wait);

            //Try to send the message
            try
            {
                Logger.Log(LOG_LEVEL.INFO, "Notifying user #" + userID + ".");

                //Setup notification text
                var text = (string.IsNullOrWhiteSpace(time) ?
                            "The raid has started." :
                            "The raid starts in " + time + ".") +
                            ((!string.IsNullOrWhiteSpace(role)) ?
                            "\nYour suggested role is **" + role + "**." : "");

                //Queue message
                this.QueueImportantMessage(userID, text);
            }
            catch { }
        }

        private string CmdRaidCreate(SocketUserMessage msg, int day, int month, int hours, int minutes, char sign, int utc, string desc)
        {
            //Apply sign to timezone
            int timezone = ((sign == '+') ? 1 : -1) * utc;

            //Find out if the event is this year or next year
            var now  = DateTime.UtcNow;
            int year = now.Year;
            if ((month < now.Month) || (month == now.Month && day < now.Day)) year++;

            //Create raid event
            int id = this.raidCalendar.CreateRaid(day, month, year, hours, minutes, timezone, desc, msg.Author.Id);

            //Save raid calendar
            RaidCalendar.SaveToFile(this.raidCalendar, RAID_CALENDAR_FILE);

            //Return success
            return "A raid has been created with ID " + id + " for the "  + day + Utility.GetOrdinal(day) +
                    " of " + Utility.GetMonth(month) + " " + year + " at " +
                    Utility.PadNum(hours) + ":" + Utility.PadNum(minutes)  +
                    " " + Utility.RenderTimezone(timezone) + "\n\"" + desc + "\"";
        }

        private string CmdRaidCreateTodayOrTomorrow(SocketUserMessage msg, string day, int hours, int minutes, char sign, int utc, string desc)
        {
            //Look up the correct day
            DateTime eventDate = DateTime.UtcNow;
            if (day == "tomorrow") eventDate += new TimeSpan(1, 0, 0, 0);

            //Pass on to the full implementation
            return this.CmdRaidCreate(msg, eventDate.Day, eventDate.Month, hours, minutes, sign, utc, desc);
        }

        private string CmdRaidCreateHelp(SocketUserMessage _)
        {
            return "The syntax for \"$raid create\" is \"$raid create [DD/MM HH:MM UTC±HH Description]\".\n" +
                   "For example: \"$raid create 01/01 20:00 UTC+1 Fun raids with Arnoud!\".\n" +
                   "You can also replace DD/MM with simply \"today\" or \"tomorrow\".\n" +
                   "For example: \"$raid create tomorrow 20:00 UTC+1 Arnoud stepping on mines!\"\n" +
                   "If you don't know what Arnoud's current timezone is, or what the current " +
                   "server time (UTC+0) is, you can use the \"$time\" command.";
        }

        private string CmdRaidDelete(SocketUserMessage msg, int raidID)
        {
            //Try to delete the raid event
            if (this.raidCalendar.DeleteRaid(raidID, msg.Author.Id))
            {
                //Save raid calendar
                RaidCalendar.SaveToFile(this.raidCalendar, RAID_CALENDAR_FILE);

                //Return with success
                return "Raid was deleted.";
            }

            //Return with failure
            return "A raid with that ID either does not exist, or you are not the owner.";
        }

        private string CmdRaidDeleteHelp(SocketUserMessage _)
        {
            return "To delete a raid you must provide the ID given to you when it was created.\n" +
                   "If you do not remember it, use \"$raid list\" to find it.\n" +
                   "You can then delete the raid like so: \"$raid delete [ID]\"\n" +
                   "For example \"$raid delete 123\".";
        }

        private string CmdRaidRosterFilter(SocketUserMessage msg, int raidID, string filter)
        {
            //Get the roles
            var roles = this.GetRoles(filter);

            //Check that we got at least one
            if (roles != null)
            {
                //Try to get roster
                if (this.raidCalendar.ListRaiders(raidID, roles, out string roster, out int rosterCount))
                {
                    //Check if it's empty
                    if (rosterCount == 0)
                    {
                        //Return empty
                        return "No raiders matched your query.";
                    }

                    //Return roster
                    return "These are the people that matched your query:\n" + roster + "Count: " + rosterCount;
                }

                //Return error
                return "No raid with ID \"" + raidID + "\" found.";
            }

            //Use the normal roster function
            return this.CmdRaidRoster(msg, raidID);
        }

        private string CmdRaidRoster(SocketUserMessage _, int raidID)
        {
            //Try to get roster
            if (this.raidCalendar.ListRaiders(raidID, out string roster, out int rosterCount))
            {
                //Check if it's empty
                if (rosterCount == 0)
                {
                    //Return empty
                    return "The roster is empty.";
                }

                //Return roster
                return "These are the people signed up:\n" + roster + "Count: " + rosterCount;
            }

            //Return error
            return "No raid with ID \"" + raidID + "\" found.";
        }

        private string CmdRaidRosterHelp(SocketUserMessage _)
        {
            return "To display the list of people signed up for a raid you must provide the ID for the raid.\n" +
                   "If you do not know the ID, type \"$raid list\" to find it.\n" +
                   "You can then display the roster like so: \"$raid roster [ID]\"\n" +
                   "For example: \"$raid roster 123\".\n" +
                   "You can also filter it based on roles by writing one or more of MES, HEAL, DPS, SLAVE or KITER at the end.\n" +
                   "For example: \"$raid roster 123 MES HEAL\".";
        }

        private string CmdRaidList(SocketUserMessage _)
        {
            //Generate the list of events
            string events = this.raidCalendar.ListRaidEvents();

            //Check that it's not empty
            if (!string.IsNullOrWhiteSpace(events))
            {
                //Return the list
                return "These are the raids being organised right now:\n" + events;
            }
                
            //Return none
            return "There are no planned raids right now.";
        }

        private List<string> GetRoles(string roleList)
        {
            //Create our regex
            var regex = this.raidConfig
                            .GetRoles         ()
                            .OrderByDescending((s) => s.Length)
                            .Aggregate        ((s, s2) => s + "|" + s2);

            //Check for matches
            return Regex.Matches (roleList, regex, RegexOptions.IgnoreCase)
                        .Select  ((r) => r.Value.ToUpper())
                        .Distinct()
                        .ToList  ();
        }

        private string CmdRaidJoin(SocketUserMessage msg, int raidID, string roleList)
        {
            //Get the roles
            var roles = this.GetRoles(roleList);
            bool bu = false;

            //Check if one of the roles is BACKUP
            if (roleList.ToUpper().Contains("BACKUP"))
            {
                //Set flag
                bu = true;
            }

            //Check that we got at least one
            if ((roles?.Count ?? 0) > 0)
            {
                //Get the username/nickname
                string name = (msg.Author as SocketGuildUser)?.Nickname ?? msg.Author.Username;

                //Try to add to the raid
                if (this.raidCalendar.AddRaider(raidID, msg.Author.Id, name, roles, bu))
                {
                    //Save raid calendar
                    RaidCalendar.SaveToFile(this.raidCalendar, RAID_CALENDAR_FILE);

                    //Return success
                    return $"You were added to the raid{(bu ? " as backup" : "")} with these roles: \"{string.Join(", ", roles)}\".";
                }
                else
                {
                    //Return error
                    return "No raid with ID \"" + raidID + "\" found. Type \"$raid list\" if you need to find the ID.";
                }
            }
            else
            {
                //Return error
                return "No roles provided. Type \"$raid join\" if you need help with this command.";
            }
        }

        private string CmdRaidJoinSimple(SocketUserMessage msg, string roleList)
        {
            //Check if there is at least one raid being organised right now
            if (this.raidCalendar.GetNumberOfRaidEvents() > 0)
            {
                //Get the ID for the first event
                int raidID = this.raidCalendar.GetFirstEvent().ID;

                //Pass on to the full implementation
                return this.CmdRaidJoin(msg, raidID, roleList);
            }

            //Return error
            return "There are no raids being organised right now.";
        }

        private string CmdRaidJoinHelp(SocketUserMessage _)
        {
            return "To join a raid you must provide the ID for the raid and your available roles.\n" +
                   "The roles are DPS, SLAVE, HEAL, MES and KITER. You can provide them in any order (for example " +
                   "in order of preference) separated by spaces, commas or any other symbol.\n" +
                   "For example: \"$raid join 123 HEAL DPS MES\". It is not case-sensitive.\n" +
                   "If you wish to add or remove a role, simply type the command again with a new list.\n" +
                   "If you do not know the ID for the raid, type \"$raid list\" to find it.\n" +
                   "**You can omit the id to simply join the first raid.**";
        }

        private string CmdRaidLeave(SocketUserMessage msg, int raidID)
        {
            //Try to remove from the raid
            if (this.raidCalendar.RemoveRaider(raidID, msg.Author.Id))
            {
                //Save raid calendar
                RaidCalendar.SaveToFile(this.raidCalendar, RAID_CALENDAR_FILE);

                //Return success
                return "You were removed from the roster.\n";
            }

            //Return failure
            return "Couldn't find you in a raid with that ID (" + raidID + ").";
        }

        private string CmdRaidLeaveHelp(SocketUserMessage _)
        {
            return "To leave a raid you must provide the ID for the raid you wish to leave.\n" +
                   "If you do not remember the ID, type \"$raid list\" to find it.\n" +
                   "You can then type \"$raid leave [ID]\"\n" + 
                   "For example: \"$raid leave 123\"";
        }

        private string CmdRaidNotify(SocketUserMessage msg, string notification)
        {
            //Get the hours and minutes
            var times = Utility.GetClockTimes(notification);
                
            //Check that it's not null
            if (times != null)
            {
                //Prepare result string
                string ret    = "You will be notified";
                string indent = " ";
                if (times.Count > 1)
                {
                    ret   += "\n";
                    indent = "\t";
                }

                //Get each clock time
                int n = 0;
                var reminders = new Reminder[times.Count];
                foreach (Tuple<int, int> clock in times)
                {
                    //Get hours and minutes
                    int hours   = clock.Item1;
                    int minutes = clock.Item2;

                    //Insert reminder
                    reminders[n++] = new Reminder(msg.Author.Id, hours, minutes);

                    //Check if hours and minutes are zero
                    if (hours == 0 && minutes == 0)
                    {
                        //Insert simplified message
                        ret += indent + "when the raid starts.\n";
                    }
                    else
                    {
                        //Format hours and minutes
                        var time = indent + Utility.FormatTime(hours, minutes);

                        //Insert message
                        ret += time + " before the raid.\n";
                    }
                }

                //Add reminders to calendar
                this.raidCalendar.AddReminders(reminders);

                //Save raid calendar
                RaidCalendar.SaveToFile(this.raidCalendar, RAID_CALENDAR_FILE);

                //Return resulting message
                return ret;
            }

            //Return error
            return "The times must be given in a HH:MM format.\n" +
                   "Type \"$raid notify\" for more info.";
        }

        private string CmdRaidNotifyRemove(SocketUserMessage msg)
        {
            //Remove notification
            this.raidCalendar.RemoveReminders(msg.Author.Id);

            //Save raid calendar
            RaidCalendar.SaveToFile(this.raidCalendar, RAID_CALENDAR_FILE);

            //Reply with success
            return "You will no longer be notified before a raid.";
        }

        private string CmdRaidNotifyHelp(SocketUserMessage _)
        {
            return "If you wish to be notified before a raid you've signed up for, " +
                   "provide the hours and minutes before the raid you wish to be notified " +
                   "separated by a colon.\n" +
                   "For example: \"$raid notify 01:00\".\n" +
                   "You can use the command again to change it.\n" +
                   "If you no longer wish to be notified, simply type \"remove\" " +
                   "instead of hours and minutes.\n" +
                   "Like so: \"$raid notify remove\"";
        }

        private Raider[] GenerateComp(int raidID, int compIdx, out List<Raider> unused)
        {
            //Try to get the raiders
            var raiders = this.raidCalendar.GetRaiders(raidID);
            var tmp = new List<Raider>();

            //Check that there is at least one
            if ((raiders?.Count ?? 0) > 0)
            {
                //Pass a copy of the raider list to our solver
                var comp = this.MakeRaidComp(raiders, compIdx);

                //Check if anyone is not included
                foreach (Raider r in raiders)
                {
                    if (r != null && !Array.Exists(comp, e => r.ID == e?.ID)) tmp.Add(r);
                }

                //Return the composition + unused list
                unused = tmp;
                return comp;
            }

            //Return failure
            unused = tmp;
            return null;
        }

        private string CmdRaidMakeComp(SocketUserMessage _, string name, int raidID)
        {
            //Find the comp
            int compIdx = this.raidConfig.GetCompIndex(name.ToUpper());

            //Check that it exists
            if (compIdx != -1)
            {
                //Generate composition
                var bestComp = this.GenerateComp(raidID, compIdx, out var unused);

                //Check that it's not null
                if (bestComp != null)
                {
                    //Generate the textual representation of the comp
                    var offset = 0;
                    var text   = this.raidConfig
                                     .GetRoleCounts(name.ToUpper())
                                     .Where ((val) => val.Value > 0)
                                     .Select((val) =>
                                     {
                                         //Prepare the string for this role
                                         var output = $"{val.Key}:\n    ";

                                         //Iterate over the area we we care about for this role
                                         var tmp = new List<string>();
                                         for (int i = 0; i < val.Value; i++)
                                         {
                                             //Check that this slot is not empty
                                             if (bestComp[offset + i] != null)
                                             {
                                                 //Add raider
                                                 tmp.Add(bestComp[offset + i].nick);
                                             }
                                             else tmp.Add("<empty>");
                                         }

                                         //Update offset
                                         offset += val.Value;

                                         //Push the names into the string
                                         return output + string.Join(", ", tmp);
                                     })
                                     .Aggregate((s1, s2) => s1 + "\n" + s2);

                    //Return the comp
                    return "This is the best comp I could make:\n" + text +
                          (unused.Count > 0 ? "\n\nNot included:\n" + string.Join('\n', unused.Select(e => e.nick)) : string.Empty);
                }

                //Return failure
                return "Cannot create a comp for raid with that ID (" + raidID + ").\n" +
                       "There either are no raiders for it or there was an error.";
            }

            //Return failure
            return "No comp with that name exists. These are the recognised comps: \n" +
                   string.Join(", ", this.raidConfig.GetCompNames());
        }

        private string CmdRaidMakeCompSimple(SocketUserMessage msg, string name)
        {
            //Check if there is at least one raid being organised right now
            if (this.raidCalendar.GetNumberOfRaidEvents() > 0)
            {
                //Get the ID for the first event
                int raidID = this.raidCalendar.GetFirstEvent().ID;

                //Pass on to the full implementation
                return this.CmdRaidMakeComp(msg, name, raidID);
            }

            //Return error
            return "There are no raids being organised right now.";
        }

        private string CmdRaidMakeCompSimplest(SocketUserMessage msg)
        {
            //Check if there is at least one raid being organised right now
            if (this.raidCalendar.GetNumberOfRaidEvents() > 0)
            {
                //Get the ID for the first event
                int raidID = this.raidCalendar.GetFirstEvent().ID;

                //Pass on to the full implementation
                return this.CmdRaidMakeComp(msg, "DEFAULT", raidID);
            }

            //Return error
            return "There are no raids being organised right now.";
        }

        private string CmdRaidMakeCompHelp(SocketUserMessage _)
        {
            return "To auto-generate a raid composition you need to provide the ID of the raid.\n" +
                   "If you do not know the ID, type \"$raid list\" to find it.\n" +
                   "You can then type \"$raid make comp [ID]\"\n" +
                   "For example: \"$raid make comp 123\"\n" +
                   "**You can omit the id to simply select the first raid.**";
        }

        private string CmdRaidCreateComp(SocketUserMessage msg, string name, string comp)
        {
            //Get all the roles (including duplicates)
            var roles = Regex.Matches(comp, @"\w+")
                             .Select ((r) => r.Value.ToUpper())
                             .ToList ();

            //Check that at least one was provided
            if (roles.Count > 0)
            {
                //Add the comp description
                this.raidConfig.AddCompDescription(new CompDescription
                {
                    Name   = name.ToUpper(),
                    Layout = roles
                });

                //Save and compile
                bool success = Debug.Try(() =>
                {
                    this.raidConfig.SaveConfig();
                    this.raidConfig.GenerateSolverLibrary();
                });

                //Return result
                return success ? "Comp was created." : "There was an error #blamearnoud";
            }

            //Return error
            return "You need to provide the roles in the composition!";
        }

        private string CmdRaidCreateCompHelp(SocketUserMessage _)
        {
            return "You did something wrong. Uh ask Grim for help, atm I'm too lazy to have a good help message.";
        }

        private string CmdRaidHelp(SocketUserMessage _)
        {
            return Bot.CmdRaidHelp();
        }

        private static string CmdRaidHelp()
        {
            return "These are the raid commands available:\n" +
                   "    $raid create [DD/MM HH:MM UTC±HH Description]\n" +
                   "    $raid delete [ID]\n" +
                   "    $raid roster [ID]\n" +
                   "    $raid list\n" +
                   "    $raid join [ID Roles]\n" +
                   "    $raid leave [ID]\n" +
                   "    $raid notify [HH:MM]\n" +
                   "    $raid make comp [ID]\n" +
                   "    $raid help\n" +
                   "\n" +
                   "To setup a raid, use the \"$raid create\" command. If you need to remove it again, " +
                   "use the \"$raid delete\" command with the ID you were given (Only the creator can do this). " +
                   "To see who has signed up for the raid, you can use the \"$raid roster\" command with the raid ID.\n" +
                   "If you're looking for a raid to join, then use the \"$raid list\" command to display " +
                   "available raids. You can then join with \"$raid join\" using the ID provided by the " +
                   "previous command. The \"Roles\" are DPS, SLAVE, HEAL and MES. Feel free to list them in " +
                   "order of preference, separated by whatever symbol you want (or just spaces). " +
                   "If you cannot join afterall, use the \"$raid leave\" command with the raid's ID.\n" +
                   "If you want to be notified before the raid, you can use the \"$raid notify\" command " +
                   "followed by the hours and minutes (in HH:MM format) before the raid you wish to be notified.\n" +
                   "To get a group composition auto-generated by the bot, you can use the \"$raid make comp\" " +
                   "command with the ID for the raid.\n" +
                   "To see this message again, type \"$raid help\" or just \"$raid\".\n" +
                   "Pro-tip: You can also whisper me (Left click me and type your command).";
        }

        private RaidConfig          raidConfig;
        private RaidCalendar        raidCalendar;
        private List<QueuedMessage> importantMessages;
        private object              importantMessageLock = new object();
    }
}