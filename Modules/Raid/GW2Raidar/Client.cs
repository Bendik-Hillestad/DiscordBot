using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

using DiscordBot.Core;
using DiscordBot.Utils;
using DiscordBot.Modules.Raid.GW2Raidar.Response;

using Newtonsoft.Json;

using static DiscordBot.Modules.Raid.GW2Raidar.Utility;

namespace DiscordBot.Modules.Raid.GW2Raidar
{
    public static class Utility
    {
        public static string TranslateBossID(int bossID)
        {
            switch (bossID)
            {
                //99CM
                case 17021:    return "MAMA (CM)";
                case 17028:    return "Siax (CM)";
                case 16948:    return "Ensolyss (CM)";

                //100CM
                case 17632:    return "Skorvald (CM)";
                case 17949:    return "Artsariiv (CM)";
                case 17759:    return "Arkk (CM)";

                //Wing 1
                case 15438:    return "Vale Guardian";
                case 15429:    return "Gorseval";
                case 15375:    return "Sabetha";

                //Wing 2
                case 16123:    return "Slothasor";
                case 16115:    return "Matthias";

                //Wing 3
                case 16235:    return "Keep Construct";
                case 16246: //FALLTHROUGH
                case 16286:    return "Xera";

                //Wing 4
                case 17194:    return "Cairn";
                case 17172:    return "Mursaat Overseer";
                case 17188:    return "Samarog";
                case 17154:    return "Deimos";

                //Wing 4 CM
                case 16728874: return "Cairn (CM)";
                case 16728852: return "Mursaat Overseer (CM)";
                case 16728868: return "Samarog (CM)";
                case 16728834: return "Deimos (CM)";

                //Wing 5
                case 19767:    return "Soulless Horror";
                case 19450:    return "Dhuum";

                //Wing 5 CM
                case 16731447: return "Soulless Horror (CM)";
                case 16731130: return "Dhuum (CM)";

                //Unknown
                default: return $"Unknown #{bossID}";
            }
        }

        public static Tuple<int, int> GetBossIDOrder(int bossID)
        {
            switch (bossID)
            {
                //99CM
                case 17021:    return new Tuple<int, int>(0, 0);
                case 17028:    return new Tuple<int, int>(0, 1);
                case 16948:    return new Tuple<int, int>(0, 2);

                //100CM
                case 17632:    return new Tuple<int, int>(1, 0);
                case 17949:    return new Tuple<int, int>(1, 1);
                case 17759:    return new Tuple<int, int>(1, 2);

                //Wing 1
                case 15438:    return new Tuple<int, int>(2, 0);
                case 15429:    return new Tuple<int, int>(2, 1);
                case 15375:    return new Tuple<int, int>(2, 2);

                //Wing 2
                case 16123:    return new Tuple<int, int>(3, 0);
                case 16115:    return new Tuple<int, int>(3, 1);

                //Wing 3
                case 16235:    return new Tuple<int, int>(4, 0);
                case 16246: //FALLTHROUGH
                case 16286:    return new Tuple<int, int>(4, 1);

                //Wing 4
                case 17194:    return new Tuple<int, int>(5, 0);
                case 17172:    return new Tuple<int, int>(5, 1);
                case 17188:    return new Tuple<int, int>(5, 2);
                case 17154:    return new Tuple<int, int>(5, 3);
                
                //Wing 4 CM
                case 16728874: return new Tuple<int, int>(6, 0);
                case 16728852: return new Tuple<int, int>(6, 1);
                case 16728868: return new Tuple<int, int>(6, 2);
                case 16728834: return new Tuple<int, int>(6, 3);

                //Wing 5
                case 19767:    return new Tuple<int, int>(7, 0);
                case 19450:    return new Tuple<int, int>(7, 1);

                //Wing 5 CM
                case 16731447: return new Tuple<int, int>(8, 0);
                case 16731130: return new Tuple<int, int>(8, 1);

                //Unknown
                default: return new Tuple<int, int>(Int32.MaxValue, Int32.MaxValue);
            }
        }
    }

    public static class Client
    {
        private static readonly string UPLOAD_URI          = @"https://www.gw2raidar.com/api/v2/encounters/new";
        private static readonly string LIST_ENCOUNTERS_URI = @"https://www.gw2raidar.com/api/v2/encounters?offset={0}&since={1}";
        private static readonly string LIST_AREAS_URI      = @"https://www.gw2raidar.com/api/v2/areas";

        public static bool UploadLog(string path, string tag)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Check if the file exists
                Debug.Assert(File.Exists(path), $"{path} does not exist!");

                //Get the token
                var token = GetToken();

