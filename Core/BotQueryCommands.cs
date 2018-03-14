using System;
using System.Text.RegularExpressions;

using Discord.WebSocket;

using DiscordBot.DDG;
using DiscordBot.Wolfram;
using DiscordBot.Wiki;
using DiscordBot.Urban;

using Utility = DiscordBot.Utils.Utility;

namespace DiscordBot.Core
{
    public sealed partial class Bot
    {
        [CommandInit]
        private void ConstructQueryCommands()
        {
            //Try to activate WolframAlpha API
            if (this.config.ContainsKey("WAAppID"))
            {
                this.wolframAppID = this.config["WAAppID"];
            }

            //Register our Query command category + commands
            this.commandCategories.Add
            (
                new CommandCategory(null, null, null)
                .RegisterCommand
                (
                    new Command
                    (
                        "query", this, "CmdQuery", "CmdQueryHelp",
                        "query text", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "q", this, "CmdQuery", "CmdQueryHelp",
                        "query text", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "wolfram", this, "CmdWolframSimple", "CmdWolframHelp",
                        "simple",     @"simple(?:$|\s)",
                        "query text", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "wolfram", this, "CmdWolfram", "CmdWolframHelp",
                        "query text", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "wa", this, "CmdWolframSimple", "CmdWolframHelp",
                        "simple",     @"simple(?:$|\s)",
                        "query text", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "wa", this, "CmdWolfram", "CmdWolframHelp",
                        "query text", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "wiki", this, "CmdWiki", "CmdWikiHelp",
                        "query text", @"(.+?)$"
                    )
                )
                .RegisterCommand
                (
                    new Command
                    (
                        "ub", this, "CmdUrban", "CmdUrbanHelp",
                        "query text", @"(.+?)$"
                    )
                )
            );
        }

