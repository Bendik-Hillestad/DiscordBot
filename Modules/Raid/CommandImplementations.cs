using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using DiscordBot.Commands;
using DiscordBot.Core;
using DiscordBot.Modules.Raid.EVTC;
using DiscordBot.Utils;
using DiscordBot.Raids;

using static DiscordBot.Modules.Raid.GW2Raidar.Utility;

namespace DiscordBot.Modules.Raid
{
    public partial class RaidModule : CommandModule<RaidModule>
    {
        private void raid_create_impl(Context ctx, int day, int month, int year, int hours, int minutes, int offset, string description)
        {
            //Get the date
            var date = new DateTimeOffset(year, month, day, hours, minutes, 0, new TimeSpan(offset, 0, 0));

            //Create the raid
            var handle = RaidManager.CreateRaid(ctx.message.Author.Id, date, description).Value;

            //Return success
            Bot.GetBotInstance()
               .SendSuccessMessage(ctx.message.Channel, 
                   "Success", 
                   $"Raid has been created.",
                   $"ID: {handle.raid_id} | Local time:", date
               );
        }

        private void raid_delete_impl(Context ctx, int id)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(id).Value;

            //Grab the data from the raid
            var data = RaidManager.GetRaidData(handle);

            //Check that it's valid
            Precondition.Assert(data.HasValue, "There was an error processing the raid.");

            //Get the owner
            var owner_id = data.Value.owner_id;

            //Check that the user is the owner
            Precondition.Assert(ctx.message.Author.Id == owner_id, "You are not the owner of the raid!");

            //Delete the raid
            RaidManager.DeleteRaid(handle);

            //Return success
            Bot.GetBotInstance()
               .SendSuccessMessage(ctx.message.Channel, "Success", $"Raid has been deleted.");
        }

        private void raid_roster_impl(Context ctx, int id, string roles)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(id).Value;

            //Extract the roles to create our filter
            var filter = this.raidConfig.MatchRoles(roles);

            //Get the raiders that match the filter
            var roster = RaidManager.CoalesceRaiders(handle)
                                    .Where (e => e.roles.Intersect(filter).Count() > 0)
                                    .ToList();