                //Open the file
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete))
                {
                    //Use the file as our content and mark with appropriate content type
                    var file = new StreamContent(fs);
                    file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    //Prepare our payload
                    var content = new MultipartFormDataContent();
                    content.Add(file, "file", Path.GetFileName(path));
                    content.Add(new StringContent(tag), "tags");

                    //Get a HttpClient
                    using (var http = new HttpClient())
                    {
                        //Set a fairly long timeout in case of slow internet
                        http.Timeout = TimeSpan.FromMinutes(10);

                        //Prepare our request message
                        var requestMessage = new HttpRequestMessage(HttpMethod.Put, UPLOAD_URI)
                        {
                            Version = HttpVersion.Version10,
                            Content = content
                        };

                        //Put our token in the header
                        http.DefaultRequestHeaders.Add("Authorization", $"Token {token}");

                        //Send our file with a PUT request
                        var ret = http.SendAsync(requestMessage).GetAwaiter().GetResult();

                        //Check if it was successful
                        ret.EnsureSuccessStatusCode();
                    }
                }
            });
        }

        public static bool FindEncounters(ref Dictionary<string, EncounterResult> bossDictionary, string tag)
        {
            //Make a local copy because of lambda
            var bosses = new Dictionary<string, EncounterResult>(bossDictionary);

            //Catch any errors
            bool e = Debug.Try(() =>
            {
                //Get the token
                var token = GetToken();

                //Format our initial request
                var next = string.Format(LIST_ENCOUNTERS_URI, 0, 0);

                //Grab as many encounters as we can until we get enough
                do
                {
                    //Send our request
                    var resp = SendRequest<EncounterResponse>(token, next);

                    //Break if the results are empty
                    if (resp?.results == null || resp.results.Count == 0) break;
                    
                    //Filter out the results that do not contain our tag
                    var filtered = resp.results.Where(r => r.tags.Contains(tag));

                    //Go through the results
                    filtered.ToList().ForEach(r =>
                    {
                        //Translate the boss ID
                        var name = TranslateBossID(r.area_id);

                        //Check if the name matches something we want
                        if (bosses.ContainsKey(name) && bosses[name] == null)
                        {
                            //Store the result
                            bosses[name] = r;
                        }
                    });

                    //Exit if we have no empty values
                    if (bosses.Count(kv => kv.Value == null) == 0) break;

                    //Get the next uri
                    next = resp.next;
                } while (next != null);
            });

            //Store the result and return with success code
            bossDictionary = bosses;
            return e;
        }

        public static List<EncounterResult> ListEncounters(int limit, DateTimeOffset since)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get the token
                var token = GetToken();

                //Get the number of seconds since the UNIX epoch
                var timestamp = since.ToUnixTimeSeconds();

                //Format our initial request
                var next = string.Format(LIST_ENCOUNTERS_URI, 0, timestamp);

                //Grab as many encounters as we can until we get enough
                var results = new List<EncounterResult>();
                do
                {
                    //Send our request
                    var resp = SendRequest<EncounterResponse>(token, next);

                    //Grab the results
                    results.AddRange(resp.results);

                    //Check if we got enough results
                    if (results.Count >= limit) break;

                    //Get the next uri
                    next = resp.next;
                } while (next != null);

                //Return just enough elements
                return results.Take(limit).ToList();
            }, new List<EncounterResult>());
        }

        public static List<AreaResult> ListAreas()
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get the token
                var token = GetToken();

                //Prepare our initial request
                var next = LIST_AREAS_URI;

                //Grab as many areas as we can until we got all of them
                var results = new List<AreaResult>();
                do
                {
                    //Send our request
                    var resp = SendRequest<AreaResponse>(token, next);

                    //Grab the results
                    results.AddRange(resp.results);

                    //Get the next uri
                    next = resp.next;
                } while (next != null);

                //Return our results
                return results;
            }, new List<AreaResult>());
        }

        private static T SendRequest<T>(string token, string uri)
        {
            //Get a HttpClient
            using (var http = new HttpClient())
            {
                //Put our token in the header
                http.DefaultRequestHeaders.Add("Authorization", $"Token {token}");

                //Send the request
                var ret = http.GetAsync(uri).GetAwaiter().GetResult();

                //Check if it was successful
                ret.EnsureSuccessStatusCode();

                //Read the json text
                var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                //Parse JSON and return the response object
                return JsonConvert.DeserializeObject<T>(jsonText);
            }
        }

        private static string GetToken()
        {
            //Check that we have a token
            Debug.Assert(BotConfig.Config.Contains("gw2raidar_token"), "A GW2Raidar token is required!");

            //Get the token
            return (string)BotConfig.Config["gw2raidar_token"];
        }
    }
}
