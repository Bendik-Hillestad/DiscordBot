using System;

using Discord;

using DiscordBot.Commands;
using DiscordBot.Utils;

namespace DiscordBot.Modules.Core
{
    public partial class CoreModule : CommandModule<CoreModule>
    {
        public override string ModuleName => "Misc";

        public override void OnInit()
        { }

        [Command("time")]
        public void time(Context ctx)
        {
            //Get UTC+0
            var utcNow = DateTimeOffset.UtcNow;

            //Get the current timezone identifier
            var timezone = TzInfo.GetCurrentZone();

            //Get the current offset
            var offset = TzInfo.GetCurrentOffset();

            //Get the local time
            var now = utcNow.AddMinutes(offset);

            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);

            //Add times
            var embed = builder.AddInlineField( "Servertime:", $"{utcNow:HH:mm}")
                               .AddInlineField($"{timezone}:", $"{now:HH:mm}")
                               .WithFooter    ( "Local time:").WithTimestamp(utcNow);

            //Send the message
            ctx.message.Channel.SendMessageAsync("", false, embed).GetAwaiter().GetResult();
        }

        public override string HelpMessage(Context ctx)
        {
            throw new NotImplementedException();
        }
    }
}
