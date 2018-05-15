using System.IO;
using System.Text;

using DiscordBot.Utils;

using Newtonsoft.Json;

namespace DiscordBot.Core
{
    public class BotConfig
    {
        public string discord_bot_token     { get; private set; }
        public ulong  discord_owner_id      { get; private set; }
        public string spotify_client_id     { get; private set; }
        public string spotify_client_secret { get; private set; }
        public string youtube_api_key       { get; private set; }

        public static BotConfig Config { get; } = ReadConfig();

        private BotConfig()
        {}

        private static BotConfig ReadConfig()
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Open the config file
                using (FileStream fs = File.Open("config.json", FileMode.Open, FileAccess.Read))
                {
                    //Get a UTF-8 encoded text stream
                    StreamReader sr = new StreamReader(fs, Encoding.UTF8);

                    //Deserialise the JSON and return the configuration
                    return JsonConvert.DeserializeObject<BotConfig>(sr.ReadToEnd());
                }
            }, null);
        }
    }
}
