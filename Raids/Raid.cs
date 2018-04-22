using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

using DiscordBot.Utils;

namespace DiscordBot.Raids
{
    public struct RaidHandle
    {
        public string fullName;
        public long   timestamp;
        public int    raidID;
    }

    public struct Raid
    {
        public ulong       owner_id    { get; set; }
        public int         raid_id     { get; set; }
        public long        timestamp   { get; set; }
        public string      description { get; set; }
        public List<Entry> roster      { get; set; }
    }

    public struct Entry
    {
        public ulong        user_id { get; set; }
        public List<string> roles   { get; set; }
    }

    public static class RaidManager
    {
        /// <summary>
        /// Enumerates all the raids.
        /// </summary>
        /// <returns>
        /// An enumerable collection of handles that can
        /// be used for further manipulation and queries.
        /// </returns>
        public static IEnumerable<RaidHandle> EnumerateRaids()
        {
            //Create a regex to match a raid folder
            Regex regex = new Regex(@"^raid_(\d+)_(\d+)$");

            //Return a collection of raid file handles
            return Directory.EnumerateDirectories("./raids/") //Enumerate folders under raid directory
                            .Where (f => regex.IsMatch(f))    //Filter out non-raids
                            .Select(f => regex.Match(f))      //Extract timestamp and ID
                            .Select(r => new RaidHandle       //Wrap in a simple aggregate object  
                            {
                                fullName  = r.Name,
                                timestamp = long.Parse(r.Groups[1].Value),
                                raidID    = int .Parse(r.Groups[2].Value)
                            });
        }

        /// <summary>
        /// Searches for an available ID to assign to a new raid.
        /// </summary>
        /// <returns>
        /// A number between 1 and <see cref="Int32.MaxValue"/> if successful,
        /// returns -1 if the operation failed.
        /// </returns>
        private static int GetNextAvailableRaidID()
        {
            //Catch any errors
            return Debug.Try<int>(() =>
            {
                //Find used raid IDs
                var ids = RaidManager.EnumerateRaids()
                                     .Select  (r => r.raidID)
                                     .Distinct();

                //Create the range we will search through
                var searchSpace = Enumerable.Range(1, ids.Max());

                //Find the next available ID
                return searchSpace.Except(ids).Min();
            }, -1);
        }

        /// <summary>
        /// Initialises a new raid.
        /// </summary>
        /// <param name="offset">The time offset for when the raid starts.</param>
        /// <param name="description">The description of the raid.</param>
        /// <returns>
        /// Returns the assigned ID for the raid which will be in the range 1 to
        /// <see cref="Int32.MaxValue"/> if successful, otherwise it will return -1.
        /// </returns>
        private static int CreateRaid(DateTimeOffset offset, string description)
        {
            //Find an available ID
            int raidID = RaidManager.GetNextAvailableRaidID();

            //Catch any errors
            return Debug.Try<int>(() =>
            {
                //Check that the ID is valid
                Debug.Assert(raidID > 0, "Couldn't get a valid raid ID");

                //Determine the folder name
                var name = $"raid_{offset.ToUnixTimeSeconds()}_{raidID}";

                //Create directory for the raid
                var dir = Directory.CreateDirectory($"./raids/{name}/");

                //Check that it was created
                Debug.Assert(dir.Exists, "Couldn't create raid directory");

                //Create the initial roster file
                using (FileStream fs = File.Open($@"./raids/{name}/roster.txt", FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    /*//Get a UTF8 encoded text stream
                    using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                    {
                        //Write description
                        sw.WriteLine(description);
                    }*/
                }

                //Return success
                return raidID;
            }, -1);
        }

        /// <summary>
        /// Appends a raider to the raid roster.
        /// Note: Changes to role will simply be documented with a later entry to the file.
        /// Shouldn't be an issue.
        /// </summary>
        /// <param name="raidID">The ID of the raid.</param>
        /// <param name="userID">The user's unique ID on Discord.</param>
        /// <param name="roles">A space-separated list of roles in upper-case.</param>
        /// <returns>Returns whether the operation was successful or not.</returns>
        /*private static bool AppendRaider(int raidID, UInt64 userID, string roles)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Find the correct raid folder
                var raid = EnumerateRaids().Where(r => r.raidID == raidID).First();

                //Keep trying until we succeed
                Utility.WithRetry((retryNum) =>
                {
                    //Sleep between retries
                    if (retryNum > 0) Thread.Sleep(100);

                    //Catch IO errors thrown when we can't get exclusive access
                    try
                    {
                        //Get an exclusive handle to the file
                        using (FileStream fs = File.Open($@"./raids/{raid.fullName}/roster.txt", FileMode.Append, FileAccess.Write, FileShare.None))
                        {
                            //Get a UTF8 encoded text stream
                            using (StreamWriter sw = new StreamWriter(fs, Encoding.UTF8))
                            {
                                //Append raider
                                sw.WriteLine($"{userID} - {roles}");
                            }
                        }

                        //Return success
                        return true;
                    }
                    catch (IOException ex)
                    {
                        //Check that it's not some other error
                        Debug.Assert(ex.HResult == -2147024864, "Unexpected IO exception.");

                        //Return failure
                        return false;
                    }
                }, 10);

                //Return success
                return true;
            });
        }*/

        /// <summary>
        /// Removes old raid files.
        /// </summary>
        /// <param name="maxAge">
        /// The allowed age before the file is removed.
        /// Starts counting after start of raid.
        /// </param>
        private static void CleanRaidFiles(TimeSpan maxAge)
        {
            //Get current time in UTC+0
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            //Find the raids we want to delete
            RaidManager.EnumerateRaids().Where(r =>
            {
                //Calculate age in seconds
                var age = (now - r.timestamp);

                //Wrap age in a TimeSpan object
                var t = TimeSpan.FromTicks(TimeSpan.TicksPerSecond * age);

                //Keep only old raids
                return t > maxAge;
            }).ToList().ForEach(r =>
            {
                //Try deleting directory (with contents)
                Debug.Try(() => Directory.Delete($"./raids/{r.fullName}", true));
            });
        }
    }
}
