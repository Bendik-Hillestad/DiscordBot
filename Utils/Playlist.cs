namespace DiscordBot.Utils
{
    public interface IPlaylist
    {
        string Title { get; }

        void  SetOffset(int offset);
        Song? GetNext  ();
    }
}
