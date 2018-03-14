using System.Collections.Generic;

namespace DiscordBot.DDG
{
    public sealed class SearchResult
    {
        public string               Abstract         { get; set; }
        public string               AbstractText     { get; set; }
        public string               AbstractSource   { get; set; }
        public string               AbstractURL      { get; set; }
        public string               Image            { get; set; }
        public string               Heading          { get; set; }

        public string               Answer           { get; set; }
        public string               AnswerType       { get; set; }

        public string               Definition       { get; set; }
        public string               DefinitionSource { get; set; }
        public string               DefinitionURL    { get; set; }

        public List<RelatedResults> RelatedTopics    { get; set; }
        public List<RelatedResults> Results          { get; set; }

        public string               Type             { get; set; }

        public string               Redirect         { get; set; }
    }

    public sealed class RelatedResults
    {
        public string Result   { get; set; }
        public string FirstURL { get; set; }
        public Icon   Icon     { get; set; }
        public string Text     { get; set; }
    }

    public sealed class Icon
    {
        public string URL    { get; set; }
        public string Height { get; set; }
        public string Width  { get; set; }
    }
}