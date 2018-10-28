namespace DiscordBot.Modules.Music.Utility
{
    public interface IPlaylist
    {
        string Title { get; }

        void  SetOffset(int offset);
        //Song? GetNext  ();
    }
}
