using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using DiscordBot.Utils;

using Newtonsoft.Json;

namespace DiscordBot.Raids
{
    public struct RaidHandle
    {
        public string full_name;
        public long   timestamp;
        public int    raid_id;
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
        public ulong?       user_id   { get; set; }
        public string       user_name { get; set; }
        public bool         backup    { get; set; }
        public List<string> roles     { get; set; }

        public override bool Equals(object obj)
        {
            //Cast to Entry
            var tmp = obj as Entry?;

            //Check if successful
            if (tmp.HasValue)
            {
                //Get the value
                var other = tmp.Value;

                //Check id
                if (this.user_id.HasValue)
                {
                    return (other.user_id.HasValue) && (this.user_id.Value == other.user_id.Value);
                }

                //Check name
                return string.Equals(this.user_name, other.user_name);
            }

            //Different types
            return false;
        }

        public override int GetHashCode()
        {
            //Check id
            if (this.user_id.HasValue)  return this.user_id  .GetHashCode();

            //Check name
            if (this.user_name != null) return this.user_name.GetHashCode();

            return 0;
        }
    }

    public static class RaidManager
    {
        private static T MaxOrDefault<T>(this IEnumerable<T> enumeration) where T : struct
        {
            return (enumeration.Max(value => (T?)value) ?? default(T));
        }

