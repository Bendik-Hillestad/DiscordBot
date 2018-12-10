using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

using DiscordBot.Core;
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
        public bool        sell        { get; set; }
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
        /// Returns an enumerable collection of handles
        /// to the raids that can be used for further 
        /// manipulation and queries.
        /// </summary>
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
            }, new List<RaidHandle>());
        }

        /// <summary>
        /// Gets a handle to the next raid, based on the
        /// earliest timestamp.
        /// </summary>
        public static RaidHandle? GetNextRaid()
        {
            //Catch any errors
            return Debug.Try<RaidHandle?>(() =>
            {
                //Enumerate and return the handle with the earliest timestamp
                return EnumerateRaids().OrderBy(r => r.timestamp)
                                       .First  ();
            }, null, severity: LOG_LEVEL.INFO);
        }

        /// <summary>
        /// Gets a read-only view of the data for a given raid.
        /// </summary>
        /// <param name="handle">The handle to the raid.</param>
        public static Raid? GetRaidData(RaidHandle handle)
        {
            //Catch any errors
            return Debug.Try<Raid?>(() =>
            {
                //Open the raid file
                using (FileStream fs = File.Open($"./raids/{handle.full_name}/raid.json", FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    //Get a UTF-8 encoded text stream
                    StreamReader sr = new StreamReader(fs, Encoding.UTF8);

                    //Deserialise the JSON and return the roster
                    return JsonConvert.DeserializeObject<Raid>(sr.ReadToEnd());
                }
            }, null);
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
        /// Initialises a new raid and returns a handle to it.
        /// </summary>
        /// <param name="owner_id">The raid owner's unique ID on Discord.</param>
        /// <param name="offset">The time offset for when the raid starts.</param>
        /// <param name="description">The description of the raid.</param>
        public static RaidHandle? CreateRaid(ulong owner_id, DateTimeOffset offset, string description, bool isSellingRaid = false)
        {
            //Catch any errors
            return Debug.Try<RaidHandle?>(() =>
            {
                //Get a handle to a new raid
                var handle = CreateNewRaidHandle(offset).Value;

                //Construct a Raid object
                var raid = new Raid
                {
                    owner_id    = owner_id,
                    raid_id     = handle.raid_id,
                    timestamp   = handle.timestamp,
                    description = description,
                    roster      = new List<Entry>(),
                    sell        = isSellingRaid
                };

                //Create the initial raid file
                using (var sw = File.CreateText($"./raids/{handle.full_name}/raid.json"))
                {
                    //Serialise the Raid object
                    sw.Write(JsonConvert.SerializeObject(raid, Formatting.Indented));
                    sw.Flush();
                }

                //Return the handle
                return handle;
            }, null);
        }

        /// <summary>
        /// Deletes the specified raid.
        /// </summary>
        /// <param name="handle">The handle to the raid.</param>
        public static bool DeleteRaid(RaidHandle handle)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Delete the raid folder and all content in it
                Directory.Delete($"./raids/{handle.full_name}/", true);
            });
        }

        /// <summary>
        /// Appends a raider to the raid roster.
        /// </summary>
        /// <param name="handle">The handle to the raid.</param>
        /// <param name="user_id">The user's unique ID on Discord.</param>
        /// <param name="roles">A collection of roles the user can take.</param>
        public static bool AppendRaider(RaidHandle handle, ulong user_id, bool backup, IEnumerable<string> roles)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Setup the entry
                var entry = new Entry
                {
                    user_id   = user_id,
                    user_name = null,
                    backup    = backup,
                    roles     = roles.ToList()
                };

                //Append the raider
                AppendRaider(handle, entry);
            });
        }

        /// <summary>
        /// Appends a raider to the raid roster.
        /// </summary>
        /// <param name="handle">The handle to the raid.</param>
        /// <param name="user_name">The user's name.</param>
        /// <param name="roles">A collection of roles the user can take.</param>
        public static bool AppendRaider(RaidHandle handle, string user_name, bool backup, IEnumerable<string> roles)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Setup the entry
                var entry = new Entry
                {
                    user_id   = null,
                    user_name = user_name,
                    backup    = backup,
                    roles     = roles.ToList()
                };

                //Append the raider
                AppendRaider(handle, entry);
            });
        }

        /// <summary>
        /// Removes a raider from the roster.
        /// </summary>
        /// <param name="handle">The handle to the raid.</param>
        /// <param name="entry">The handle to the user.</param>
        public static bool RemoveRaider(RaidHandle handle, Entry entry)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Open the raid file
                using (FileStream fs = File.Open($"./raids/{handle.full_name}/raid.json", FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    //Get UTF-8 encoded text streams
                    StreamReader sr = new StreamReader(fs, Encoding.UTF8);
                    StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                    //Deserialise the JSON
                    var raid = JsonConvert.DeserializeObject<Raid>(sr.ReadToEnd());

                    //Reset the stream
                    fs.Seek     (0, SeekOrigin.Begin);
                    fs.SetLength(0);

                    //Remove the raider
                    raid.roster.RemoveAll(e => e.Equals(entry));

                    //Serialise the Raid object
                    sw.Write(JsonConvert.SerializeObject(raid, Formatting.Indented));
                    sw.Flush();
                }
            });
        }

        /// <summary>
        /// Finds any raiders that match a given name.
        /// </summary>
        /// <param name="handle">The handle to the raid.</param>
        /// <param name="user_name">The user's name.</param>
        public static List<Entry> FindRaiders(RaidHandle handle, string user_name)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Find any that match the name
                return RaidManager.CoalesceRaiders(handle)
                                  .Where (r => ResolveName(r).ToLower().Contains(user_name.ToLower()))
                                  .ToList();
            }, new List<Entry>(), severity: LOG_LEVEL.INFO);
        }

        /// <summary>
        /// Find the raider that matches the id.
        /// </summary>
        /// <param name="handle">The handle to the raid.</param>
        /// <param name="user_id">The user's unique ID on Discord.</param>
        public static Entry? FindRaider(RaidHandle handle, ulong user_id)
        {
            //Catch any errors
            return Debug.Try<Entry?>(() =>
            {
                //Find the raider with the same id
                return CoalesceRaiders(handle).First(r => r.user_id == user_id);
            }, null, severity: LOG_LEVEL.INFO);
        }

        /// <summary>
        /// Returns a distinct list of raiders with the most
        /// up-to-date values.
        /// </summary>
        /// <param name="raid_id">The ID of the raid.</param>
        public static List<Entry> CoalesceRaiders(RaidHandle handle)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get the roster history
                var roster = GetRosterHistory(handle);

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
        /// <param name="maxAge">The maximum allowed age.</param>
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
                }).ToList().ForEach(r => DeleteRaid(r));
            });
        }

        private static RaidHandle? CreateNewRaidHandle(DateTimeOffset offset)
        {
            //Catch any errors
            return Debug.Try<RaidHandle?>(() =>
            {
                //Find used raid IDs
                var ids = EnumerateRaids().Select(r => r.raid_id);

                //Create the range we will search through
                var searchSpace = Enumerable.Range(1, ids.MaxOrDefault() + 1);

                //Find the next available ID
                var nextID = searchSpace.Except(ids).Min();

                //Determine the timestamp
                var timestamp = offset.ToUnixTimeSeconds();

                //Determine the folder name
                var name = $"raid_{timestamp}_{nextID}";

                //Create directory for the raid
                Directory.CreateDirectory($"./raids/{name}/");

                //Return the handle to it
                return new RaidHandle
                {
                    full_name = name,
                    raid_id   = nextID,
                    timestamp = timestamp
                };
            }, null);
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

        private static string ResolveName(Entry e)
        {
            //Check if a name is available
            if (!string.IsNullOrEmpty(e.user_name))
            {
                //Just return the name
                return e.user_name;
            }

            //Query for the name
            return Bot.GetBotInstance().GetUserName(e.user_id.Value);
        }

        private static IEnumerable<Entry> GetRosterHistory(RaidHandle handle)
        {
            //Open the raid file
            using (FileStream fs = File.Open($"./raids/{handle.full_name}/raid.json", FileMode.Open, FileAccess.Read, FileShare.None))
            {
                //Get a UTF-8 encoded text stream
                StreamReader sr = new StreamReader(fs, Encoding.UTF8);

                //Deserialise the JSON and return the roster
                return JsonConvert.DeserializeObject<Raid>(sr.ReadToEnd()).roster;
            }
        }
    }
}