        private string CmdQuery(SocketUserMessage _, string query)
        {
            //Catch any errors
            try
            {
                //Send request to DuckDuckGo
                var result = DuckDuckGo.Query(query);

                //Check for redirect
                if (!string.IsNullOrWhiteSpace(result.Redirect))
                {
                    //Follow the redirect
                    var redirect = Utility.FollowRedirect(result.Redirect);

                    //Setup regex to match wiki pages
                    var wikiRegex = Regex.Escape(@"https://en.wikipedia.org/wiki/") + @"(.+)";

                    //Check if the url got resolved to a wiki page
                    if (Regex.IsMatch(redirect, wikiRegex))
                    {
                        //Get the title
                        var title = Regex.Match(redirect, wikiRegex).Groups[1].Value;

                        //Get the abstract
                        var str = Wikipedia.GetAbstract(title, true, true);

                        //Check if it's not null
                        if (!string.IsNullOrWhiteSpace(str))
                        {
                            //Return abstract + redirect
                            return str + "\n" + "<" + redirect + ">";
                        }
                    }

                    //Return redirect
                    return "<" + redirect + ">";
                }
                else
                {
                    //Check for article or instant answer
                    if (!string.IsNullOrWhiteSpace(result.Heading) || !string.IsNullOrWhiteSpace(result.Answer))
                    {
                        //Prepare answer
                        string ans = "";

                        //Add instant answer
                        if (!string.IsNullOrWhiteSpace(result.Answer))
                            ans += result.Answer + "\n\n";

                        //Add definition
                        if (!string.IsNullOrWhiteSpace(result.Definition))
                            ans += result.Definition + "\n<" + result.DefinitionURL + ">\n\n";

                        //Add abstract text
                        if (!string.IsNullOrWhiteSpace(result.AbstractText))
                        {
                            ans += result.AbstractText + "\n";
                        }
                        //Check if there is a URL and DDG simply failed to extract abstract
                        else if (!string.IsNullOrWhiteSpace(result.AbstractURL))
                        {
                            //Setup regex to match wiki pages
                            var wikiRegex = Regex.Escape(@"https://en.wikipedia.org/wiki/") + @"(.+)";

                            //Check if the URL is a wiki page
                            if (Regex.IsMatch(result.AbstractURL, wikiRegex))
                            {
                                //Get the title
                                var title = Regex.Match(result.AbstractURL, wikiRegex).Groups[1].Value;

                                //Extract the abstract ourselves
                                ans += Wikipedia.GetAbstract(title, true, true) + "\n";
                            }
                        }

                        //Add abstract URL
                        if (!string.IsNullOrWhiteSpace(result.AbstractURL))
                            ans += "<" + result.AbstractURL + ">\n\n";

                        //Add first external link
                        if (!string.IsNullOrWhiteSpace(((result.Results?.Count ?? 0) > 0) ? result.Results[0].Text : null))
                            ans +=  "Relevant matches:\n" +
                                    "\t" + result.Results[0].Text  + " - <" + result.Results[0].FirstURL + ">\n";

                        //Add second external link
                        if (!string.IsNullOrWhiteSpace(((result.Results?.Count ?? 0) > 1) ? result.Results[1].Text : null))
                            ans += "\t" + result.Results[1].Text + " - <" + result.Results[1].FirstURL + ">\n";

                        //Add third external link
                        if (!string.IsNullOrWhiteSpace(((result.Results?.Count ?? 0) > 2) ? result.Results[2].Text : null))
                            ans += "\t" + result.Results[2].Text + " - <" + result.Results[2].FirstURL + ">\n";

                        //Add first related topic
                        if (!string.IsNullOrWhiteSpace(((result.RelatedTopics?.Count ?? 0) > 0) ? result.RelatedTopics[0].Text : null))
                            ans += "Related topics:\n" +
                                    "\t" + result.RelatedTopics[0].Text + "\n";

                        //Add second related topic
                        if (!string.IsNullOrWhiteSpace(((result.RelatedTopics?.Count ?? 0) > 1) ? result.RelatedTopics[1].Text : null))
                            ans += "\t" + result.RelatedTopics[1].Text + "\n";

                        //Add third related topic
                        if (!string.IsNullOrWhiteSpace(((result.RelatedTopics?.Count ?? 0) > 2) ? result.RelatedTopics[2].Text : null))
                            ans += "\t" + result.RelatedTopics[2].Text + "\n";

                        //Add image
                        if (!string.IsNullOrWhiteSpace(result.Image))
                            ans += "\nImage: " + result.Image;

                        //Return trimmed answer
                        return ans.Trim();
                    }
                }

                //No result
                return "No result";
            }
            catch (Exception ex)
            {
                //Return error
                return "An error occurred!\nError: " + ex.Message;
            }
        }

        private string CmdQueryHelp(SocketUserMessage _)
        {
            return "The syntax for this command is \"$query [search text]\".\n" +
                   "You can also use the short version \"$q [search text]\".\n" +
                   "For example: \"$q Guild Wars 2\".";
        }

        private string CmdWolframSimple(SocketUserMessage _, string query)
        {
            //Check if wolfram has been activated
            if (!string.IsNullOrWhiteSpace(this.wolframAppID))
            {
                //Send request to WolframAlpha
                return WolframAlpha.SimpleQuery(query, this.wolframAppID);
            }

            //Return error
            return "The host of this bot has not provided an API key for WolframAlpha!";
        }

