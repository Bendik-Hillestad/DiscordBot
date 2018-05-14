using System;
using System.Collections.Generic;
using System.Text;

using DiscordBot.Commands;

namespace DiscordBot.Modules
{
    public class MusicModule : CommandModule<MusicModule>
    {
        public override string ModuleName => nameof(MusicModule);

        [Command("music join")]
        public void music_join(Context ctx)
        {

        }

        [Command("music leave")]
        public void music_leave(Context ctx)
        {

        }

        [Command("music play {}")]
        public void music_play(Context ctx, [RegexParameter(@"[\S\s]+")] string song)
        {

        }

        [Command("music skip")]
        public void music_skip(Context ctx)
        {

        }

        [Command("music queue")]
        public void music_queue(Context ctx)
        {

        }

        [Command("music np")]
        public void music_np(Context ctx)
        {

        }

        public override string HelpMessage(Context ctx)
        {
            throw new NotImplementedException();
        }
    }
}
