﻿using System;
using System.Linq;

using DiscordBot.Commands;
using DiscordBot.Raids;
using DiscordBot.Utils;

using Timer = System.Timers.Timer;

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

            //Create a timer to periodically clean up old raids
            Timer t = new Timer(60 * 60 * 1000);
            t.Elapsed += (s, e) =>
            {
                //Delete raids older than 5 hours
                RaidManager.CleanRaidFiles(new TimeSpan(5, 0, 0));
            };
            t.AutoReset = true;
            t.Start();
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
            var roles = string.Join(" ", this.raidConfig.Roles);

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

        [Command("raid aliases")]
        public void raid_aliases(Context ctx)
        {
            //Pass on to the implementation
            this.raid_aliases_impl(ctx);
        }

        [Command("raid aliases add {} => {}")]
        public void raid_aliases_add(Context ctx, [RegexParameter(@"\w+")] string key, [RegexParameter(@"\w+")] string value)
        {
            //Check that the role exists
            Precondition.Assert(this.raidConfig.HasRole(value.ToUpper()), "No role with that name.");

            //Pass on to the implementation
            this.raid_aliases_add_impl(ctx, key.ToUpper(), value.ToUpper());
        }

        [Command("raid aliases remove {}")]
        public void raid_aliases_remove(Context ctx, [RegexParameter(@"\w+")] string key)
        {
            //Check that the alias exists
            Precondition.Assert(this.raidConfig.HasAlias(key.ToUpper()), "No existing alias with that name.");

            //Pass on to the implementation
            this.raid_aliases_remove_impl(ctx, key.ToUpper());
        }

        [Command("raid roles")]
        public void raid_roles(Context ctx)
        {
            //Pass on to the implementation
            this.raid_roles_impl(ctx);
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
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID((int)id);

            //Make sure it exists
            Precondition.Assert(handle.HasValue, $"No raid with that id ({id}).");

            //Find the raider in the roster
            var player = RaidManager.FindRaider(handle.Value, ctx.message.Author.Id);

            //Check that the raider exists in the roster
            Precondition.Assert(player.HasValue, "You are not in the roster.");

            //Pass on to the implementation
            this.raid_leave_impl(ctx, handle.Value, player.Value);
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
            this.raid_add_impl(ctx, (int)id, name.Trim(), roles);
        }

        [Command("raid add {}:{}")]
        public void raid_add(Context ctx, [RegexParameter(@"[\S\s]+")] string name, [RegexParameter(@"[\S\s]+")] string roles)
        {
            //Get the next raid
            var handle = RaidManager.GetNextRaid();

            //Check if we have one
            Precondition.Assert(handle.HasValue, "No raids up.");

            //Pass on
            this.raid_add(ctx, (uint)handle.Value.raid_id, name, roles);
        }

        [Command("raid kick {} <@{}>")]
        public void raid_kick(Context ctx, uint id, ulong userID)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID((int)id);

            //Make sure it exists
            Precondition.Assert(handle.HasValue, $"No raid with that id ({id}).");

            //Find the raider that matches the ID
            var player = RaidManager.FindRaider(handle.Value, userID);

            //Check that the raider exists in the roster
            Precondition.Assert(player.HasValue, "That raider is not in the roster.");

            //Pass on to the implementation
            this.raid_kick_impl(ctx, handle.Value, player.Value);
        }

        [Command("raid kick {} {}")]
        public void raid_kick(Context ctx, uint id, [RegexParameter(@"[\S\s]+")] string name)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID((int)id);

            //Make sure it exists
            Precondition.Assert(handle.HasValue, $"No raid with that id ({id}).");

            //Find any raiders that match the name
            var players = RaidManager.FindRaiders(handle.Value, name);

            //Check that we got only one
            Precondition.Assert(players.Count > 0,   "No one with that name.");
            Precondition.Assert(players.Count == 1, $"More than one matches that name.");

            //Pass on to the implementation
            this.raid_kick_impl(ctx, handle.Value, players.First());
        }

        [Command("raid make comp {} {}")]
        public void raid_make_comp(Context ctx, [RegexParameter(@"[\S\s]+")] string name, uint id)
        {
            //Get a handle to the raid
            var handle = RaidManager.GetRaidFromID((int)id);

            //Make sure it exists
            Precondition.Assert(handle.HasValue, $"No raid with that id ({id}");

            //Check that the composition exists
            string[] layout ;
            Precondition.Assert
            (
                this.raidConfig.Compositions.TryGetValue(name.ToUpper(), out layout),
                "No comp with that name. These are the recognised comps: \n" +
                string.Join(", ", this.raidConfig.Compositions.Keys)
            );

            //Pass on to the implementation
            this.raid_make_comp_impl(ctx, handle.Value, layout);
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

        [Command("raid delete comp {}")]
        public void raid_delete_comp(Context ctx, [RegexParameter(@"[^ ]+")] string name)
        {
            //Check that they're not trying to delete the default comp
            Precondition.Assert
            (
                !string.Equals(name.ToUpper(), "DEFAULT"),
                "Cannot delete the default comp.\nOverwrite it instead."
            );

            //Get the index of the comp
            //var idx = this.raidConfig.GetCompIndex(name.ToUpper());
            bool exists = this.raidConfig.Compositions.ContainsKey(name.ToUpper());

            //Check that it exists
            Precondition.Assert
            (
                exists,
                "No comp with that name. These are the recognised comps: \n" +
                string.Join(", ", this.raidConfig.Compositions.Keys)
            );

            //Pass on to implementation
            this.raid_delete_comp_impl(ctx, name);
        }

        [Command("raid show comps")]
        public void raid_show_comps(Context ctx)
        {
            //Pass on to implementation
            this.raid_show_comps_impl(ctx);
        }

        [Command("raid list comps")]
        public void raid_list_comps(Context ctx)
        {
            //Pass on to implementation
            this.raid_show_comps_impl(ctx);
        }

        [Command("raid upload logs")]
        public void raid_upload_logs(Context ctx)
        {
            //Check that we got an attachment
            Precondition.Assert(ctx.message.Attachments.Count > 0, "Missing attachment!");

            //Get the attachment
            var attachment = ctx.message.Attachments.First();

            //Check that it's a zip file
            Precondition.Assert(attachment.Filename.EndsWith(".zip"), "Only .zip files are accepted!");

            //Pass on to the implementation
            this.raid_upload_logs_impl(ctx);
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
