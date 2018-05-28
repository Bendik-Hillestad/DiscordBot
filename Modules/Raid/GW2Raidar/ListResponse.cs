using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Modules.Raid.GW2Raidar
{
    public sealed class EncounterResult
    {
        public string url_id      { get; set; }
        public long   started_at  { get; set; }
        public int    area_id     { get; set; }
        public int?   category_id { get; set; }
        public string tags        { get; set; }
        public bool   success     { get; set; }
    }

    public sealed class ListResponse
    {
        public int                   count   { get; set; }
        public string                next    { get; set; }
        public string                prev    { get; set; }
        public List<EncounterResult> results { get; set; }
    }
}
