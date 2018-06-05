using System;
using System.IO;
using System.Net;
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

        public static UploadResponse UploadLog(Stream stream, string name)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Use the file as our content and mark with appropriate content type
                var file = new StreamContent(stream);
                file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                //Prepare our payload
                var content = new MultipartFormDataContent();
                content.Add(file, "file", name);

                //Get an HttpClient
                using (var http = new HttpClient())
                {
                    //Set a really long timeout because the server is slow.
                    http.Timeout = TimeSpan.FromMinutes(30);

                    //Prepare our container for the response
                    string response = null;

                    //Prepare our request message
                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, UPLOAD_URI)
                    {
                        Version = HttpVersion.Version10,
                        Content = content
                    };

                    //Send our request
                    using (var ret = http.SendAsync(requestMessage).GetAwaiter().GetResult())
                    {
                        //Check if it was successful
                        ret.EnsureSuccessStatusCode();

                        //Assign the response
                        response = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }

                    //Parse JSON and return the UploadResponse object
                    return JsonConvert.DeserializeObject<UploadResponse>(response);
                }
            }, null);
        }
    }
}