        private string CmdWolfram(SocketUserMessage _, string query)
        {
            //Check if wolfram has been activated
            if (!string.IsNullOrWhiteSpace(this.wolframAppID))
            {
                //Send query to WolframAlpha
                var queryResult = WolframAlpha.FullQuery(query, this.wolframAppID);
                if
                (
                     ( queryResult?.success       ?? false) &&
                    !( queryResult?.error         ?? true)  && 
                    !( queryResult?.parsetimedout ?? true)  &&
                     ((queryResult?.numpods       ?? 0) > 0)
                )
                {
                    //Prepare response
                    string interpretation = "";
                    string result         = "";
                    string weather        = "";
                    string forecast       = "";
                    string definition     = "";
                    string assumption     = "";

                    //Iterate over pods
                    for (int i = 0; i < queryResult.numpods; i++)
                    {
                        //Get pod
                        Pod pod = queryResult.pods[i];

                        //Skip if it has no subpods (The spec doesn't say if this can happen or not)
                        if (pod.numsubpods == 0) continue;

                        //Also skip if there was an error with this pod
                        if (pod.error) continue;

                        //Switch based on id
                        switch (pod.id)
                        {
                            case "Input":
                            {
                                //Prepare interpretation
                                string interps = "\t";

                                //Cleanup because Wolfram|Alpha does weird shit
                                var tmp = pod.subpods[0].plaintext.Split('\n');
                                for (int j = 0; j < tmp.Length; j++) tmp[j] = tmp[j].Trim();
                                interps += string.Join(" ", tmp);

                                //Add input interpretation
                                interpretation = pod.title + ":\n" + Utility.Prettify(interps.Replace('', '=')
                                                                                             .Replace('', 'i')
                                                                                             .Replace('', 'Δ')
                                                                                             .Replace('', 'e')
                                                                                             .Replace('', 'ℤ'));
                            } break;

                            case "BasicInformation:MovieData":
                            case "Cast:MovieData":
                            case "Basic:TelevisionProgramData":
                            case "Cast:TelevisionProgramData":
                            case "DecimalApproximation":
                            case "MinimalPolynomial":
                            case "AlternateForm":
                            case "IndefiniteIntegral":
                            case "RiemannSums":
                            case "DefiniteIntegralOverAHalfPeriod":
                            case "DefiniteIntegralMeanSquare":
                            case "SymbolicSolution":
                            case "DefinitionPod:MathWorldData":
                            case "Basic:DiseaseData":
                            case "Result":
                            {
                                //Iterate over subpods
                                bool title = false;
                                foreach (SubPod subpod in pod.subpods)
                                {
                                    //Skip if empty
                                    if (string.IsNullOrWhiteSpace(subpod.plaintext)) continue;

                                    //Check if title has been added
                                    if (!title)
                                    {
                                        title = true;
                                        result += pod.title + ": " + "\n";
                                    }
                                    
                                    //Add to result
                                    result += Utility.Prettify(subpod.plaintext.Replace('', '=')
                                                                               .Replace('', 'i')
                                                                               .Replace('', 'Δ')
                                                                               .Replace('', 'e')
                                                                               .Replace('', 'ℤ'), "\t") + "\n\n";
                                }     
                            } break;

                            case "Definition:WordData":
                            {
                                //Fetch definitions
                                string defs = "";
                                foreach (string line in pod.subpods[0].plaintext.Split('\n'))
                                    defs += "\n" + Utility.Prettify("\t" + line);

                                //Add definition
                                definition = pod.title + ":" + defs;
                            } break;

                            case "InstantaneousWeather:WeatherData":
                            {
                                //Fetch weather data
                                string data = "";
                                foreach (string line in pod.subpods[0].plaintext.Split('\n'))
                                    data += "\n" + Utility.Prettify("\t" + line);

                                //Add input interpretation
                                weather = pod.title + ":" + data;
                            } break;

                            case "WeatherForecast:WeatherData":
                            {
                                //Fetch weather data
                                string data = "";
                                for (int j = 0; j < pod.numsubpods; j++)
                                {
                                    //Get subpod
                                    SubPod subPod = pod.subpods[j];

                                    //Add data
                                    data += "\n\t" + subPod.title + ":\n" +
                                            Utility.Prettify(subPod.plaintext, "\t\t");
                                }

                                //Add input interpretation
                                forecast = pod.title + ":" + data;
                            } break;
                        }
                    }

                    //Look for assumptions
                    if ((queryResult.assumptions?.Length ?? 0) > 0)
                    {
                        //Grab the active assumptions
                        foreach (Assumption ass in queryResult.assumptions)
                        {
                            //Skip if no values (The spec doesn't say if this can happen or not)
                            if (ass.count == 0) continue;

                            //Skip more complicated assumptions
                            if (ass.type == "SubCategory" || ass.type == "Unit" || ass.type == "Clash")
                            {
                                if (string.IsNullOrWhiteSpace(assumption)) assumption = "Assuming:";
                                assumption += "\n" + Utility.Prettify("\t" + ass.values[0].desc);
                            }
                            else if (ass.type == "FormulaVariable")
                            {
                                if (string.IsNullOrWhiteSpace(assumption)) assumption = "Assuming:";
                                assumption += "\n" + Utility.Prettify("\t" + ass.desc + ": " + ass.values[0].desc);
                            }
                        }
                    }

                    //Reply with formatted response
                    return "\n" +
                            (
                                (!string.IsNullOrWhiteSpace(interpretation) ? interpretation.     Trim() : "") +
                                (!string.IsNullOrWhiteSpace(result)         ? "\n\n" + result.    Trim() : "") +
                                (!string.IsNullOrWhiteSpace(weather)        ? "\n\n" + weather.   Trim() : "") +
                                (!string.IsNullOrWhiteSpace(forecast)       ? "\n\n" + forecast.  Trim() : "") +
                                (!string.IsNullOrWhiteSpace(definition)     ? "\n\n" + definition.Trim() : "") +
                                (!string.IsNullOrWhiteSpace(assumption)     ? "\n\n" + assumption.Trim() : "")
                            ).TrimStart();
                }
                else if (queryResult?.parsetimedout ?? false)
                {
                    return "Wolfram|Alpha timed out.";
                }
                else if ((queryResult?.didyoumeans?.Length ?? 0) > 0)
                {
                    string suggestions = "";
                    foreach (Suggestion sug in queryResult.didyoumeans) suggestions += "\n\t" + sug.val;

                    return "Wolfram|Alpha did not understand your query.\nDid you mean:" + suggestions;
                }
                else if ((queryResult?.tips?.Length ?? 0) > 0)
                {
                    string tips = "";
                    foreach (Tip tip in queryResult.tips) tips += "\n\t" + tip.text;

                    return "Wolfram|Alpha did not understand your query.\nTips:" + tips;
                }
                else
                {
                    //Return error
                    return "Wolfram|Alpha encountered an unknown error handling your request.";
                }
            }

            //Return error
            return "The host of this bot has not provided an API key for WolframAlpha!";
        }

