using System;
using System.Collections.Generic;

using Newtonsoft.Json;

namespace DiscordBot.Wiki
{
    public sealed class QueryResult
    {
        public string                   batchcomplete { get; set; }
        public Query                    query         { get; set; }
    }

    public sealed class Query
    {
        public string[]                 pageids       { get; set; }
        [JsonConverter(typeof(DictionaryConverter))]
        public Dictionary<string, Page> pages         { get; set; }
    }

    public sealed class Page
    {
        public ulong                    pageid        { get; set; }
        public int                      ns            { get; set; }
        public string                   title         { get; set; }
        public string                   extract       { get; set; }
        public string                   fullurl       { get; set; }
    }

    public sealed class DictionaryConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                //Prepare dictionary
                Dictionary<string, Page> pages = null;

                //Read tokens
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.EndObject) break;

                    //Look for property name
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        //Read name
                        var value = reader.Value.ToString();

                        //Step to the next token
                        reader.Read();

                        //Read the page object
                        var page = serializer.Deserialize(reader, typeof(Page)) as Page;

                        //Insert into dictionary
                        if (pages == null) pages = new Dictionary<string, Page>();
                        pages.Add(value, page);
                    }
                }

                //Return dictionary
                return pages;
            }

            //Return null
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
