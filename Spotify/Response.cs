namespace DiscordBot.ST
{
    public sealed class Response
    {
        public string    name    { get; set; }
        public TrackInfo tracks  { get; set; }
    }

    public sealed class TrackInfo
    {
        public Item[]    items   { get; set; }
    }

    public sealed class Item
    {
        public Track     track   { get; set; }
    }

    public sealed class Track
    {
        public Album     album   { get; set; }
        public string    name    { get; set; }
    }

    public sealed class Album
    {
        public Artist[]  artists { get; set; }
    }

    public sealed class Artist
    {
        public string    name    { get; set; }
    }
}
