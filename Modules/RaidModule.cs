using System;
using System.Collections.Generic;
using System.Text;

using DiscordBot.Commands;

namespace DiscordBot.Modules
{
    [Module]
    public static class RaidModule
    {
        [Command("raid create {}/{}")]
        public static void raid_create(Context ctx, uint day, uint month)
        {

        }

        [Command("raid create {}")]
        public static void raid_create(Context ctx, [RegexParameter(@"today|tomorrow")] string day)
        {

        }

        [Command("raid delete {}")]
        public static void raid_delete(Context ctx, uint id)
        {

        }

        [Command("raid roster {} {}")]
        public static void raid_roster(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid roster {}")]
        public static void raid_roster(Context ctx, uint id)
        {

        }

        [Command("raid list")]
        public static void raid_list(Context ctx)
        {

        }

        [Command("raid join {} {}")]
        public static void raid_join(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid join {}")]
        public static void raid_join(Context ctx, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid leave {}")]
        public static void raid_leave(Context ctx, uint id)
        {

        }

        [Command("raid leave")]
        public static void raid_leave(Context ctx)
        {

        }

        [Command("raid add {} {}|{}")]
        public static void raid_add(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid add {}|{}")]
        public static void raid_add(Context ctx, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid kick {} {}")]
        public static void raid_kick(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string name)
        {

        }

        [Command("raid make comp {} {}")]
        public static void raid_make_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name, uint id)
        {

        }

        [Command("raid make comp {}")]
        public static void raid_make_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name)
        {

        }

        [Command("raid make comp")]
        public static void raid_make_comp(Context ctx)
        {

        }

        [Command("raid create comp {} {}")]
        public static void raid_create_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid help")]
        public static void raid_help(Context ctx)
        {

        }
    }
}
