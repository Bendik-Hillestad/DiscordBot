using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Net.Http;
using System.IO;

namespace DiscordBot.Utils
{
    public static class Utility
    {
        static readonly string[] UNICODE6_EMOJI_REPLACEMENT =
        {
            "🌊", ":ocean:",
            "😍", ":heart_eyes:",
            "✋", ":raised_hand:",
            "🙌", ":raised_hands:",
            "🚲", ":bike:",
            "🌎", ":earth_americas:",
            "👌", ":ok_hand:",
            "👳", ":man_with_turban:",
            "👍", ":thumbsup:",
            "💦", ":sweat_drops:",
            "💯", ":100:"
        };

        public static string ReplaceEmojies(string s)
        {
            //Iterate over emoji list
            for (int i = 0; i < UNICODE6_EMOJI_REPLACEMENT.Length; i += 2)
            {
                //Replace emoji
                s = s.Replace(UNICODE6_EMOJI_REPLACEMENT[i], UNICODE6_EMOJI_REPLACEMENT[i + 1]);
            }

            //Return string
            return s;
        }

        public static int SkipWhitespace(string s, int offset)
        {
            //Check that we're not past the string
            if (offset < s.Length)
            {
                //Iterate through the string until we hit something that isn't whitespace
                int i = offset;
                for (; i < s.Length; i++)
                {
                    if (!char.IsWhiteSpace(s, i)) break;
                }

                //Return index
                return i;
            }

            //No skipping
            return offset;
        }

        public static int[] GetNumbers(string s)
        {
            //Find numbers
            MatchCollection matches = Regex.Matches(s, @"-?\d+");

            //Check if there are no matches
            if (matches.Count == 0) return null;

            //Prepare array
            int[] nums = new int[matches.Count];
            int n = 0;

            //Iterate over matches
            foreach (Match item in matches)
            {
                //Cast to int and insert
                nums[n++] = int.Parse(item.Value);
            }

            //Return numbers
            return nums;
        }

        public static SortedSet<Tuple<int, int>> GetClockTimes(string s)
        {
            //Find clock times
            var matches = Regex.Matches(s, "(2[0-3]|[01]?[0-9]):([0-5][0-9])");

            //Check if there are no matches
            if (matches.Count == 0) return null;

            //Prepare set
            SortedSet<Tuple<int, int>> times = new SortedSet<Tuple<int, int>>();

            //Iterate over matches
            foreach (Match item in matches)
            {
                //Cast to int and insert
                times.Add
                (
                    new Tuple<int, int>
                    (
                        int.Parse(item.Groups[1].Value),
                        int.Parse(item.Groups[2].Value)
                    )
                );
            }

            //Return times
            return times;
        }

        public static string RenderLuaObjects(object[] objs)
        {
            //Prepare string
            string ret = "";

            //Iterate over objects
            for (int i = 0; i < objs.Length; i++)
            {
                //Get objects
                var obj = objs[i];

                //Check for null
                if (obj == null)
                {
                    //Just write nil
                    ret += "nil";
                }
                else
                {
                    //Get the type as a string
                    var type = obj.GetType().ToString();

                    //Switch based on type
                    switch (type)
                    {
                        case "System.String":
                        {
                            //Show "" if string is empty
                            ret += string.IsNullOrWhiteSpace(obj as System.String) ? "\"\"" : obj.ToString();
                        } break;

                        default:
                        {
                            //Use the default way
                            ret += obj.ToString();
                        } break;
                    }
                }

                //Separate with comma
                if (i < objs.Length - 1) ret += ", ";
            }

            //Return result
            return ret;
        }

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

        public static string FormatTime(int hours, int minutes)
        {
            string time = "";
            if (hours > 0) //Add hours
            {
                if (hours == 1) time += "1 hour";
                else time += hours + " hours";

                if (minutes > 0) time += " and ";
            }
            if (minutes > 0) //Add minutes
            {
                if (minutes == 1) time += "1 minute";
                else time += minutes + " minutes";
            }

            //Return formatted time
            return time;
        }