        private string CmdWolframHelp(SocketUserMessage _)
        {
            return "The syntax for this command is \"$wolfram [query text]\".\n" +
                   "You can also use the short version \"$wa [query text]\".\n" +
                   "For example: \"$wolfram 2 + 2\".\n" +
                   "Additionally, if you add the word 'simple' in front of your text " +
                   "you'll get a simplifed response.\n" +
                   "For example: \"$wa simple what is the airspeed velocity of an unladen swallow\"";
        }

        private string CmdWiki(SocketUserMessage _, string query)
        {
            //Get the abstract
            var str = Wikipedia.GetAbstract(query, false, true);

            //Check if it was successful
            if (!string.IsNullOrWhiteSpace(str))
            {
                //Return it
                return str;
            }

            //Return error
            return "Couldn't find a wiki page matching your query.";
        }

        private string CmdWikiHelp(SocketUserMessage _)
        {
            return "The syntax for this command is \"$wiki <query text>\".\n" +
                   "For example: \"$wiki einstein\".\n";
        }

        private string CmdUrban(SocketUserMessage _, string query)
        {
            //Get the definitions
            var defs = UrbanDictionary.GetDefinitions(query);

            //Check if it was successful
            if (!(defs?.result_type ?? "no_results").Equals("no_results") && ((defs?.list?.Length ?? 0) > 0))
            {
                //Grab the first result
                var result = defs.list[0];

                //Return the definition and example
                return "\n**" + result.word + "**:\n" +
                       "\t__Definition__:\n" + Utility.Prettify(result.definition, "\t\t") + "\n\n" +
                       "\t__Example__:\n" + Utility.Prettify(result.example, "\t\t");
            }   

            //Return error
            return "No results.";
        }

        private string CmdUrbanHelp(SocketUserMessage _)
        {
            return "The syntax for this command is \"$ub <search term>\".\n" +
                   "For example: \"$ub dank memes\".\n";
        }

        private string wolframAppID;
    }
}
