using System;
using System.Collections.Generic;
using System.Text;

using DiscordBot.Commands;

namespace DiscordBot.Modules
{
    public class RaidModule : CommandModule<RaidModule>
    {
        public override string ModuleName => nameof(RaidModule);

        [Command("raid create {}/{} {}:{} UTC{} {}")]
        public void raid_create(Context ctx, uint day, uint month, uint hours, uint minutes, int offset, [RegexParameter(@"[\S\s]+")] string description)
        {

        }

        [Command("raid create {} {}:{} UTC{} {}")]
        public void raid_create(Context ctx, [RegexParameter(@"today|tomorrow")] string day, uint hours, uint minutes, int offset, [RegexParameter(@"[\S\s]+")] string description)
        {

        }

        [Command("raid delete {}")]
        public void raid_delete(Context ctx, uint id)
        {

        }

        [Command("raid roster {} {}")]
        public void raid_roster(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid roster {}")]
        public void raid_roster(Context ctx, uint id)
        {

        }

        [Command("raid list")]
        public void raid_list(Context ctx)
        {

        }

        [Command("raid join {} {}")]
        public void raid_join(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid join {}")]
        public void raid_join(Context ctx, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid leave {}")]
        public void raid_leave(Context ctx, uint id)
        {

        }

        [Command("raid leave")]
        public void raid_leave(Context ctx)
        {

        }

        [Command("raid add {} {}:{}")]
        public void raid_add(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid add {}|{}")]
        public void raid_add(Context ctx, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid kick {} {}")]
        public void raid_kick(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string name)
        {

        }

        [Command("raid make comp {} {}")]
        public void raid_make_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name, uint id)
        {

        }

        [Command("raid make comp {}")]
        public void raid_make_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name)
        {

        }

        [Command("raid make comp")]
        public void raid_make_comp(Context ctx)
        {

        }

        [Command("raid create comp {} {}")]
        public void raid_create_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {

        }

        [Command("raid help")]
        public void raid_help(Context ctx)
        {

        }

        public override string HelpMessage(Context ctx)
        {
            throw new NotImplementedException();
        }
    }
}