        public static string Indent(string text, string indent = "\t")
        {
            //Split into lines
            string[] lines = Regex.Split(text, @"\r\n|\n|\r");

            //Iterate over lines and add indentation
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = indent + lines[i];
            }

            //Combine lines and normalise line endings
            return string.Join("\n", lines);
        }

        public static string RemoveSentences(string text, int maxLen)
        {
            //Walk backwards until we hit a period within our max length
            int i = Math.Min(text.Length, maxLen) - 1;
            while (text[i] != '.' && i > 0) i--;

            //Return substring
            return text.Substring(0, i);
        }

        public static string Prettify(string text, string indent = "")
        {
            //Define the length to split at
            const int splitLen = 80;

            //Make sure we're actually handling single lines
            string[] lines = Regex.Split(text, @"\r\n|\n|\r");
            if (lines.Length > 1)
            {
                //Process the lines individually
                string output = Prettify(lines[0].Trim(), indent);
                for (int i = 1; i < lines.Length; i++)
                {
                    output += "\n" + Prettify(lines[i].Trim(), indent);
                }

                //Return prettified text
                return output;
            }

            //Check if string is longer than the split length
            if (text.Length > splitLen)
            {
                //Get amount of indentation
                if (string.IsNullOrEmpty(indent))
                {
                    indent = "";
                    for (int i = 0; i < text.Length && char.IsWhiteSpace(text, i); i++) indent += text[i];
                }

                //Jump to the desired split point and walk back until we find whitespace
                int splitPoint = splitLen;
                while (!char.IsWhiteSpace(text, splitPoint) && splitPoint >= indent.Length) splitPoint--;

                //Check that we found a split point
                if (splitPoint > indent.Length)
                {
                    //Split the string and recursively process the tail
                    return indent + text.Substring(0, splitPoint).Trim() + "\n" + Prettify(indent + text.Substring(splitPoint).Trim());
                }

                //In the highly unusual case where we didn't find one walking backwards, just force a split
                return indent + text.Substring(0, splitLen).Trim() + "-\n" + Prettify(indent + text.Substring(splitLen).Trim());
            }

            //Just return the text as it is + indent if one is provided
            return indent + text;
        }

        public static string FollowRedirect(string url)
        {
            //Setup a handler so we can catch a redirect
            var handler = new HttpClientHandler()
            {
                AllowAutoRedirect = false
            };

            //Start our HttpClient
            using (var http = new HttpClient(handler))
            {
                //Send the request
                var ret = http.GetAsync(url).GetAwaiter().GetResult();

                //Check for a location header
                var loc = ret.Headers.Location?.AbsoluteUri;

                //Check if it's valid
                if (!string.IsNullOrWhiteSpace(loc))
                {
                    //Return redirect
                    return loc;
                }

                //Return original
                return url;
            }
        }

        public static void WithRetry(Func<int, bool> f, int maxRetries)
        {
            //Loop until we succeed or hit the retry cap
            for (int i = 0; i < maxRetries; i++)
            {
                //Execute function and return if successful
                if (f(i)) return;
            }

            //Throw error
            throw new Exception($"Max retries exceeded!");
        }

        public static string[] GetLines(string str)
        {
            //Prepare the regex to match LF or RFLF line endings
            Regex regex = new Regex("\r\n|\n");

            //Split the string with the regex
            return regex.Split(str);
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
            return Path.Combine(Path.GetTempPath(), "DiscordBot\\");
        }

        public static ulong RandomUInt64()
        {
            //Get a random number generator
            Random r = new Random();

            //Create buffer for the bytes
            byte[] buf = new byte[8];

            //Fill the buffer
            r.NextBytes(buf);

            //Return a 64-bit unsigned integer
            return BitConverter.ToUInt64(buf, 0);
        }
    }
}
