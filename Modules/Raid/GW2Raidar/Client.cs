using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

using DiscordBot.Core;
using DiscordBot.Utils;
using DiscordBot.Modules.Raid.GW2Raidar.Response;

using Newtonsoft.Json;

namespace DiscordBot.Modules.Raid.GW2Raidar
{
    public static class Client
    {
        private static readonly string UPLOAD_URI          = @"https://www.gw2raidar.com/api/v2/encounters/new";
        private static readonly string LIST_ENCOUNTERS_URI = @"https://www.gw2raidar.com/api/v2/encounters?offset={0}&since={1}";
        private static readonly string LIST_AREAS_URI      = @"https://www.gw2raidar.com/api/v2/areas";

        public static bool UploadLog(string path)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Check if the file exists
                Debug.Assert(File.Exists(path), $"{path} does not exist!");

                //Get the token
                var token = GetToken();

                //Read the file
                var file = new StreamContent(File.OpenRead(path));
                file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                //Prepare our payload
                var content = new MultipartFormDataContent();
                content.Add(file, "file", Path.GetFileName(path));  

                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Put our token in the header
                    http.DefaultRequestHeaders.Add("Authorization", $"Token {token}");

                    //Send our file with a PUT request
                    var ret = http.PutAsync(UPLOAD_URI, content).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();
                }
            });
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
