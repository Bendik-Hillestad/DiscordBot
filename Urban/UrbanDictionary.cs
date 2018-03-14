using System;
using System.Net.Http;
using System.Web;

using Newtonsoft.Json;

using DiscordBot.Utils;

namespace DiscordBot.Urban
{
    public static class UrbanDictionary
    {
        private static readonly string DEFINITION_QUERY_URI = @"https://api.urbandictionary.com/v0/define?term={0}";

        public static QueryResult GetDefinitions(string searchTerm)
        {
            //Encode the query
            string encodedQuery = HttpUtility.UrlEncode(searchTerm);

            //Format our request Uri
            string requestUri   = string.Format
            (
                DEFINITION_QUERY_URI, encodedQuery
            );

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

                    //Parse JSON and return QueryResult object
                    return JsonConvert.DeserializeObject<QueryResult>(jsonText);
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

            //Return null
            return null;
        }
    }
}
