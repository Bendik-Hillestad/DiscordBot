namespace DiscordBot.YT
{
    public sealed class Response
    {
        public string         kind                 { get; set; }
        public string         etag                 { get; set; }
        public string         nextPageToken        { get; set; }
        public string         regionCode           { get; set; }
        public PageInfo       pageInfo             { get; set; }
        public SearchResult[] items                { get; set; }
    }

    public sealed class PageInfo
    {
        public int            totalResults         { get; set; }
        public int            resultsPerPage       { get; set; }
    }

    public sealed class SearchResult
    {
        public string         kind                 { get; set; }
        public string         etag                 { get; set; }
        public ID             id                   { get; set; }
        public Snippet        snippet              { get; set; }
    }

    public sealed class ID
    {
        public string         kind                 { get; set; }
        public string         videoId              { get; set; }
        public string         channelId            { get; set; }
        public string         playlistId           { get; set; }
    }

    public sealed class Snippet
    {
        public string         publishedAt          { get; set; }
        public string         channelId            { get; set; }
        public string         title                { get; set; }
        public string         description          { get; set; }
        public string         channelTitle         { get; set; }
        public string         liveBroadcastContent { get; set; }
    }
}
