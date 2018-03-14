using System;
using System.Net.Http;
using System.Web;

using Newtonsoft.Json;

using DiscordBot.Utils;

namespace DiscordBot.DDG
{
    public static class DuckDuckGo
    {
        private static readonly string QUERY_URI = @"https://api.duckduckgo.com/?q={0}&format=json&pretty=0&no_redirect=1&no_html=1&skip_disambig=0";

        public static SearchResult Query(string queryString)
        {
            //Encode the query string
            string encodedQuery = HttpUtility.UrlEncode(queryString);

            //Format our request Uri
            string requestUri = string.Format(QUERY_URI, encodedQuery);

            //Catch any errors
            try
            {
                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Send the request
                    var ret = http.GetAsync(requestUri).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();

                    //Read the json text
                    var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    //Parse JSON and return SearchResult object
                    return JsonConvert.DeserializeObject<SearchResult>(jsonText);
                }
            }
            catch (AggregateException ae)
            {
                //Get errors
                ae.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                    //Mark as handled
                    return true;
                });
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message);
            }

            //Return failure
            return null;
        }
    }
}
