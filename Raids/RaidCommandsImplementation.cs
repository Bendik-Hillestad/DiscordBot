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
        /* Spooky unsafe C++ stuff */

        [DllImport("libherrington")]
        private static extern void solve(uint idx, IntPtr users, int length, IntPtr output);

        /* End of spooky unsafe C++ stuff */

        /* Nice C# wrapper for spooky unsafe C++ stuff */

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

        private List<string> GetRoles(string roleList)
        {
            //Create our regex
            var regex = this.raidConfig
                            .GetAllRoles      ()
                            .OrderByDescending(s => s.Length)
                            .Aggregate        ((s, s2) => s + "|" + s2);

            //Check for matches
            return Regex.Matches (roleList, regex, RegexOptions.IgnoreCase)
                        .Select  ((r) => r.Value.ToUpper())
                        .Distinct()
                        .ToList  ();
        }
    }
}
