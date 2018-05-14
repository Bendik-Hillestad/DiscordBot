using System;
using System.Collections.Generic;
using System.Text;

using DiscordBot.Commands;
using DiscordBot.Core;

namespace DiscordBot.Modules.Raid
{
    public partial class RaidModule : CommandModule<RaidModule>
    {
        private void raid_create_impl(Context ctx, int day, int month, int year, int hours, int minutes, int offset, string description)
        {
            Bot.GetBotInstance().SendErrorMessage(ctx.message.Channel, "Test", $"Day: {day}\nMonth: {month}\nYear: {year}\nTime: {hours}:{minutes} UTC+{offset}\n");
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
