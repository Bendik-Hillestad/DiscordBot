using System.IO;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;

namespace DiscordBot.Raids
{
    public sealed class CompDescription
    {
        public string       Name   { get; set; }
        public List<string> Layout { get; set; }
    }

    public sealed class RaidConfig
    {
        public static RaidConfig DefaultConfig = new RaidConfig
        {
            Roles        = new List<string> { "MES", "HEAL", "DPS", "SLAVE" },
            Compositions = new List<CompDescription>
            {
                new CompDescription
                {
                    Name   = "DEFAULT",
                    Layout = new List<string>
                    {
                        "MES", "HEAL", "DPS", "DPS", "SLAVE",
                        "MES", "HEAL", "DPS", "DPS", "DPS"
                    }
                }
            }
        };

        public List<string>          Roles        { get; set; }
        public List<CompDescription> Compositions { get; set; }

        public static RaidConfig ReadConfig()
        {
            //Check if the config exists
            if (File.Exists("raid_config.json"))
            {
                //Read the file
                var text = File.ReadAllText("raid_config.json");

                //Deserialize and return
                return JsonConvert.DeserializeObject<RaidConfig>(text);
            }
            else
            {
                //Load a default composition
                return RaidConfig.DefaultConfig;
            }
        }

        public void AddCompDescription(CompDescription description)
        {
            //Make sure one with this name does not already exist
            this.Compositions.RemoveAll((other) => string.Equals(description.Name, other.Name));

            //Add composition description
            this.Compositions.Add(description);

            //Update role list
            this.Roles = this.Compositions
                             .Select   ((desc) => desc.Layout.Distinct())
                             .Aggregate((i, j) => i.Union(j))
                             .ToList   ();
        }

        public void SaveConfig()
        {
            //Open/Create the file
            using (FileStream fs = File.Open("raid_config.json", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
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

        public void GenerateSolver()
        {
            //TODO
        }
    }
}
