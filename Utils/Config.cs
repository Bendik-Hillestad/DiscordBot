using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace DiscordBot.Utils
{
    public static class Config
    {
        private static readonly string CONFIG_FILE  =  "config.ini";
        private static readonly string CONFIG_REGEX = @"(\w+)\s*=\s*([^\s]+)";

        public static Dictionary<string, string> ReadConfig()
        {
            //Check if the config file exists
            if (File.Exists(CONFIG_FILE))
            {
                try
                {
                    //Open the config file
                    string str = null;
                    using (StreamReader sr = new StreamReader(File.OpenRead(CONFIG_FILE)))
                    {
                        //Read the contents of the file
                        str = sr.ReadToEnd();
                    }

                    //Create dictionary
                    var dict = new Dictionary<string, string>();

                    //Parse the string with our regex
                    foreach (Match m in Regex.Matches(str, CONFIG_REGEX))
                    {
                        //Save to dictionary
                        dict.Add(m.Groups[1].Value, m.Groups[2].Value);
                    }

                    //Return dictionary
                    return dict;
                }
                catch (Exception ex)
                {
                    //Print error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);
                }
            }
            else
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, "config.ini not found!");
            }

            //Return failure
            return null;
        }
    }
}
