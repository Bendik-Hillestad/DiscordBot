using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

using DiscordBot.Core;
using DiscordBot.Utils;

namespace DiscordBot.Modules.Raid.GW2Raidar
{
    public static class Client
    {
        private static readonly string UPLOAD_URI = @"https://www.gw2raidar.com/api/v2/encounters/new";

        public static bool UploadLog(string path)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Check if the file exists
                Debug.Assert(File.Exists(path), $"{path} does not exist!");

                //Check that we have a token
                Debug.Assert(BotConfig.Config.Contains("gw2raidar_token"), "A GW2Raidar token is required!");

                //Get the token
                var token = (string)BotConfig.Config["gw2raidar_token"];

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
    }
}
