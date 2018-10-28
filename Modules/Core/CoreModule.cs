using System;

using Discord;

using DiscordBot.Commands;

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

            //FIXME: Check for DST
            bool dst = false;//DateTime.Now.IsDaylightSavingTime();

            //Reliably get the actual time
            var now = utcNow.AddHours(dst ? 2 : 1);

            //Setup the embed builder
            var builder = new EmbedBuilder().WithColor(Color.Blue);

            //Add times
            var embed = builder.AddInlineField( "Servertime:",        $"{utcNow:HH:mm}")
                               .AddInlineField($"CE{(dst?"S":"")}T:", $"{now:HH:mm}")
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