        /// <summary>
        /// Performs some simple initialisation to ensure
        /// that all necessary files and folders exist.
        /// </summary>
        public static bool Initialise()
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Check if the raids folder doesn't exist
                if (!Directory.Exists("./raids/"))
                {
                    //Create the directory
                    Directory.CreateDirectory("./raids/");
                }
            });
        }

        /// <summary>
        /// Enumerates all the raids.
        /// </summary>
        /// <returns>
        /// An enumerable collection of handles that can
        /// be used for further manipulation and queries.
        /// </returns>
        public static IEnumerable<RaidHandle> EnumerateRaids()
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Create a regex to match a raid folder
                var regex = new Regex(@"raid_(\d+)_(\d+)$");

                //Return a collection of raid file handles
                return Directory.EnumerateDirectories("./raids/") //Enumerate folders under raid directory
                                .Where (f => regex.IsMatch(f))    //Filter out non-raids
                                .Select(f => regex.Match(f))      //Extract timestamp and ID
                                .Select(r => new RaidHandle       //Wrap in a simple aggregate object  
                                {
                                    full_name = r.Value,
                                    timestamp = long.Parse(r.Groups[1].Value),
                                    raid_id   = int .Parse(r.Groups[2].Value)
                                });
            }, new RaidHandle[]{ });
        }

        /// <summary>
        /// Retrieves a handle to the raid matching the given id.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        public static RaidHandle? GetRaidFromID(int raid_id)
        {
            //Catch any errors
            return Debug.Try<RaidHandle?>(() =>
            {
                //Find the raid that matches the ID
                return EnumerateRaids().First(r => r.raid_id == raid_id);
            }, null);
        }

        /// <summary>
        /// Searches for an available ID to assign to a new raid.
        /// </summary>
        /// <returns>
        /// A number between 1 and <see cref="Int32.MaxValue"/> if successful,
        /// returns -1 if the operation failed.
        /// </returns>
        public static int GetNextAvailableRaidID()
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Find used raid IDs
                var ids = EnumerateRaids().Select(r => r.raid_id);

                //Create the range we will search through
                var searchSpace = Enumerable.Range(1, ids.MaxOrDefault() + 1);

                //Find the next available ID
                return searchSpace.Except(ids).Min();
            }, -1);
        }

        /// <summary>
        /// Initialises a new raid.
        /// </summary>
        /// <param name="owner_id">The raid owner's unique ID on Discord.</param>
        /// <param name="offset">The time offset for when the raid starts.</param>
        /// <param name="description">The description of the raid.</param>
        /// <returns>
        /// Returns the assigned ID for the raid which will be in the range 1 to
        /// <see cref="Int32.MaxValue"/> if successful, otherwise it will return -1.
        /// </returns>
        public static int CreateRaid(ulong owner_id, DateTimeOffset offset, string description)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Construct a Raid object
                var raid = new Raid
                {
                    owner_id    = owner_id,
                    raid_id     = GetNextAvailableRaidID(),
                    timestamp   = offset.ToUnixTimeSeconds(),
                    description = description,
                    roster      = new List<Entry>()
                };

                //Check that the ID is valid
                Debug.Assert(raid.raid_id > 0, "Couldn't get a valid raid ID");

                //Determine the folder name
                var name = $"raid_{raid.timestamp}_{raid.raid_id}";

                //Create directory for the raid
                Directory.CreateDirectory($"./raids/{name}/");

                //Create the initial raid file
                using (var sw = File.CreateText($"./raids/{name}/raid.json"))
                {
                    //Serialise the Raid object
                    sw.Write(JsonConvert.SerializeObject(raid, Formatting.Indented));
                }

                //Return the assigned id
                return raid.raid_id;
            }, -1);
        }

        /// <summary>
        /// Deletes the specified raid.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        /// <returns>Returns whether the operation was successful or not.</returns>
        public static bool DeleteRaid(int raid_id)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get a handle to the raid
                var handle = GetRaidFromID(raid_id).Value;

                //Delete the raid folder and all content in it
                Directory.Delete($"./raids/{handle.full_name}/", true);
            });
        }

        private static void AppendRaider(RaidHandle handle, Entry entry)
        {
            //Open the raid file
            using (FileStream fs = File.Open($"./raids/{handle.full_name}/raid.json", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                //Prepare the structure holding the data
                Raid raid;

                //Get UTF-8 encoded text streams
                StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                //Deserialise the JSON
                raid = JsonConvert.DeserializeObject<Raid>(sr.ReadToEnd());

                //Reset the stream
                fs.Seek     (0, SeekOrigin.Begin);
                fs.SetLength(0);

                //Append the raider
                raid.roster.Add(entry);

                //Serialise the Raid object
                sw.Write(JsonConvert.SerializeObject(raid, Formatting.Indented));
                sw.Flush();
            }
        }

        /// <summary>
        /// Appends a raider to the raid roster.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        /// <param name="user_id">The user's unique ID on Discord.</param>
        /// <param name="roles">A collection of roles the user can take.</param>
        /// <returns>Returns whether the operation was successful or not.</returns>
        public static bool AppendRaider(int raid_id, ulong user_id, bool backup, IEnumerable<string> roles)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get a handle to the raid
                var handle = GetRaidFromID(raid_id).Value;

                //Setup the entry
                var entry = new Entry { user_id = user_id, user_name = null, backup = backup, roles = roles.ToList() };

                //Append the raider
                AppendRaider(handle, entry);
            });
        }

        /// <summary>
        /// Appends a raider to the raid roster.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        /// <param name="user_name">The user's name.</param>
        /// <param name="roles">A collection of roles the user can take.</param>
        /// <returns>Returns whether the operation was successful or not.</returns>
        public static bool AppendRaider(int raid_id, string user_name, bool backup, IEnumerable<string> roles)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get a handle to the raid
                var handle = GetRaidFromID(raid_id).Value;

                //Setup the entry
                var entry = new Entry { user_id = null, user_name = user_name, backup = backup, roles = roles.ToList() };

                //Append the raider
                AppendRaider(handle, entry);
            });
        }

        private static void RemoveRaider(RaidHandle handle, Entry entry)
        {
            //Open the raid file
            using (FileStream fs = File.Open($"./raids/{handle.full_name}/raid.json", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                //Prepare the structure holding the data
                Raid raid;

                //Get UTF-8 encoded text streams
                StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                //Deserialise the JSON
                raid = JsonConvert.DeserializeObject<Raid>(sr.ReadToEnd());

                //Reset the stream
                fs.Seek     (0, SeekOrigin.Begin);
                fs.SetLength(0);

                //Remove the raider
                raid.roster.RemoveAll(e => e.Equals(entry));

                //Serialise the Raid object
                sw.Write(JsonConvert.SerializeObject(raid, Formatting.Indented));
                sw.Flush();
            }
        }

        /// <summary>
        /// Removes a raider from the roster.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        /// <param name="user_id">The user's unique ID on Discord.</param>
        /// <returns>Returns whether the operation was successful or not.</returns>
        public static bool RemoveRaider(int raid_id, ulong user_id)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get a handle to the raid
                var handle = GetRaidFromID(raid_id).Value;

                //Create a dummy entry to compare against
                var entry = new Entry { user_id = user_id, user_name = null };

                //Remove the raider
                RemoveRaider(handle, entry);
            });
        }

        /// <summary>
        /// Removes a raider from the roster.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        /// <param name="user_name">The user's name.</param>
        /// <returns>Returns whether the operation was successful or not.</returns>
        public static bool RemoveRaider(int raid_id, string user_name)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get a handle to the raid
                var handle = GetRaidFromID(raid_id).Value;

                //Create a dummy entry to compare against
                var entry = new Entry { user_id = null, user_name = user_name };

                //Remove the raider
                RemoveRaider(handle, entry);
            });
        }

        private static IEnumerable<Entry> ReadRoster(RaidHandle handle)
        {
            //Open the raid file
            using (FileStream fs = File.Open($"./raids/{handle.full_name}/raid.json", FileMode.Open, FileAccess.Read, FileShare.None))
            {
                //Get UTF-8 encoded text streams
                StreamReader sr = new StreamReader(fs, Encoding.UTF8);

                //Deserialise the JSON and return the roster
                return JsonConvert.DeserializeObject<Raid>(sr.ReadToEnd()).roster;
            }
        }

        /// <summary>
        /// Takes the full record of entries and coalesces it
        /// into a single list with unique entries and the most
        /// recent values.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        /// <returns>
        /// Returns a distinct list of raiders with the most
        /// up-to-date values.
        /// </returns>
        public static List<Entry> CoalesceRaiders(int raid_id)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get a handle to the raid
                var handle = GetRaidFromID(raid_id).Value;

                //Get the roster
                var roster = ReadRoster(handle);

                //Check if it's empty
                if (roster.Count() == 0) return new List<Entry>();

                //Find the most recent entries for each user
                var entries = roster.Reverse().Distinct();

                //Return the most recent entries while maintaining the correct join order
                return roster.Distinct()
                             .Select  (e => entries.First(e2 => e2.Equals(e)))
                             .ToList  ();
            }, new List<Entry>());
        }

        /// <summary>
        /// Removes old raid files.
        /// </summary>
        /// <param name="maxAge">
        /// The allowed age before the file is removed.
        /// Starts counting after start of raid.
        /// </param>
        public static bool CleanRaidFiles(TimeSpan maxAge)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get current time in UTC+0
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                //Find the raids we want to delete
                EnumerateRaids().Where(r =>
                {
                    //Calculate age in seconds
                    var age = (now - r.timestamp);

                    //Wrap age in a TimeSpan object
                    var t = TimeSpan.FromTicks(TimeSpan.TicksPerSecond * age);

                    //Keep only old raids
                    return t > maxAge;
                }).ToList().ForEach(r =>
                {
                    //Delete the raid folder and all content in it
                    Directory.Delete($"./raids/{r.full_name}/", true);
                });
            });
        }
    }
}
