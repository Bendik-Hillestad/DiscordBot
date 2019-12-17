using System;
using System.Text.RegularExpressions;
using System.IO;

namespace DiscordBot.Utils
{
    public static class Utility
    {
        public static string PadNum(int val)
        {
            if (val < 10) return "0" + val;
            else return val.ToString();
        }

        public static string GetOrdinal(int value)
        {
            //Special case for 11, 12 and 13
            if (value == 11 || value == 12 || value == 13)
            {
                return "th";
            }

            //Switch on the right-most digit
            switch (value % 10)
            {
                case 1:  return "st";
                case 2:  return "nd";
                case 3:  return "rd";
                default: return "th";
            }
        }

        public static string GetMonth(int mo)
        {
            switch (mo)
            {
                case 1:  return "January";
                case 2:  return "February";
                case 3:  return "March";
                case 4:  return "April";
                case 5:  return "May";
                case 6:  return "June";
                case 7:  return "July";
                case 8:  return "August";
                case 9:  return "September";
                case 10: return "October";
                case 11: return "November";
                case 12: return "December";
                default: return "Invalid month";
            }
        }

        public static string RenderTimezone(int timezone)
        {
            if (timezone >= 0) return "UTC+" + timezone;
            else               return "UTC"  + timezone;
        }

        public static string GetTempDirectory()
        {
            //Check if we're on Linux
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                //Use the ramdisk as temporary directory
                return @"/dev/shm/DiscordBot/";
            }

            //Get the temporary path from the OS
            return Path.Combine(Path.GetTempPath(), "DiscordBot" + Path.DirectorySeparatorChar);
        }
    }
}
