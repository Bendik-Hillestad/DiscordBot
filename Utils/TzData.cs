using System.IO;

using Newtonsoft.Json;

namespace DiscordBot.Utils
{
    public static class TzInfo
    {
        private struct TzData
        {
            public string zone; //e.g CET
            public int offset;  //e.g 60 (minutes)
        }

        private static readonly TzData defaultTimezone    = new TzData{ zone = "CET", offset = 60 };
        private static readonly string timezoneConfigPath = "tzdata.json";

        private static TzData CreateDefault()
        {
            try
            {
                string jsonText = JsonConvert.SerializeObject(defaultTimezone);

                using (StreamWriter fs = File.CreateText(timezoneConfigPath))
                {
                    fs.Write(jsonText);
                }
            }
            catch { }

            return defaultTimezone;
        }

        private static TzData LoadData()
        {
            try
            {
                if (File.Exists(timezoneConfigPath))
                {
                    string jsonText = File.ReadAllText(timezoneConfigPath);

                    return JsonConvert.DeserializeObject<TzData>(jsonText);
                }
            }
            catch { }

            return CreateDefault();
        }

        public static string GetCurrentZone()
        {
            return LoadData().zone;
        }

        public static int GetCurrentOffset()
        {
            return LoadData().offset;
        }
    }
}
