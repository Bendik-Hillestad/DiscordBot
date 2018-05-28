using System.Collections.Generic;

namespace DiscordBot.Modules.Raid.GW2Raidar.Response
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

    public sealed class EncounterResponse
    {
        public int                   count   { get; set; }
        public string                next    { get; set; }
        public string                prev    { get; set; }
        public List<EncounterResult> results { get; set; }
    }

    public sealed class AreaResult
    {
        public int    id   { get; set; }
        public string name { get; set; }
    }

    public sealed class AreaResponse
    {
        public int              count   { get; set; }
        public string           next    { get; set; }
        public string           prev    { get; set; }
        public List<AreaResult> results { get; set; }
    }
}
