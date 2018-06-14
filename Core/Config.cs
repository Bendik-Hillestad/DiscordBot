using System.IO;
using System.Text;
using System.Collections;

using DiscordBot.Utils;

using Newtonsoft.Json;

namespace DiscordBot.Core
{
    public static class BotConfig
    {
        public static Hashtable Config { get; } = ReadConfig();

        private static Hashtable ReadConfig()
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
                    return JsonConvert.DeserializeObject<Hashtable>(sr.ReadToEnd());
                }
            }, null);
        }
    }
}
