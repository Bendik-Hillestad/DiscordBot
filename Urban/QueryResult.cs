namespace DiscordBot.Urban
{
    public sealed class QueryResult
    {
        public string   result_type  { get; set; }
        public Result[] list         { get; set; }
    }

    public sealed class Result
    {
        public string   definition   { get; set; }
        public string   permalink    { get; set; }
        public int      thumbs_up    { get; set; }
        public int      thumbs_down  { get; set; }
        public string   author       { get; set; }
        public string   word         { get; set; }
        public int      defid        { get; set; }
        public string   current_vote { get; set; }
        public string   example      { get; set; }
    }
}
