using System;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Web;

using Newtonsoft.Json;

using DiscordBot.Utils;

namespace DiscordBot.Wiki
{
    public static class Wikipedia
    {
        private static readonly string INTRO_EXTRACT_QUERY_URI =
                                                          @"https://en.wikipedia.org/w/api.php?format=json&action=query" +
                                                          @"&prop=extracts&indexpageids=&exintro=&explaintext=&titles={0}";

        private static readonly string INTRO_EXTRACT_QUERY_WITH_REDIRECT_URI =
                                                          @"https://en.wikipedia.org/w/api.php?format=json&action=query" +
                                                          @"&prop=extracts&indexpageids=&exintro=&explaintext=&redirects=1" +
                                                          @"&titles={0}";

        private static readonly string TITLE_EXTRACT_QUERY_URI =
                                                          @"https://en.wikipedia.org/w/api.php?format=json&action=query" +
                                                          @"&prop=info&inprop=url&pageids={0}";

        public static string GetAbstract(string title, bool isEncoded, bool allowRedirect)
        {
            //Grab the intro
            var queryResult = GetIntro(title, isEncoded, allowRedirect);

            //Check if it was successful
            if (queryResult != null && queryResult.query != null)
            {
                //Get the page ids
                string[] pageids = queryResult.query.pageids;

                //Just grab the first ID
                string id = (pageids != null) ? pageids[0] : "-1";

                //Check that the pageid is not -1
                if (id != "-1")
                {
                    //Get the matching page
                    var page = queryResult.query.pages[id];

                    //TODO: handle cases with '<text> may also refer to:'

                    //Check that it has text
                    if (!string.IsNullOrEmpty(page.extract))
                    {
                        //Check if it's just "<title> may refer to:"
                        var regex = @"^(.+?)\s+" + Regex.Escape(@"may refer to:");
                        if (Regex.IsMatch(page.extract, regex))
                        {
                            //Extract the title part and just return that + link to wiki page
                            return "\"" + Regex.Match(page.extract, regex).Groups[1].Value + "\" can have multiple meanings.\n" +
                                   "See <" + (GetURL(page.pageid) ?? ("https://en.wikipedia.org/?curid=" + page.pageid)) + ">" +
                                   " for the full list of categories.";

                        }

                        //Check for new lines
                        if (Regex.IsMatch(page.extract, @"\r\n|\n|\r"))
                        {
                            //Split and extract the first paragraph
                            var paragraph = Regex.Split(page.extract, @"\r\n|\n|\r")[0];

                            //Return the paragraph + url
                            return paragraph + "\n<" + (GetURL(page.pageid) ?? ("https://en.wikipedia.org/?curid=" + page.pageid)) + ">";
                        }

                        //Use the whole thing + url
                        return page.extract + "\n<" + (GetURL(page.pageid) ?? ("https://en.wikipedia.org/?curid=" + page.pageid)) + ">";
                    }
                }
            }

            //Return null
            return null;
        }

        public static QueryResult GetIntro(string title, bool isEncoded, bool allowRedirect)
        {
            //Encode the query string if needed
            string encodedQuery = isEncoded ? title : HttpUtility.UrlEncode(title);

            //Format our request Uri
            string requestUri   = string.Format
            (
                allowRedirect ?
                INTRO_EXTRACT_QUERY_WITH_REDIRECT_URI :
                INTRO_EXTRACT_QUERY_URI,
                encodedQuery
            );

            //Catch any errors
            try
            {
                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Wikipedia API requires a custom user-agent with contact info, so add that
                    http.DefaultRequestHeaders.Add("User-Agent", "DiscordBot/1.0 (bendik.hillestad+discordbot@gmail.com)");

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

        public static string GetURL(ulong pageID)
        {
            //Format our request Uri
            string requestUri = string.Format
            (
                TITLE_EXTRACT_QUERY_URI, pageID
            );

            //Catch any errors
            try
            {
                //Get a HttpClient
                using (var http = new HttpClient())
                {
                    //Wikipedia API requires a custom user-agent with contact info, so add that
                    http.DefaultRequestHeaders.Add("User-Agent", "DiscordBot/1.0 (bendik.hillestad+discordbot@gmail.com)");

                    //Send the request
                    var ret = http.GetAsync(requestUri).GetAwaiter().GetResult();

                    //Check if it was successful
                    ret.EnsureSuccessStatusCode();

                    //Read the json text
                    var jsonText = ret.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    //Parse JSON as a QueryResult object
                    var queryResult = JsonConvert.DeserializeObject<QueryResult>(jsonText);

                    //Grab the full url
                    return queryResult.query.pages[pageID.ToString()].fullurl;
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
