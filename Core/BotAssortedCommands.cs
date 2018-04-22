using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Net;

using Discord.WebSocket;

using DiscordBot.Utils;

namespace DiscordBot.Core
{
    public partial class Bot
    {
        [CommandInit]
        private void ConstructAssortedCommands()
        {
            //Add various assorted commands
            this.commandCategories.Add
            (
                new CommandCategory(null, null, null)
                .RegisterCommand
                (
                    new Command
                    (
                        "time", this, "CmdTime", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "help", this, "CmdHelp", null
                    )
                )
            );
        }

        private string CmdTime(SocketUserMessage _)
        {
            //Get UTC+0
            var utcNow = DateTimeOffset.UtcNow;

            //FIXME: Check for DST
            bool dst = true;//DateTime.Now.IsDaylightSavingTime();

            //Reliably get the actual time
            var now = utcNow.AddHours(dst ? 2 : 1);

            //Reply with time info
            return "Current time in UTC+0 is " + Utility.PadNum(utcNow.Hour) + ":" + Utility.PadNum(utcNow.Minute) + ".\n" +
                   "Arnoud is " + (dst ? "currently" : "NOT" ) + " experiencing Daylight Saving Time.\n" +
                   "His time is now " + Utility.PadNum(now.Hour) + ":" + Utility.PadNum(now.Minute) +
                   " (UTC+" + (dst ? 2 : 1) + ").";
        }

        private string CmdHelp(SocketUserMessage _)
        {
            //One day this will be generated from the CommandInit functions
            return "\n" +
                   "__Raid commands__:\n" +
                   "    $raid create [DD/MM HH:MM UTC±HH Description]\n" +
                   "    $raid delete [ID]\n" +
                   "    $raid roster [ID]\n" +
                   "    $raid list\n" +
                   "    $raid join [ID Roles]\n" +
                   "    $raid leave [ID]\n" +
                   "    $raid notify [HH:MM]\n" +
                   "    $raid make comp [ID]\n" +
                   "    $raid help\n" +
                   "__Music commands__:\n" +
                   "    $music join\n" +
                   "    $music leave\n" +
                   "    $music play [youtube-link or name]\n" +
                   "    $music playlist [youtube-link or name]\n" +
                   "    $music volume [Value]\n" +
                   "    $music skip\n" +
                   "    $music np\n" +
                   "__Other__:\n" +
                   "    $time\n" +
                   "    $help\n" +
                   "\n" +
                   "Pro-tip: You can also DM me (Left click me and type your command).";
        }


    }
}
