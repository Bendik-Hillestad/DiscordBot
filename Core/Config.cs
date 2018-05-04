using System.IO;
using System.Text;

using DiscordBot.Utils;

using Newtonsoft.Json;

namespace DiscordBot.Core
{
    public struct Config
    {
        public string discord_bot_token     { get; set; }
        public ulong  discord_owner_id      { get; set; }
        public string spotify_client_id     { get; set; }
        public string spotify_client_secret { get; set; }
        public string youtube_api_key       { get; set; }

        public void WriteConfig()
        {
            //Open the config file
            using (FileStream fs = File.Open("config.json", FileMode.Create, FileAccess.Write))
            {
                //Get a UTF-8 encoded text stream
                StreamWriter sw = new StreamWriter(fs, Encoding.UTF8);

                //Deserialise the JSON and return the configuration
                sw.Write(JsonConvert.SerializeObject(this, Formatting.Indented));
                sw.Flush();
            }
        }

        public static Config? ReadConfig()
        {
            //Catch any errors
            return Debug.Try<Config?>(() =>
            {
                //Open the config file
                using (FileStream fs = File.Open("config.json", FileMode.Open, FileAccess.Read))
                {
                    //Get a UTF-8 encoded text stream
                    StreamReader sr = new StreamReader(fs, Encoding.UTF8);

                    //Deserialise the JSON and return the configuration
                    return JsonConvert.DeserializeObject<Config>(sr.ReadToEnd());
                }
            }, null);
        }
    }
}