            //Check that it's not empty
            if (roster.Count > 0)
            {
                //Resolve their names and roles
                var tmp = roster.Select(e =>
                {
                    //Get the name
                    var name = (e.user_id.HasValue) ? Bot.GetBotInstance().GetUserName(e.user_id.Value)
                                                    : e.user_name;

                    //Check if this entry is a backup
                    if (e.backup)
                    {
                        //Add the roles and cursive style
                        return $"*{name} - {string.Join(", ", e.roles)}*";
                    }

                    //Add the roles
                    return $"{name} - {string.Join(", ", e.roles)}";
                }).ToList();

                //Display the roster
                Bot.GetBotInstance()
                   .SendSuccessMessage(ctx.message.Channel, "Result:", string.Join("\n", tmp), $"Count: {tmp.Count}");
            }
            else
            {
                //Empty roster
                Bot.GetBotInstance()
                   .SendSuccessMessage(ctx.message.Channel, "Result:", $"None");
            }
        }

        private void raid_aliases_add_impl(Context ctx, string key, string value)
        {
            //Check if the alias exists
            if (this.raidConfig.HasAlias(key))
            {
                //Update the alias
                this.raidConfig.UpdateAlias(key, value);

                //Return success
                Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                    "Success", $"Alias updated to '{key} => {value}'."
                );
            }
            else
            {
                //Add the alias
                this.raidConfig.AddAlias(key, value);

                //Return success
                Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                    "Success", $"Alias '{key} => {value}' created."
                );
            }
        }

        private void raid_aliases_remove_impl(Context ctx, string key)
        {
            //Remove the alias
            this.raidConfig.RemoveAlias(key);

            //Return success
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Success",
                $"Alias '{key}' removed."
            );
        }

        private void raid_aliases_impl(Context ctx)
        {
            //Get all the aliases we recognize
            var aliases = this.raidConfig.Aliases;

            //Display the aliases
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Aliases:", string.Join("\n", aliases.Select(kv => $"{kv.Key.ToUpper()} => {kv.Value.ToUpper()}"))
            );
        }

        private void raid_roles_impl(Context ctx)
        {
            //Get all the roles we recognize
            var roles = this.raidConfig.Roles;

            //Display the roles
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Roles:", string.Join(", ", roles)
            );
        }

        private void raid_list_impl(Context ctx)
        {
            //Generate the list of raids
            var raids = RaidManager.EnumerateRaids()
                                   .Select(r => RaidManager.GetRaidData(r))
                                   .Where (r => r.HasValue).Select(r => r.Value)
                                   .ToList();

            //Check that it's not empty
            if (raids.Count > 0)
            {
                //Go through each raid
                raids.ForEach(r =>
                {
                    //Display the raid
                    Bot.GetBotInstance()
                       .SendSuccessMessage(ctx.message.Channel,
                            r.description,
                            $"Roster size: {r.roster.Distinct().Count()}",
                            $"ID: {r.raid_id} | Local time:", DateTimeOffset.FromUnixTimeSeconds(r.timestamp)
                       );
                });
            }
            else
            {
                //No raids
                Bot.GetBotInstance()
                   .SendSuccessMessage(ctx.message.Channel, "Result:", $"None.");
            }
        }

        private void raid_join_impl(Context ctx, int id, string roles)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(id).Value;

            //Extract the roles
            var extractedRoles = this.raidConfig.MatchRoles(roles);
            bool bu = roles.ToUpper().Contains("BACKUP");

            //Check that we got any roles
            Precondition.Assert(extractedRoles.Length > 0, "No recognized roles provided!");

            //Add to the raid
            RaidManager.AppendRaider(handle, ctx.message.Author.Id, bu, extractedRoles);

            //Return success
            Bot.GetBotInstance()
               .SendSuccessMessage(ctx.message.Channel, 
                   "Success", 
                   $"You were added to the raid{(bu ? " as backup" : "")} with these roles: \"{string.Join(", ", extractedRoles)}\"."
               );
        }

        private void raid_leave_impl(Context ctx, RaidHandle handle, Entry e)
        {
            //Remove from the raid
            RaidManager.RemoveRaider(handle, e);

            //Return success
            Bot.GetBotInstance()
               .SendSuccessMessage(ctx.message.Channel,
                   "Success",
                   $"You were removed from the roster."
               );
        }

        private void raid_add_impl(Context ctx, int id, string name, string roles)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(id).Value;

            //Extract the roles
            var extractedRoles = this.raidConfig.MatchRoles(roles);
            bool bu = roles.ToUpper().Contains("BACKUP");

            //Check that we got any roles
            Precondition.Assert(extractedRoles.Length > 0, "No recognized roles provided!");

            //Add to the raid
            RaidManager.AppendRaider(handle, name, bu, extractedRoles);

            //Return success
            Bot.GetBotInstance()
               .SendSuccessMessage(ctx.message.Channel,
                   "Success",
                   $"They were added to the raid{(bu ? " as backup" : "")} with these roles: \"{string.Join(", ", extractedRoles)}\"."
               );
        }

        private void raid_kick_impl(Context ctx, RaidHandle handle, Entry e)
        {
            //Grab the data from the raid
            var data = RaidManager.GetRaidData(handle);

            //Check that it's valid
            Precondition.Assert(data.HasValue, "There was an error processing the raid.");

            //Get the owner
            var owner_id = data.Value.owner_id;

            //Check that the user is the owner
            Precondition.Assert(ctx.message.Author.Id == owner_id, "You are not the owner of the raid!");

            //Remove from the raid
            RaidManager.RemoveRaider(handle, e);

            //Return success
            Bot.GetBotInstance()
               .SendSuccessMessage(ctx.message.Channel,
                   "Success",
                   $"They were removed from the roster."
               );
        }

        private void raid_make_comp_impl(Context ctx, RaidHandle handle, /*readonly*/ string[] layout)
        {
            //Get the raiders
            var raiders = RaidManager.CoalesceRaiders(handle);

            //Generate composition
            var result = this.GenerateComp(raiders, layout, out Entry[] unused);

            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);

            //Get the unique roles in this composition
            var roles = layout.Distinct().ToArray();

            //Go through each role
            foreach (var r in roles)
            {
                //Count the instances of this role in the layout
                var roleCount = layout.Count(str => r == str);

                //Get all players with this assigned role
                var players = result.Where(p => p.assignment == r);

                //Format the names
                var formatted = players.Select(p =>
                {
                    //Get the name of the player
                    var name = (p.player.user_id.HasValue ? Bot.GetBotInstance().GetUserName(p.player.user_id.Value) : p.player.user_name);

                    //Check if backup
                    if (p.player.backup)
                    {
                        //Add cursive
                        return $"*{name}*";
                    }
                    else return name;
                }).ToArray();

                //Check that it's not empty
                if (formatted.Length > 0)
                {
                    //Write the formatted names
                    builder = builder.AddField($"{r} ({formatted.Length}/{roleCount}):", string.Join('\n', formatted));
                }
                else
                {
                    //Add an empty field
                    builder = builder.AddField($"{r} (0/{roleCount}):", "...");
                }
            }

            //Check if we need to add a "not included" category
            if (unused.Length > 0)
            {
                //Format the names
                var formatted = unused.Select(p =>
                {
                    //Get the name of the player
                    var name = (p.user_id.HasValue ? Bot.GetBotInstance().GetUserName(p.user_id.Value) : p.user_name);

                    //Check if backup
                    if (p.backup)
                    {
                        //Add cursive
                        return $"*{name}*";
                    }
                    else return name;
                }).ToArray();

                //Add the field
                builder = builder.AddField("Not included:", string.Join('\n', formatted));
            }

            //Build the embed
            var embed = builder.WithTitle("This is the best comp I could make:")
                               .Build();

            //Send the message
            ctx.message.Channel.SendMessageAsync("", false, embed).GetAwaiter().GetResult();
        }

        private void raid_create_comp_impl(Context ctx, string name, string roles)
        {
            //Get all the roles (including duplicates)
            var layout = Regex.Matches(roles, @"\w+")
                              .Select (r => r.Value.ToUpper())
                              .ToArray();

            //Check that we got a comp
            Precondition.Assert(layout.Length > 0, "No roles provided!");

            //Add the composition
            this.raidConfig.AddComposition(name.ToUpper(), layout);

            //Send success message
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Success", "Comp was created."
            );
        }

        private void raid_delete_comp_impl(Context ctx, string comp)
        {
            //Delete the raid
            this.raidConfig.RemoveComposition(comp);
        }

        private void raid_show_comps_impl(Context ctx)
        {
            //Get all the comps
            var comps = this.raidConfig.Compositions;

            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);

            //Go through each composition we have
            foreach (var kv in this.raidConfig.Compositions)
            {
                //Add the comp
                builder = builder.AddField(kv.Key, string.Join(", ", kv.Value));
            }

            //Build the embed
            var embed = builder.Build();

            //Send the message
            ctx.message.Channel.SendMessageAsync("", false, embed).GetAwaiter().GetResult();
        }

        private void raid_upload_logs_impl(Context ctx)
        {
            //Get the attachment
            var attachment = ctx.message.Attachments.First();

            //Send status report
            var dlStatus = ctx.message.Channel.SendMessageAsync("Downloading...");

            //Download it
            Stream file = null;
            using (var httpClient = new HttpClient())
            {
                file = httpClient.GetStreamAsync(attachment.Url).GetAwaiter().GetResult();
            }

            //Update status report
            var msg = dlStatus.GetAwaiter().GetResult();
            var zipStatus = msg.ModifyAsync(prop =>
            {
                prop.Content = "Unzipping...";
            });

            //Open the zip archive and accept only the first 30 distinct streams
            var streams = ZipHelper.GetUnzippedStreams(file)
                                   .GroupBy(t => t.filename).Select(g => g.First())
                                   .Take   (30).ToList();

            //Close the zip archive
            file.Close();
     
            //Update status report
            zipStatus.GetAwaiter().GetResult();
            var verifyStatus = msg.ModifyAsync(prop =>
            {
                prop.Content = "Verifying...";
            });

            //Check that we have some streams
            Precondition.Assert(streams.Count > 0, "No .evtc files found in zip!");

            //Get the encounter ids
            var encounters = streams.Select(log => Debug.Try(() =>
            {
                //Open the log
                using (var evtcReader = new EVTCReader(log.stream.GetStream()))
                {
                    //Read the header
                    var header = evtcReader.ReadHeader();

                    //Group filename and encounter id
                    return (log.filename, id: header.encounter_id);
                }
            }, default, severity: LOG_LEVEL.WARNING))
            .Where  (t => !string.IsNullOrEmpty(t.filename)) //Filter invalid logs
            .ToList ();

            //Check that we got some encounters
            Precondition.Assert(encounters.Count > 0, "No valid .evtc files found in zip!");

            //Join the encounter ids with the streams
            var data = encounters.Join(streams, outer => outer.filename, inner => inner.filename, (outer, inner) =>
            {
                return (outer.id, outer.filename, inner.stream);
            })
            .GroupBy(tuple => tuple.id).Select(g => g.First()) //Get unique encounters
            .ToList ();

            //Run the next part concurrently so we don't block the bot itself
            Task.Run(() =>
            {
                //Setup dictionaries to contain results
                var report = new Dictionary<short, string>();
                var raidar = new Dictionary<short, string>();
                data.ForEach(d =>
                {
                    report.Add(d.id, "Uploading");
                    raidar.Add(d.id, "Uploading");
                });

                //Update status report
                verifyStatus.GetAwaiter().GetResult();
                var uploadStatus = msg.ModifyAsync(prop =>
                {
                    prop.Content = "";
                    prop.Embed   = CreateLogEmbed(raidar, report);
                });

                //Setup uploader
                var uploader = new LogUploader();
                uploader.RegisterUploader(typeof(DPSReport.ReportUploadManager));
                uploader.RegisterUploader(typeof(GW2Raidar.RaidarUploadManager));

                //Listen to events
                var obj = new object();
                uploader.UploadStatusChanged += (o, e) =>
                {
                    //Lazy solution
                    lock (obj)
                    {
                        //Check if it's done
                        var done = (e.UploadStatus == UploadManager.LogUploadStatus.Succeeded);

                        //Determine the text to write
                        var text = (done ? $"[{e.HostName}]({e.URL})" : e.UploadStatus.ToString());

                        //Select the right dictionary to insert into
                        switch (e.HostName)
                        {
                            case "dps.report": report[(short)e.UniqueID] = text; break;
                            case "GW2Raidar":  raidar[(short)e.UniqueID] = text; break;
                        }

                        //Wait for discord
                        uploadStatus.GetAwaiter().GetResult();

                        //Update
                        uploadStatus = msg.ModifyAsync(prop =>
                        {
                            prop.Content = "";
                            prop.Embed   = CreateLogEmbed(raidar, report);
                        });
                    }
                };

                //Add items to upload
                data.ForEach(d => uploader.AddItem(d.stream, d.filename, d.id));

                //Upload the logs
                uploader.Start();
            });
        }

        private void raid_help_impl(Context ctx)
        {
            //Get all the commands and format them nicely
            var commands = this.Commands.Select(m => CommandManager.FormatCommandSignature(m));

            //Return commands
            Bot.GetBotInstance()
               .SendSuccessMessage(ctx.message.Channel,
                   "Commands:",
                   string.Join("\n", commands)
               );
        }

        private static Embed CreateLogEmbed(Dictionary<short, string> raidarResults, Dictionary<short, string> reportResults)
        {
            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);

            //Extract the results
            var results = raidarResults.Keys.Select(key =>
            {
                return (bossID: key, raidar: raidarResults[key], report: reportResults[key]);
            });

            //Setup footer for arnoud
            var footer = "";
            var regex  = new Regex(@"^(?:\[.+\]\((.+)\)|(.+))$");

            //Go through the groups
            results.GroupBy(r => GetBossIDOrder(r.bossID).group)
                   .OrderBy(g => g.Key).ToList().ForEach(g =>
            {
                //Iterate through the encounters in the proper order
                g.OrderBy(e => GetBossIDOrder(e.bossID).order)
                 .ToList().ForEach(encounter =>
                {
                    //Format the status
                    var title = TranslateBossID(encounter.bossID);
                    var value = $"{encounter.raidar} · {encounter.report}";

                    //Add to footer
                    var m1 = regex.Match(encounter.report);
                    var m2 = regex.Match(encounter.raidar);
                    footer += $"{(m1.Groups[1].Value + m1.Groups[2].Value)} {(m2.Groups[1].Value + m2.Groups[2].Value)} ";

                    //Add the field to the embed
                    builder = builder.AddInlineField(title, value);
                });
            });

            //Build and return the embed
            return builder.WithFooter(footer).Build();
        }

        private readonly struct PlayerAssignment
        {
            public PlayerAssignment(Entry player, string assignment)
            {
                this.player     = player;
                this.assignment = assignment;
            }

            public readonly Entry  player;
            public readonly string assignment;
        }

        private PlayerAssignment[] GenerateComp(/*readonly*/ Entry[] roster, ReadOnlySpan<string> layout, out Entry[] unused)
        {
            //Move backups to the end using a stable sort
            var finalRoster = Enumerable.OrderBy(roster, e => e.backup).ToArray();

            //Get all recognized roles
            var roles = this.raidConfig.Roles.ToArray();

            //Transform the layout into a representation our optimizer accepts
            var composition = Optimizer.PrepareComposition(layout, roles);

            //Transform the roster into a representation our optimizer accepts
            var processedRoster = Optimizer.PrepareRoster(finalRoster, roles);

            //Send to the optimizer
            var result = Optimizer.Optimize(composition, processedRoster);
            if (result.Length > 0)
            {
                //Map the result back into something we can understand
                var processedResult = result.Select (x => new PlayerAssignment(finalRoster[x.id], roles[(int)x.role]))
                                .ToArray();

                //Get anyone that is not included in the result
                var used = processedResult.Select(x => x.player).ToArray();
                unused = finalRoster.Where(e => !used.Contains(e)).ToArray();

                //Return the result
                return processedResult;
            }
            else
            {
                unused = finalRoster;
                return Array.Empty<PlayerAssignment>();
            }
        }
    }
}
