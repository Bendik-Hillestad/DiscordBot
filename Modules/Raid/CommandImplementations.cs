using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Discord;
using DiscordBot.Commands;
using DiscordBot.Core;
using DiscordBot.Raids;
using DiscordBot.Utils;

namespace DiscordBot.Modules.Raid
{
    public partial class RaidModule : CommandModule<RaidModule>
    {
        [DllImport("libherrington")]
        private static extern void solve(uint idx, IntPtr users, int length, IntPtr output);

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
            var filter = this.GetRoles(roles);

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

        private void raid_roles_impl(Context ctx)
        {
            //Get all the roles we recognize
            var roles = this.raidConfig.GetAllRoles();

            //Display the roles
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                "Roles:",
                string.Join(", ", roles)
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
            var extractedRoles = this.GetRoles(roles);
            bool bu = false;

            //Check if one of the roles is BACKUP
            if (roles.ToUpper().Contains("BACKUP"))
            {
                //Set flag
                bu = true;
            }

            //Check that we got any roles
            Precondition.Assert(extractedRoles.Count > 0, "No recognized roles provided!");

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
            var extractedRoles = this.GetRoles(roles);
            bool bu = false;

            //Check if one of the roles is BACKUP
            if (roles.ToUpper().Contains("BACKUP"))
            {
                //Set flag
                bu = true;
            }

            //Check that we got any roles
            Precondition.Assert(extractedRoles.Count > 0, "No recognized roles provided!");

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

        private void raid_make_comp_impl(Context ctx, string compName, int id)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(id).Value;

            //Find the comp
            var compIdx = this.raidConfig.GetCompIndex(compName.ToUpper());

            //Check if valid
            Precondition.Assert
            (
                compIdx != -1,
                "No comp with that name. These are the recognised comps: \n" +
                string.Join(", ", this.raidConfig.GetCompNames())
            );

            //Generate composition
            var bestComp = this.GenerateComp(handle, compIdx, out var unused);

            //Prepare the categories
            var categories = this.raidConfig.GetRoleCounts(compIdx)
                                            .Where (val => val.Value > 0)
                                            .ToList();

            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);


            //Iterate over the categories
            var offset = 0;
            categories.ForEach(c =>
            {
                //Get title
                var title = $"{c.Key}:";

                //Get all the members in this category
                var members = bestComp.Skip(offset).Take(c.Value).Select(e =>
                {
                    //Check that it's not null
                    if (e.HasValue)
                    {
                        //Get the entry
                        var entry = e.Value;

                        //Check if this entry is a backup
                        if (entry.backup)
                        {
                            //Return the name with cursive text
                            return "*" + ((entry.user_id.HasValue) ? Bot.GetBotInstance().GetUserName(entry.user_id.Value)
                                                                   : entry.user_name) + "*";
                        }

                        //Return the name with normal text
                        return (entry.user_id.HasValue) ? Bot.GetBotInstance().GetUserName(entry.user_id.Value)
                                                        : entry.user_name;
                    }

                    //Just fill in with empty
                    return "<empty>";
                });

                //Add the field
                builder = builder.AddField(title, string.Join("\n", members));

                //Update offset
                offset += c.Value;
            });

            //Check if we need to add an "not included" category
            if (unused.Count > 0)
            {
                //Get the names of the entries
                var names = unused.Select(e =>
                {
                    //Check if this entry is a backup
                    if (e.backup)
                    {
                        //Return the name with cursive text
                        return "*" + ((e.user_id.HasValue) ? Bot.GetBotInstance().GetUserName(e.user_id.Value)
                                                           : e.user_name) + "*";
                    }

                    //Return the name with normal text
                    return ((e.user_id.HasValue) ? Bot.GetBotInstance().GetUserName(e.user_id.Value)
                                                 : e.user_name);
                });

                //Add the field
                builder = builder.AddField("Not included:", string.Join("\n", names));
            }

            //Build the embed
            var embed = builder.WithTitle("This is the best comp I could make:")
                               .Build    ();

            //Send the message
            ctx.message.Channel.SendMessageAsync("", false, embed).GetAwaiter().GetResult();
        }

