using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;

using DiscordBot.Core;
using DiscordBot.Modules.Raid.GW2Raidar.Response;
using DiscordBot.Utils;

using Newtonsoft.Json;

namespace DiscordBot.Modules.Raid.GW2Raidar
{
    public static class CredentialsManager
    {
        public struct Credentials
        {
            public DateTimeOffset maxAge;
            public string csrftoken;
            public string sessionid;
        }

        private static Credentials? cookie = null;

        public static Credentials GetCredentials()
        {
            //Check if we have cached credentials
            if (cookie.HasValue)
            {
                //Check that they have not expired
                if (DateTimeOffset.UtcNow < cookie.Value.maxAge)
                {
                    //Return the credentials
                    return cookie.Value;
                }
            }

            //Log in and save credentials
            cookie = DoLogin();

            //Return the credentials
            return cookie.Value;
        }

        private static Credentials? DoLogin()
        {
            //Catch any errors
            return Debug.Try<Credentials?>(() =>
            {
                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Prepare our request message
                    var requestMessage = new HttpRequestMessage(HttpMethod.Get, @"https://www.gw2raidar.com/")
                    {
                        Version = HttpVersion.Version11
                    };

                    //Send our request
                    var csrftoken        = "";
                    var completionOption = HttpCompletionOption.ResponseHeadersRead;
                    using (var ret = http.SendAsync(requestMessage, completionOption).GetAwaiter().GetResult())
                    {
                        //Check if it was successful
                        ret.EnsureSuccessStatusCode();

                        //Get the Set-Cookie response header
                        var header = ret.Headers.GetValues("Set-Cookie").First();

                        //Extract the token
                        var regex = new Regex(@"csrftoken\=(.+?)\;");
                        var match = regex.Match(header);
                        csrftoken = match.Groups[1].Value;
                    }

                    //Check that it's valid
                    Debug.Assert(!string.IsNullOrEmpty(csrftoken), "Couldn't retrieve initial CSRF token!");

                    //Prepare our login credentials
                    var content = new StringContent
                    (
                        (string)BotConfig.Config["raidar_credentials"],
                        Encoding.UTF8,
                        "application/x-www-form-urlencoded"
                    );

                    //Prepare our request message
                    requestMessage = new HttpRequestMessage(HttpMethod.Post, @"https://www.gw2raidar.com/login.json")
                    {
                        Version = HttpVersion.Version11,
                        Content = content
                    };

                    //Modify the headers we want to send
                    requestMessage.Headers.Add("Accept", "application/json");
                    requestMessage.Headers.Add("Origin", "https://www.gw2raidar.com");
                    requestMessage.Headers.Add("Referer", "https://www.gw2raidar.com/login");
                    requestMessage.Headers.Add("Cookie", $"csrftoken={csrftoken}");
                    requestMessage.Headers.Add("X-CSRFToken", csrftoken);

                    //Send our request
                    var maxAge = 0;
                    var sessionid = "";
                    using (var ret = http.SendAsync(requestMessage, completionOption).GetAwaiter().GetResult())
                    {
                        //Check if it was successful
                        ret.EnsureSuccessStatusCode();

                        //Get the Set-Cookie response header
                        var header = string.Join(";", ret.Headers.GetValues("Set-Cookie"));

                        //Extract the token
                        var regex = new Regex(@"csrftoken\=(.+?)\;", RegexOptions.IgnoreCase);
                        var match = regex.Match(header);
                        csrftoken = match.Groups[1].Value;

                        //Extract the sessionid and max age
                        regex     = new Regex(@"sessionid\=(.+?)\;.+?max\-age=(.+?)\;", RegexOptions.IgnoreCase);
                        match     = regex.Match(header);
                        sessionid = match.Groups[1].Value;
                        maxAge    = int.Parse(match.Groups[2].Value);
                    }

                    //Return the credentials
                    return new Credentials
                    {
                        maxAge    = DateTimeOffset.UtcNow.AddSeconds(maxAge),
                        csrftoken = csrftoken,
                        sessionid = sessionid
                    };
                }
            }, null);
        }
    }

    public class SneakyClient
    {
        private static readonly string UPLOAD_URI = @"https://www.gw2raidar.com/upload.json";
        private static readonly string POLL_URI   = @"https://www.gw2raidar.com/poll.json";

        public UploadResponse Upload(Stream stream, string name)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get the credentials
                var creds = CredentialsManager.GetCredentials();

                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Use the stream as our content and mark with appropriate content type
                    var file = new StreamContent(stream);
                    file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    //Prepare our payload
                    var content = new MultipartFormDataContent();
                    content.Add(file, "file", name);

                    //Prepare our request message
                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, UPLOAD_URI)
                    {
                        Version = HttpVersion.Version10,
                        Content = content
                    };

                    //Modify the headers we want to send
                    requestMessage.Headers.Add("Accept", "application/json");
                    requestMessage.Headers.Add("Origin", "https://www.gw2raidar.com");
                    requestMessage.Headers.Add("Referer", "https://www.gw2raidar.com/uploads");
                    requestMessage.Headers.Add("Cookie", $"csrftoken={creds.csrftoken};sessionid={creds.sessionid}");
                    requestMessage.Headers.Add("X-CSRFToken", creds.csrftoken);

                    //Send our request
                    var response = "";
                    using (var ret = http.SendAsync(requestMessage).GetAwaiter().GetResult())
                    {
                        //Check if it was successful
                        ret.EnsureSuccessStatusCode();

                        //Save the response
                        response = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }

                    //Parse JSON and return the UploadResponse object
                    return JsonConvert.DeserializeObject<UploadResponse>(response);
                }
            }, default);
        }

        public PollResponse Poll()
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get the credentials
                var creds = CredentialsManager.GetCredentials();

                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Prepare our payload
                    var content = default(StringContent);
                    if (this.lastid != 0)
                    {
                        content = new StringContent
                        (
                            "last_id=" + this.lastid,
                            Encoding.UTF8,
                            "application/x-www-form-urlencoded"
                        );
                    }

                    //Prepare our request message
                    var requestMessage = new HttpRequestMessage(HttpMethod.Post, POLL_URI)
                    {
                        Version = HttpVersion.Version10,
                        Content = content
                    };

                    //Modify the headers we want to send
                    requestMessage.Headers.Add("Accept", "application/json");
                    requestMessage.Headers.Add("Origin", "https://www.gw2raidar.com");
                    requestMessage.Headers.Add("Referer", "https://www.gw2raidar.com/uploads");
                    requestMessage.Headers.Add("Cookie", $"csrftoken={creds.csrftoken};sessionid={creds.sessionid}");
                    requestMessage.Headers.Add("X-CSRFToken", creds.csrftoken);

                    //Send our request
                    var response = "";
                    using (var ret = http.SendAsync(requestMessage).GetAwaiter().GetResult())
                    {
                        //Check if it was successful
                        ret.EnsureSuccessStatusCode();

                        //Save the response
                        response = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }

                    //Parse JSON as a PollResponse object
                    var obj = JsonConvert.DeserializeObject<PollResponse>(response);

                    //Store the last_id value
                    this.lastid = obj.last_id ?? 0;

                    //Return the parsed object
                    return obj;
                }
            }, default);
        }

        private int lastid = 0;
    }
}
