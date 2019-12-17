using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using Newtonsoft.Json;
using DiscordBot.Utils;
using System.Text.RegularExpressions;

#nullable enable

namespace DiscordBot.Raids
{
    internal sealed class RaidConfig
    {
        private static readonly RaidConfig DefaultConfig = new RaidConfig
        {
            compositions = new Dictionary<string, string[]>
            {
                {
                    "DEFAULT",
                    new string[]
                    {
                        "CHRONO", "HEALER", "DPS", "DPS", "SLAVE",
                        "CHRONO", "HEALER", "DPS", "DPS", "DPS"
                    }
                },

                {
                    "DEIMOS",
                    new string[]
                    {
                        "CHRONO", "HEALER", "DPS", "SLAVE",
                        "CHRONO", "HEALER", "DPS", "DPS", "DPS",
                        "KITER"
                    }
                },

                {
                    "FIREBRIGADE",
                    new string[]
                    {
                        "CHRONO",     "ALACRIGADE", "SLAVE", "DPS", "DPS",
                        "QUICKBRAND", "HEALER",     "DPS",   "DPS", "DPS"
                    }
                }
            },

            aliases = new Dictionary<string, string>()
            {
                { "MES",   "CHRONO" },
                { "HEAL",  "HEALER" },
                { "DRUID", "HEALER" },
                { "QFB",   "QUICKBRAND" },
                { "AREN",  "ALACRIGADE" }
            }
        };

        public RaidConfig()
        {
            this.compositions = new Dictionary<string, string[]>();
            this.roles        = new HashSet<string>();
            this.aliases      = new Dictionary<string, string>();
        }


        public IReadOnlyDictionary<string, string[]> Compositions => this.compositions;

        [JsonIgnore]
        public IReadOnlyCollection<string> Roles => this.roles;

        public IReadOnlyDictionary<string, string> Aliases => this.aliases;

        public static RaidConfig ReadConfig()
        {
            RaidConfig? config;

            //Check if a config exists
            if (File.Exists("raid_config.json"))
            {
                //Read the file
                var text = File.ReadAllText("raid_config.json");

                //Deserialize it
                config = JsonConvert.DeserializeObject<RaidConfig>(text);
            }
            else
            {
                //Load the default configuration
                config = RaidConfig.DefaultConfig;

                //Save the configuration
                Debug.Try(() => config.SaveConfig());
            }

            //Regenerate the roles
            config.RegenerateRoles();

            //Remove any unused aliases
            config.RemoveUnusedAliases();

            //Return it
            return config;
        }

        public void AddComposition(string name, string[] layout)
        {
            //Add the composition
            this.compositions.Add(name.ToUpper(), layout);

            //Update the roles
            this.RegenerateRoles();

            //Save the configuration
            Debug.Try(() => this.SaveConfig());
        }

        public void RemoveComposition(string name)
        {
            //Remove the composition
            this.compositions.Remove(name.ToUpper());

            //Update the roles
            this.RegenerateRoles();

            //Remove any unused aliases
            this.RemoveUnusedAliases();

            //Save the configuration
            Debug.Try(() => this.SaveConfig());
        }

        public bool HasRole(string role)
        {
            //Return whether the role exists or not
            return this.roles.Contains(role);
        }

        public bool HasAlias(string key)
        {
            //Return whether the key exists or not
            return this.aliases.ContainsKey(key.ToUpper());
        }

        public void AddAlias(string key, string value)
        {
            //Add the alias
            this.aliases.Add(key.ToUpper(), value.ToUpper());

            //Save the configuration
            Debug.Try(() => this.SaveConfig());
        }

        public void UpdateAlias(string key, string value)
        {
            //Update the alias
            this.aliases[key.ToUpper()] = value.ToUpper();

            //Save the configuration
            Debug.Try(() => this.SaveConfig());
        }

        public void RemoveAlias(string key)
        {
            //Remove the alias
            this.aliases.Remove(key.ToUpper());

            //Save the configuration
            Debug.Try(() => this.SaveConfig());
        }

        public string[] MatchRoles(string input)
        {
            //Create our regex
            var regex = this.roles.Union(this.aliases.Keys)
                                  .OrderByDescending(str => str.Length)
                                  .Aggregate((l, r) => $"{l}|{r}");

            //Get the matches
            return Regex.Matches (input, regex, RegexOptions.IgnoreCase)
                        .Select  (m => m.Value.ToUpper())
                        .Select  (str => this.aliases.ContainsKey(str) ? this.aliases[str] : str)
                        .Distinct().ToArray();
        }

        private void SaveConfig()
        {
            //Open/Create the file
            using (FileStream fs = File.Open("raid_config.json", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    //Generate JSON text
                    var json = JsonConvert.SerializeObject(this, Formatting.Indented);

                    //Write to the file
                    sw.Write(json);
                    sw.Flush();
                }
            }
        }

        private void RegenerateRoles()
        {
            this.roles.Clear();

            //Go through each composition
            foreach (ReadOnlySpan<string> comp in this.Compositions.Values)
            {
                //Go through each role in the comp
                foreach (string role in comp)
                {
                    //Add it to the hash set to get all the unique roles
                    this.roles.Add(role);
                }
            }
        }

        private void RemoveUnusedAliases()
        {
            List<string> unused = new List<string>();

            //Go through each alias
            foreach (var kv in this.aliases)
            {
                //Check if the corresponding role is not used
                if (!this.roles.Contains(kv.Value))
                {
                    //Add the alias to the list
                    unused.Add(kv.Key);
                }
            }

            //Go through the unused aliases
            foreach (string str in unused)
            {
                //Remove it from our dictionary
                this.aliases.Remove(str);
            }
        }

        private Dictionary<string, string[]> compositions;
        private HashSet<string> roles;
        private Dictionary<string, string> aliases;
    }
}
