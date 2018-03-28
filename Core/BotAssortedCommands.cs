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
                        "bf", this, "CmdBF", "CmdBFHelp",
                        "brainfuck code", "(.+)"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "ip", this, "CmdIP", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "mem", this, "CmdMem", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "mem", this, "CmdMem", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "free", this, "CmdFree", null
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "tunnel", this, "CmdSSHTunnelOpen", "CmdSSHTunnelOpenHelp",
                        "open",      @"open(?:$|\s)",
                        "source IP", @"(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "tunnel", this, "CmdSSHTunnelClose", null,
                        "close", @"close(?:$|\s)"
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

            //Initialise sshTunnel to null
            this.sshTunnel = null;
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

        private string CmdBF(SocketUserMessage _, string bfCode)
        {
            //Catch errors
            try
            {
                //Tokenize
                var tok = BF.Brainfuck.TokenizeBF(bfCode);

                //Generate opcodes
                var ops = BF.Brainfuck.Parse(tok);

                //Convert to x86 assembly
                var asm = BF.Brainfuck.GenerateASM(ops);

                //Return text
                return "```\n" + asm + "```";
            }
            catch { }

            //Return error
            return "Syntax error!";
        }

        private string CmdBFHelp(SocketUserMessage _)
        {
            return "The syntax for this command is \"$bf [brainfuck code]\".\n" +
                   "For example: \"$bf ++++[.-]\".";
        }

        private string CmdIP(SocketUserMessage msg)
        {
            //Check if user is me
            if (msg.Author.Id == 206062064140025856)
            {
                //Check if linux
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    //Run the "hostname -I" command
                    var proc = System.Diagnostics.Process.Start
                    (
                        new System.Diagnostics.ProcessStartInfo
                        {
                            FileName               = $"hostname",
                            Arguments              = $"-I",
                            UseShellExecute        = false,
                            RedirectStandardOutput = true,
                            RedirectStandardError  = true
                        }
                    );

                    //Capture output asynchronously
                    var task = Task.Run(async () =>
                    {
                        using (MemoryStream ms = new MemoryStream())
                        {
                            await proc.StandardOutput.BaseStream.CopyToAsync(ms);
                            return Encoding.UTF8.GetString(ms.ToArray());
                        }
                    });

                    //Grab the output data
                    return task.GetAwaiter().GetResult();
                }
                else
                {
                    //Concatenate and return output
                    return string.Join
                    (
                        "\n",
                        //Grab host addresses
                        Dns.GetHostAddresses
                        (
                            Dns.GetHostName()
                        ).Select(ip => ip.ToString())
                    );
                }
            }

            //Return failure
            return "No permission.";
        }

        private string CmdMem(SocketUserMessage msg)
        {
            //Check if user is me
            if (msg.Author.Id == 206062064140025856)
            {
                //Get the process
                var proc = System.Diagnostics.Process.GetCurrentProcess();

                //Make sure we got recent values
                proc.Refresh();

                //Return memory used
                return $"Memory used: {((proc.PrivateMemorySize64 != 0) ? proc.PrivateMemorySize64 : GC.GetTotalMemory(false))} bytes.";
            }

            //Return failure
            return "No permission.";
        }

        private string CmdFree(SocketUserMessage msg)
        {
            //Check if user is me
            if (msg.Author.Id == 206062064140025856)
            {
                //Read the current memory used
                var mem = GC.GetTotalMemory(false);

                //Force the GC to perform garbage collection
                GC.Collect();
                GC.WaitForPendingFinalizers();

                //Find out how much was released
                var diff = mem - GC.GetTotalMemory(true);

                //Check if it's positive
                if (diff > 0)
                {
                    return $"{diff} bytes were successfully freed.";
                }
                else
                {
                    return "No memory was freed.";
                }
            }

            //Return failure
            return "No permission.";
        }

        private string CmdSSHTunnelOpen(SocketUserMessage msg, string ip)
        {
            //Check if user is me
            if (msg.Author.Id == 206062064140025856)
            {
                //Check if linux
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    //Setup ssh tunnel
                    this.sshTunnel = System.Diagnostics.Process.Start
                    (
                        new System.Diagnostics.ProcessStartInfo
                        {
                            FileName               = $"ssh",
                            Arguments              = $"-i /home/pi/.ssh/id_rsa -o \"StrictHostKeyChecking no\" -nNT -R {19999}:localhost:22 tehre@{ip}",
                            UseShellExecute        = false
                        }
                    );

                    //Return success
                    return $"SSH tunnel on port 19999 created.\nUse `ssh pi@localhost -p 19999` to connect.";
                }
                else
                {
                    //Return failure
                    return "Only available on Linux.";
                }
            }

            //Return failure
            return "No permission.";
        }

        private string CmdSSHTunnelOpenHelp(SocketUserMessage msg)
        {
            //Check if user is me
            if (msg.Author.Id == 206062064140025856)
            {
                //Return help message
                return "The syntax for this command is \"$tunnel open [source IP]\".\n" +
                       "For example: \"$tunnel open 192.168.0.1\".";
            }

            //Return failure
            return "No permission.";
        }

        private string CmdSSHTunnelClose(SocketUserMessage msg)
        {
            //Check if user is me
            if (msg.Author.Id == 206062064140025856)
            {
                //Catch any errors
                try
                {
                    //Check that the process is not null
                    if (this.sshTunnel != null && !this.sshTunnel.HasExited)
                    {
                        //Close tunnel
                        this.sshTunnel.Close();

                        //Wait for it to close gracefully
                        if (!this.sshTunnel.WaitForExit(500))
                        {
                            //Kill it
                            this.sshTunnel.Kill();
                        }
                    }
                }
                catch (Exception ex)
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);
                }

                //Set to null
                this.sshTunnel = null;

                //Return success
                return "Tunnel closed.";
            }

            //Return failure
            return "No permission.";
        }

        private string CmdHelp(SocketUserMessage _)
        {
            //One day this will be generated from the CommandInit functions
            return "\n" +
                 /*"__Lua commands__:\n" +
                   "    $lua [lua command]\n" +
                   "    $reload lua\n" +*/
                   "__Query commands__:\n" +
                   "    $query [Search terms]\n" +
                   "    $wolfram [Search terms]\n" +
                   "    $wiki [Title]\n" +
                   "    $ub [Word]\n" +
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
                   "    $bf [brainfuck code]\n" +
                   "    $help\n" +
                   "\n" +
                   "Pro-tip: You can also DM me (Left click me and type your command).";
        }

        private System.Diagnostics.Process sshTunnel;
    }
}
