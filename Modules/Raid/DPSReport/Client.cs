using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

using DiscordBot.Utils;
using DiscordBot.Modules.Raid.DPSReport.Response;

using Newtonsoft.Json;

namespace DiscordBot.Modules.Raid.DPSReport
{
    public static class Client
    {
        //private static readonly string GET_TOKEN_URI = @"https://dps.report/getUserToken"; //OPTIONAL, maybe in the future?
        private static readonly string UPLOAD_URI    = @"https://dps.report/uploadContent?json=1&rotation_weap1=1&generator=rh";

        public static UploadResponse UploadLog(string path)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Check if the file exists
                Debug.Assert(File.Exists(path), $"{path} does not exist!");

                //Read the file
                var fs   = File.OpenRead(path);
                var file = new StreamContent(fs);
                file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                //Prepare our payload
                var content = new MultipartFormDataContent();
                content.Add(file, "file", Path.GetFileName(path));  

                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Send our file with a POST request
                    var ret = http.PostAsync(UPLOAD_URI, content).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();

                    //Make sure we have closed the file
                    fs.Close();

                    //Read the json text
                    var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    //Parse JSON and return the UploadResponse object
                    return JsonConvert.DeserializeObject<UploadResponse>(jsonText);
                }
            }, null);
        }
    }
}
