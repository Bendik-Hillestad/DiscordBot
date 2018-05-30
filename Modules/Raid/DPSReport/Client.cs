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

        public static UploadResponse UploadLog(string path)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Check if the file exists
                Debug.Assert(File.Exists(path), $"{path} does not exist!");

                //Open the file
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Delete, 4096, FileOptions.DeleteOnClose))
                {
                    //Use the file as our content and mark with appropriate content type
                    var file = new StreamContent(fs);
                    file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    //Prepare our payload
                    var content = new MultipartFormDataContent();
                    content.Add(file, "file", Path.GetFileName(path));

                    //Get an HttpClient
                    using (var http = new HttpClient())
                    {
                        //Set an infinite timeout because the server is slow. TODO: Should probably force it to close after 10+ minutes
                        http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;

                        //Prepare our container for the response
                        string response = null;

                        //Try to upload the log
                        Utility.WithRetry(i => Debug.Try(() =>
                        {
                            //Prepare our request message
                            var requestMessage = new HttpRequestMessage(HttpMethod.Post, UPLOAD_URI)
                            {
                                Version = HttpVersion.Version10,
                                Content = content
                            };

                            //Send our request
                            using (var tmp = http.SendAsync(requestMessage).GetAwaiter().GetResult())
                            {
                                //Check if it was successful
                                tmp.EnsureSuccessStatusCode();

                                //Assign the response
                                response = tmp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                            }  
                        }, severity: LOG_LEVEL.WARNING), 3);

                        //Parse JSON and return the UploadResponse object
                        return JsonConvert.DeserializeObject<UploadResponse>(response);
                    }
                }
            }, null);
        }
    }
}
