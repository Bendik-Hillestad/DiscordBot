﻿using System;
using System.Linq;

using DiscordBot.Commands;
using DiscordBot.Raids;
using DiscordBot.Utils;
using DiscordBot.Core;

namespace DiscordBot.Modules.Raid
{
    public partial class RaidModule : CommandModule<RaidModule>
    {
        public override string ModuleName => nameof(RaidModule);

        public override void OnInit()
        {
            //Initialise the raid manager
            RaidManager.Initialise();

            //Load raid config
            this.raidConfig = RaidConfig.ReadConfig();

            //Compile
            this.raidConfig.GenerateSolverLibrary();
        }

        [Command("raid create {}/{} {}:{} UTC{} {}")]
        public void raid_create(Context ctx, uint day, uint month, uint hours, uint minutes, int offset, [RegexParameter(@"[\S\s]+")] string description)
        {
            //Derive the year
            var year = DateTool.GetDefaultYear((int)day, (int)month);

            //Validate our inputs
            Precondition.Assert(DateTool.IsValidDate((int)day, (int)month, year), "Invalid date!");
            Precondition.Assert(hours < 24, "Invalid hours!");
            Precondition.Assert(minutes < 60, "Invalid minutes!");
            Precondition.Assert(Math.Abs(offset) <= 12, "Invalid offset!");

            //Pass on to the implementation
            this.raid_create_impl(ctx, (int)day, (int)month, year, (int)hours, (int)minutes, offset, description);
        }

        [Command("raid create {} {}:{} UTC{} {}")]
        public void raid_create(Context ctx, [RegexParameter(@"today|tomorrow")] string day, uint hours, uint minutes, int offset, [RegexParameter(@"[\S\s]+")] string description)
        {
            //Get the current date
            var date = DateTimeOffset.UtcNow.ToOffset(new TimeSpan(offset, 0, 0));
            if (day == "tomorrow") date += new TimeSpan(1, 0, 0, 0);

            //Pass on
            this.raid_create(ctx, (uint)date.Day, (uint)date.Month, hours, minutes, offset, description);
        }

        [Command("raid create")]
        public void raid_create(Context ctx)
        {
            //Get the default timezone offset
            var offset = DateTool.GetDefaultTimezone();

            //Pass on
            this.raid_create(ctx, "today", 20, 00, offset, "W1-4");
        }

        [Command("raid delete {}")]
        public void raid_delete(Context ctx, uint id)
        {
            //Determine if a raid with this ID exists
            var exists = RaidManager.EnumerateRaids().Any(r => r.raid_id == id);
            Precondition.Assert(exists, $"No raid with that id ({id}).");

            //Pass on to the implementation
            this.raid_delete_impl(ctx, (int)id);
        }

        [Command("raid delete")]
        public void raid_delete(Context ctx)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if we have one
            Precondition.Assert(handle.HasValue, "No raids up.");

            //Pass on
            this.raid_delete(ctx, (uint)handle.Value.raid_id);
        }

        [Command("raid roster {} {}")]
        public void raid_roster(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string roles)
        {
            //Determine if a raid with this ID exists
            var exists = RaidManager.EnumerateRaids().Any(r => r.raid_id == id);
            Precondition.Assert(exists, $"No raid with that id ({id}).");

            //Pass on to the implementation
            this.raid_roster_impl(ctx, (int)id, roles);
        }

        [Command("raid roster {}")]
        public void raid_roster(Context ctx, uint id)
        {
            //Get all roles
            var roles = string.Join(" ", this.raidConfig.GetAllRoles());

            //Pass on
            this.raid_roster(ctx, id, roles);
        }

        [Command("raid roster")]
        public void raid_roster(Context ctx)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if we have one
            Precondition.Assert(handle.HasValue, "No raids up.");

            //Pass on
            this.raid_roster(ctx, (uint)handle.Value.raid_id);
        }

        [Command("raid list")]
        public void raid_list(Context ctx)
        {
            //Pass on to the implementation
            this.raid_list_impl(ctx);
        }

        [Command("raid join {} {}")]
        public void raid_join(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string roles)
        {
            //Determine if a raid with this ID exists
            var exists = RaidManager.EnumerateRaids().Any(r => r.raid_id == id);
            Precondition.Assert(exists, $"No raid with that id ({id}).");

            //Pass on to the implementation
            this.raid_join_impl(ctx, (int)id, roles);
        }

        [Command("raid join {}")]
        public void raid_join(Context ctx, [RegexParameter(@"[\S\s]+")] string roles)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if we have one
            Precondition.Assert(handle.HasValue, "No raids up.");

            //Pass on
            this.raid_join(ctx, (uint)handle.Value.raid_id, roles);
        }

        [Command("raid leave {}")]
        public void raid_leave(Context ctx, uint id)
        {
            //Determine if a raid with this ID exists
            var exists = RaidManager.EnumerateRaids().Any(r => r.raid_id == id);
            Precondition.Assert(exists, $"No raid with that id ({id}).");

            //Pass on to the implementation
            this.raid_leave_impl(ctx, (int)id);
        }

        [Command("raid leave")]
        public void raid_leave(Context ctx)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if we have one
            Precondition.Assert(handle.HasValue, "No raids up.");

            //Pass on
            this.raid_leave(ctx, (uint)handle.Value.raid_id);
        }

        [Command("raid add {} {}:{}")]
        public void raid_add(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {
            //Determine if a raid with this ID exists
            var exists = RaidManager.EnumerateRaids().Any(r => r.raid_id == id);
            Precondition.Assert(exists, $"No raid with that id ({id}).");

            //Pass on to the implementation
            this.raid_add_impl(ctx, (int)id, name, roles);
        }

        [Command("raid add {}|{}")]
        public void raid_add(Context ctx, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if we have one
            Precondition.Assert(handle.HasValue, "No raids up.");

            //Pass on
            this.raid_add(ctx, (uint)handle.Value.raid_id, name, roles);
        }

        [Command("raid kick {} {}")]
        public void raid_kick(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string name)
        {
            //Determine if a raid with this ID exists
            var exists = RaidManager.EnumerateRaids().Any(r => r.raid_id == id);
            Precondition.Assert(exists, $"No raid with that id ({id}).");

            //Pass on to the implementation
            this.raid_kick_impl(ctx, (int)id, name);
        }

        [Command("raid make comp {} {}")]
        public void raid_make_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name, uint id)
        {
            //Determine if a raid with this ID exists
            var exists = RaidManager.EnumerateRaids().Any(r => r.raid_id == id);
            Precondition.Assert(exists, $"No raid with that id ({id}).");

            //Pass on to the implementation
            this.raid_make_comp_impl(ctx, name, (int)id);
        }

        [Command("raid make comp {}")]
        public void raid_make_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if we have one
            Precondition.Assert(handle.HasValue, "No raids up.");

            //Pass on
            this.raid_make_comp(ctx, name, (uint)handle.Value.raid_id);
        }

        [Command("raid make comp")]
        public void raid_make_comp(Context ctx)
        {
            //Pass on
            this.raid_make_comp(ctx, "DEFAULT");
        }

        [Command("raid create comp {} {}")]
        public void raid_create_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {
            //Pass on to implementation
            this.raid_create_comp_impl(ctx, name, roles);
        }

        [Command("raid help")]
        public void raid_help(Context ctx)
        {
            //Pass on to implementation
            this.raid_help_impl(ctx);
        }

        public override string HelpMessage(Context ctx)
        {
            throw new NotImplementedException();
        }

        private RaidConfig raidConfig;
    }
}