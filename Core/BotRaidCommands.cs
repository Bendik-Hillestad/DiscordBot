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
                        "add", this, "CmdRaidAdd", "CmdRaidAddHelp",
                        "ID",    @"(\d+)(?:$|\s)",
                        "name",  @"(.+?)\s*\|",
                        "roles", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "add", this, "CmdRaidAddSimple", "CmdRaidAddHelp",
                        "name",  @"(\D.+?)\s*\|",
                        "roles", @"(.+?)$"
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
                        "kick", this, "CmdRaidKick", "CmdRaidKickHelp",
                        "ID",   @"(\d+)(?:$|\s)",
                        "name", @"(.+?)$"
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

            //Initialise raid manager
            RaidManager.Initialise();

            //Load raid config
            this.raidConfig = RaidConfig.ReadConfig();

            //Compile
            this.raidConfig.GenerateSolverLibrary();
        }

        private string CmdRaidCreate(SocketUserMessage msg, int day, int month, int hours, int minutes, char sign, int utc, string desc)
        {
            //Apply sign to timezone
            int timezone = ((sign == '+') ? 1 : -1) * utc;

            //Find out if the event is this year or next year
            var now  = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(timezone, 0, 0));
            int year = now.Year;
            if ((month < now.Month) || (month == now.Month && day < now.Day)) year++;

            //Calculate the timestamp
            var offset = new DateTimeOffset(year, month, day, hours, minutes, 0, new TimeSpan(timezone, 0, 0));

            //Pass on to implementation
            return CmdRaidCreate_Implementation(msg, offset, desc);
        }

        private string CmdRaidCreateTodayOrTomorrow(SocketUserMessage msg, string day, int hours, int minutes, char sign, int utc, string desc)
        {
            //Apply sign to timezone
            int timezone = ((sign == '+') ? 1 : -1) * utc;

            //Look up the correct day
            var eventDate = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(timezone, 0, 0));
            if (day == "tomorrow") eventDate += new TimeSpan(1, 0, 0, 0);

            //Calculate the timestamp
            var offset = new DateTimeOffset(eventDate.Year, eventDate.Month, eventDate.Day, hours, minutes, 0, new TimeSpan(timezone, 0, 0));

            //Pass on to implementation
            return CmdRaidCreate_Implementation(msg, offset, desc);
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
            //Find the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Pass on to implementation
            return CmdRaidDelete_Implementation(msg, handle.Value);
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
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Get the roles
            var roles = this.GetRoles(filter);

            //Pass on to implementation
            return CmdRaidRoster_Implementation(msg, handle.Value, roles);
        }

        private string CmdRaidRoster(SocketUserMessage msg, int raidID)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Get all roles
            var roles = this.raidConfig.GetAllRoles();

            //Pass on to implementation
            return CmdRaidRoster_Implementation(msg, handle.Value, roles);
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

        private string CmdRaidList(SocketUserMessage msg)
        {
            //Just pass on to implementation (in the future we might filter based on context)
            return CmdRaidList_Implementation(msg);
        }

        private string CmdRaidJoin(SocketUserMessage msg, int raidID, string roleList)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Get the roles
            var roles = this.GetRoles(roleList);
            bool bu = false;

            //Check if one of the roles is BACKUP
            if (roleList.ToUpper().Contains("BACKUP"))
            {
                //Set flag
                bu = true;
            }

            //Pass on to implementation
            return CmdRaidJoin_Implementation(msg, handle.Value, bu, roles);
        }

        private string CmdRaidJoinSimple(SocketUserMessage msg, string roleList)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if valid
            if (!handle.HasValue) return "No raids to join.";

            //Get the roles
            var roles = this.GetRoles(roleList);
            bool bu = false;

            //Check if one of the roles is BACKUP
            if (roleList.ToUpper().Contains("BACKUP"))
            {
                //Set flag
                bu = true;
            }

            //Pass on to implementation
            return CmdRaidJoin_Implementation(msg, handle.Value, bu, roles);
        }

        private string CmdRaidJoinHelp(SocketUserMessage _)
        {
            return $"To join a raid you must provide the ID for the raid and your available roles.\n" +
                   $"The roles are {string.Join(", ", this.raidConfig.GetAllRoles())}. " +
                   $"You can provide them in any order (for example in order of preference) " +
                   $"separated by spaces, commas or any other symbol.\n" +
                   $"For example: \"$raid join 123 DPS\". It is not case-sensitive.\n" +
                   $"If you wish to add or remove a role, simply type the command again with a new list.\n" +
                   $"If you do not know the ID for the raid, type \"$raid list\" to find it.\n" +
                   $"**You can omit the id to simply join the first raid.**";
        }

        private string CmdRaidAdd(SocketUserMessage msg, int raidID, string name, string roleList)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Get the roles
            var roles = this.GetRoles(roleList);
            bool bu = false;

            //Check if one of the roles is BACKUP
            if (roleList.ToUpper().Contains("BACKUP"))
            {
                //Set flag
                bu = true;
            }

            //Pass on to implementation
            return CmdRaidAdd_Implementation(msg, handle.Value, name, bu, roles);
        }

        private string CmdRaidAddSimple(SocketUserMessage msg, string name, string roleList)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if valid
            if (!handle.HasValue) return "No raids to join.";

            //Get the roles
            var roles = this.GetRoles(roleList);
            bool bu = false;

            //Check if one of the roles is BACKUP
            if (roleList.ToUpper().Contains("BACKUP"))
            {
                //Set flag
                bu = true;
            }

            //Pass on to implementation
            return CmdRaidAdd_Implementation(msg, handle.Value, name, bu, roles);
        }

        private string CmdRaidAddHelp(SocketUserMessage _)
        {
            return $"raid add [ID] [Name] | [Roles]\n" +
                   $"\t ID - (Optional) The ID of the raid you wish to join\n" +
                   $"\t Name - The name of the person to add\n" +
                   $"\t Roles - The roles the person can take";
        }

        private string CmdRaidLeave(SocketUserMessage msg, int raidID)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Pass on to implementation
            return CmdRaidLeave_Implementation(msg, handle.Value);
        }

        private string CmdRaidLeaveHelp(SocketUserMessage _)
        {
            return "To leave a raid you must provide the ID for the raid you wish to leave.\n" +
                   "If you do not remember the ID, type \"$raid list\" to find it.\n" +
                   "You can then type \"$raid leave [ID]\"\n" + 
                   "For example: \"$raid leave 123\"";
        }

        private string CmdRaidKick(SocketUserMessage msg, int raidID, string name)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Pass on to implementation
            return CmdRaidKick_Implementation(msg, handle.Value, name);
        }

        private string CmdRaidKickHelp(SocketUserMessage _)
        {
            return "To kick someone from the raid you must provide the ID for the raid and their name.\n" +
                   "If you do not remember the ID, type \"$raid list\" to find it.\n" +
                   "You can then type \"$raid kick [ID] [name]\"\n" +
                   "For example: \"$raid kick 123 SomeGuy\"";
        }

        private string CmdRaidMakeComp(SocketUserMessage msg, string name, int raidID)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(raidID);

            //Check if valid
            if (!handle.HasValue) return "No raid with that ID.";

            //Find the comp
            int compIdx = this.raidConfig.GetCompIndex(name.ToUpper());

            //Check if valid
            if (compIdx == -1) return "No comp with that name. These are the recognised comps: \n" +
                                       string.Join(", ", this.raidConfig.GetCompNames());

            //Pass on to implementation
            return CmdRaidMakeComp_Implementation(msg, handle.Value, compIdx);
        }

        private string CmdRaidMakeCompSimple(SocketUserMessage msg, string name)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if valid
            if (!handle.HasValue) return "No raids being organized.";

            //Find the comp
            int compIdx = this.raidConfig.GetCompIndex(name.ToUpper());

            //Check if valid
            if (compIdx == -1) return "No comp with that name. These are the recognised comps: \n" +
                                       string.Join(", ", this.raidConfig.GetCompNames());

            //Pass on to implementation
            return CmdRaidMakeComp_Implementation(msg, handle.Value, compIdx);
        }

        private string CmdRaidMakeCompSimplest(SocketUserMessage msg)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if valid
            if (!handle.HasValue) return "No raids being organized.";

            //Pass on to implementation
            return CmdRaidMakeComp_Implementation(msg, handle.Value, 0);
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
                             .Select (r => r.Value.ToUpper())
                             .ToList ();

            //Pass on to implementation
            return CmdRaidCreateComp_Implementation(msg, name, roles);
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
                   "    $raid add [ID Name | Roles]\n" +
                   "    $raid leave [ID]\n" +
                   "    $raid kick [ID Name]\n" +
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

        private RaidConfig raidConfig;
    }
}