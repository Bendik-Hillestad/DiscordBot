using System;

using Newtonsoft.Json;

namespace DiscordBot.Wolfram
{
    //That feel when Wolfram|Alpha is retarded
    public sealed class WolframBugWorkaround : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            //Check if we actually got an array (LIKE WE FUCKING SHOULD WOLFRAM|ALPHA)
            if (reader.TokenType == JsonToken.StartArray)
            {
                //Just continue like normal
                return serializer.Deserialize(reader, objectType);
            }
            //Check if Wolfram|Alpha fucked up
            else if (reader.TokenType == JsonToken.StartObject)
            {
                //Get the element type
                Type elementType = objectType.GetElementType();

                //Create an array of that type
                var arr = Array.CreateInstance(elementType, 1);

                //Read the object
                var obj = serializer.Deserialize(reader, elementType);

                //Insert into array
                arr.SetValue(obj, 0);

                //Return array
                return arr;
            }

            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class QueryResponse
    {
        public QueryResult      queryresult   { get; set; }
    }

    public sealed class QueryResult
    {
        public bool             success       { get; set; }
        public bool             error         { get; set; }
        public bool             parsetimedout { get; set; }
        public int              numpods       { get; set; }
        [JsonConverter(typeof(WolframBugWorkaround))]
        public Pod[]            pods          { get; set; }
        [JsonConverter(typeof(WolframBugWorkaround))]
        public Assumption[]     assumptions   { get; set; }
        [JsonConverter(typeof(WolframBugWorkaround))]
        public Suggestion[]     didyoumeans   { get; set; }
        [JsonConverter(typeof(WolframBugWorkaround))]
        public Tip[]            tips          { get; set; }
    }

    public sealed class Pod
    {
        public string           title         { get; set; }
        public string           scanner       { get; set; }
        public string           id            { get; set; }
        public int              position      { get; set; }
        public bool             error         { get; set; }
        public int              numsubpods    { get; set; }
        [JsonConverter(typeof(WolframBugWorkaround))]
        public SubPod[]         subpods       { get; set; }
    }

    public sealed class SubPod
    {
        public string           title         { get; set; }
        public string           plaintext     { get; set; }
    }

    public sealed class Assumption
    {
        public string           type          { get; set; }
        public string           word          { get; set; }
        public string           template      { get; set; }
        public string           desc          { get; set; }
        public int              count         { get; set; }
        [JsonConverter(typeof(WolframBugWorkaround))]
        public Value[]          values        { get; set; }
    }

    public sealed class Value
    {
        public string           name          { get; set; }
        public string           desc          { get; set; }
        public string           input         { get; set; }
    }

    public sealed class Suggestion
    {
        public string           score         { get; set; }
        public string           level         { get; set; }
        public string           val           { get; set; }
    }

    public sealed class Tip
    {
        public string           text          { get; set; }
    }
}