        private void raid_create_comp_impl(Context ctx, string name, string roles)
        {
            //Get all the roles (including duplicates)
            var comp = Regex.Matches(roles, @"\w+")
                            .Select (r => r.Value.ToUpper())
                            .ToList ();

            //Check that we got a comp
            Precondition.Assert(comp.Count > 0, "No roles provided!");

            //Add the comp description
            this.raidConfig.AddCompDescription(new CompDescription
            {
                Name   = name.ToUpper(),
                Layout = comp
            });

            //Save and compile
            bool success = Debug.Try(() =>
            {
                this.raidConfig.SaveConfig();
                this.raidConfig.GenerateSolverLibrary();
            });

            //Check result
            if (success)
            {
                //Send success message
                Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel,
                    "Success", "Comp was created."
                );
            }
            else
            {
                //Send error message
                Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel,
                    "Error", "There was an error #blamearnoud."
                );
            }
        }

        private void raid_delete_comp_impl(Context ctx, int compIdx)
        {
            //Delete the raid
            this.raidConfig.Compositions.RemoveAt(compIdx);

            //Save the config and recompile
            Debug.Try(() =>
            {
                this.raidConfig.SaveConfig();
                this.raidConfig.GenerateSolverLibrary();
            });
        }

        private void raid_show_comps_impl(Context ctx)
        {
            //Get all the comps
            var comps = this.raidConfig.Compositions;

            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);

            //Go through each composition we have
            this.raidConfig.Compositions.ForEach(c =>
            {
                //Add the comp
                builder = builder.AddField(c.Name, string.Join(", ", c.Layout));
            });

            //Build the embed
            var embed = builder.Build();

            //Send the message
            ctx.message.Channel.SendMessageAsync("", false, embed).GetAwaiter().GetResult();
        }

        private void raid_upload_logs_impl(Context ctx, int id)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID(id).Value;

            //Determine the folder to unzip to
            var dst = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + Path.DirectorySeparatorChar);

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

            //Open the zip archive
            using (var zip = new ZipArchive(file, ZipArchiveMode.Read))
            {
                //Unzip the file to the destination
                zip.ExtractToDirectory(dst, true);
            }

            //Update status report
            zipStatus.GetAwaiter().GetResult();
            var uploadStatus = msg.ModifyAsync(prop =>
            {
                prop.Content = "Uploading...";
            });

            //Find all the logs
            var logs = Directory.EnumerateFiles(dst)
                                .Where (f => string.Equals(Path.GetExtension(f), ".evtc"))
                                .ToList();

            //Run the next part concurrently so we don't block the bot itself
            Task.Run(() =>
            {
                //Asynchronously upload the logs
                var tasks = logs.Select(log => Task.Run(() =>
                {
                    //TODO: Upload to GW2Raidar

                    //Upload to dps.report
                    var resp = DPSReport.Client.UploadLog(log);

                    //Store the response
                    return resp;
                })).ToArray();

                //Wait for the uploads
                Task.WaitAll(tasks);

                //Get the links
                var links = tasks.Select(t => t.Result)
                                 .Where (r => r != null && string.IsNullOrEmpty(r.error))
                                 .Select(r => new Tuple<int, string>(r.metadata.evtc.bossId, r.permalink))
                                 .ToList();

                //Setup the embed builder
                var builder = new EmbedBuilder().WithColor(Color.Blue);

                //Add all the bosses
                links.ForEach(link => builder = builder.AddInlineField(""+link.Item1, $"[dps.report]({link.Item2})"));

                //Send success message
                msg.ModifyAsync(prop =>
                {
                    prop.Content = "";
                    prop.Embed   = builder.WithDescription("Logs uploaded!").Build();
                }).GetAwaiter().GetResult();

                //Delete the directory
                Debug.Try(() => Directory.Delete(dst), severity: LOG_LEVEL.WARNING);
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

        private List<string> GetRoles(string roleList)
        {
            //Create our regex
            var regex = this.raidConfig
                            .GetAllRoles      ()
                            .OrderByDescending(s => s.Length)
                            .Aggregate        ((s, s2) => s + "|" + s2);

            //Check for matches
            return Regex.Matches (roleList, regex, RegexOptions.IgnoreCase)
                        .Select  (r => r.Value.ToUpper())
                        .Distinct().ToList();
        }

        private Dictionary<string, float> GetRoleWeights(Entry e, int compIdx)
        {
            //Filter the user's roles based on what this comp needs
            var roles = e.roles.Intersect(this.raidConfig.GetRolesForComp(compIdx));

            //Calculate the weights
            var weights = new Dictionary<string, float>();
            roles.Select((r, i) =>
            {
                if (i == 0) return new KeyValuePair<string, float>(r, 1.5f);
                else        return new KeyValuePair<string, float>(r, 1.0f);
            }).ToList().ForEach(w => weights.Add(w.Key, w.Value));

            //Return the weights
            return weights;
        }

        private static ulong HashEntry(Entry e)
        {
            unchecked
            {
                //Prepare the has with the FNV offset basis 
                ulong hash = 14695981039346656037;

                //Perform the main FNV-1a hash routine
                foreach (char c in e.user_name)
                {
                    hash = hash ^ c;
                    hash = hash * 1099511628211;
                }

                //Return the resulting hash value
                return hash;
            }
        }

        private Entry?[] MakeRaidComp(in List<Entry> raiders, int compIdx)
        {
            //Get the roles
            var roles     = this.raidConfig.GetAllRoles();

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
            raiders.ForEach(r =>
            {
                //Get the user id
                var id = r.user_id;

                //Check if we don't have a user id
                if (!r.user_id.HasValue)
                {
                    //Create a temporary id (we pray for no hash collision)
                    id = HashEntry(r);
                }

                //Write the id into the array
                Marshal.WriteInt64(offset, (Int64)id.Value);
                var next = offset + userSize;
                offset  += sizeof(ulong);

                //Get the role weights
                var weights = this.GetRoleWeights(r, compIdx);

                //Calculate a bias by squashing the join index into the [0, 1] range
                float bias = 1.0f - (idx / (idx + 2.0f * compSize));

                //Iterate through the roles
                roles.ForEach(role =>
                {
                    //Get the weight for this role
                    float weight = weights.GetValueOrDefault(role);

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
            var result = new Entry?[compSize];

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
                    result[i] = raiders.Find(r => (r.user_id.HasValue) ? (r.user_id.Value == id) : (HashEntry(r) == id));
                }
                else result[i] = null;
            }

            //Release our unmanaged memory
            Marshal.FreeHGlobal(input);
            Marshal.FreeHGlobal(output);

            //Return the result
            return result;
        }

        private Entry?[] GenerateComp(RaidHandle handle, int compIdx, out List<Entry> unused)
        {
            //Get the raiders
            var raiders = RaidManager.CoalesceRaiders(handle);

            //Send to solver
            var comp = this.MakeRaidComp(raiders, compIdx);

            //Get anyone that is not included
            unused = raiders.Where(e => !comp.Contains(e)).ToList();

            //Return the comp
            return comp;
        }
    }
}
