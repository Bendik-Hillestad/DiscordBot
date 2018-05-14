using System;
using System.Collections.Generic;
using System.Text;

using DiscordBot.Commands;
using DiscordBot.Core;
using DiscordBot.Raids;
using DiscordBot.Utils;

namespace DiscordBot.Modules.Raid
{
    public partial class RaidModule : CommandModule<RaidModule>
    {
        private void raid_create_impl(Context ctx, int day, int month, int year, int hours, int minutes, int offset, string description)
        {
            //Get the date
            var date = new DateTimeOffset(year, month, day, hours, minutes, 0, new TimeSpan(offset, 0, 0));

            //Create the raid
            var handle = RaidManager.CreateRaid(ctx.message.Author.Id, date, description).Value;

            //Return success
            Bot.GetBotInstance().SendSuccessMessage(ctx.message.Channel, "Success", $"Raid has been created (ID: {handle.raid_id}).\nTime: {DateTool.GetPrettyDate(date.ToUnixTimeSeconds())}");
        }

        private void raid_delete_impl(Context ctx, int id)
        {

        }

        private void raid_roster_impl(Context ctx, int id, string roles)
        {

        }

        private void raid_list_impl(Context ctx)
        {

        }

        private void raid_join_impl(Context ctx, int id, string roles)
        {

        }

        private void raid_leave_impl(Context ctx, int id)
        {

        }

        private void raid_add_impl(Context ctx, int id, string name, string roles)
        {

        }

        private void raid_kick_impl(Context ctx, int id, string name)
        {

        }

        private void raid_make_comp_impl(Context ctx, string name, int id)
        {

        }

        private void raid_create_comp_impl(Context ctx, string name, string roles)
        {

        }

        private void raid_help_impl(Context ctx)
        {

        }
    }
}
