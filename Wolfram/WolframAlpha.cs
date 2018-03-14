using System;
using System.Net.Http;
using System.Web;

using Newtonsoft.Json;

using DiscordBot.Utils;

namespace DiscordBot.Wolfram
{
    public static class WolframAlpha
    {
        private static readonly string SIMPLE_QUERY_URI = @"https://api.wolframalpha.com/v1/result?i={0}&appid={1}";
        private static readonly string FULL_QUERY_URI   = @"https://api.wolframalpha.com/v2/query?input={0}" +
                                                          @"&format=plaintext&output=JSON&units=metric&appid={1}";

        public static string SimpleQuery(string queryString, string appID)
        {
            //Encode the query string
            string encodedQuery = HttpUtility.UrlEncode(queryString);

            //Format our request Uri
            string requestUri = string.Format(SIMPLE_QUERY_URI, encodedQuery, appID);

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

                    //Return the text
                    return ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();
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
            return "Wolfram|Alpha could not answer your question.";
        }

        public static QueryResult FullQuery(string queryString, string appID)
        {
            //Encode the query string
            string encodedQuery = HttpUtility.UrlEncode(queryString);

            //Format our request Uri
            string requestUri = string.Format(FULL_QUERY_URI, encodedQuery, appID);

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
                    return (JsonConvert.DeserializeObject<QueryResponse>(jsonText)).queryresult;
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
