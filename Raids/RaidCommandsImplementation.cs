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
        [DllImport("libherrington")]
        private static extern void solve(uint idx, IntPtr users, int length, IntPtr output);

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
            var roles = e.roles.Union(this.raidConfig.GetRolesForComp(compIdx));

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

        private Entry?[] MakeRaidComp(List<Entry> raiders, int compIdx)
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
            raiders.ForEach((r) =>
            {
                //Write the id into the array
                Marshal.WriteInt64(offset, (long)r.user_id);
                var next = offset + userSize;
                offset  += sizeof(ulong);

                //Get the role weights
                var weights = this.GetRoleWeights(r, compIdx);

                //Calculate a bias by squashing the join index into the [0, 1] range
                float bias = 1.0f - (idx / (idx + 2.0f * compSize));

                //Iterate through the roles
                roles.ForEach((role) =>
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
                    result[i] = raiders.Find(r => id == r.user_id);
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

        private string CmdRaidCreate_Implementation(SocketUserMessage msg, DateTimeOffset offset, string desc)
        {
            //Create the raid
            var handle = RaidManager.CreateRaid(msg.Author.Id, offset, desc).Value;

            //Return success
            return $"Raid has been created (ID: {handle.raid_id}).";
        }

        private string CmdRaidDelete_Implementation(SocketUserMessage msg, RaidHandle handle)
        {
            //Grab the data from the raid
            var data = RaidManager.GetRaidData(handle);

            //Check if valid
            if (data.HasValue)
            {
                //Get the owner
                var owner_id = data.Value.owner_id;

                //Check if the user is the owner
                if (msg.Author.Id == owner_id)
                {
                    //Delete the raid
                    RaidManager.DeleteRaid(handle);

                    //Return success
                    return "Raid has been deleted.";
                }

                //Return permission error
                return "You do not have the permission to delete that raid.";
            }

            //Return generic error
            return "There was an error processing the raid.";
        }

        private string CmdRaidRoster_Implementation(SocketUserMessage msg, RaidHandle handle, List<string> filter)
        {
            //Get the raiders that match the filter
            var roster = RaidManager.CoalesceRaiders(handle)
                                    .Where (e => e.roles.Union(filter).Count() > 0)
                                    .Select(e => $"{e.user_id} - {string.Join(", ", e.roles)}")
                                    .ToList();

            //Check if there are any
            if (roster.Count > 0)
            {
                //Return the roster
                return $"Result:\n{string.Join("\n", roster)}\nCount: {roster.Count}";
            }

            //None found
            return "Result: None";
        }

        private string CmdRaidList_Implementation(SocketUserMessage msg)
        {
            //Generate the list of events
            var events = RaidManager.EnumerateRaids()
                                    .Select(r => RaidManager.GetRaidData(r))
                                    .Select(r => $"[{r?.raid_id}] - {r?.description}")
                                    .ToList();

            //Check if there are any
            if (events.Count > 0)
            {
                //Return list
                return $"Result:\n{string.Join("\n", events)}";
            }

            //None found
            return "Result: None";
        }

        private string CmdRaidJoin_Implementation(SocketUserMessage msg, RaidHandle handle, bool backup, List<string> roles)
        {
            //Check that we got any roles
            if (roles.Count > 0)
            {
                //Add to the raid
                RaidManager.AppendRaider(handle, msg.Author.Id, backup, roles);

                //Return success
                return $"You were added to the raid{(backup ? " as backup" : "")} with these roles: \"{string.Join(", ", roles)}\".";
            }

            //Return missing roles error
            return "No recognized roles provided.";
        }
    }
}
