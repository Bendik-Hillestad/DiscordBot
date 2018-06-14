using System.Collections.Generic;

namespace DiscordBot.Modules.Raid.DPSReport.Response
{
    public sealed class Player
    {
        public string display_name   { get; set; }
        public string character_name { get; set; }
        public int    profession     { get; set; }
        public int    elite_spec     { get; set; }
    }

    public sealed class EVTCInfo
    {
        public string type    { get; set; }
        public string version { get; set; }
        public int    bossId  { get; set; }
    }

    public sealed class EVTCMetaData
    {
        public Dictionary<string, Player> players { get; set; }
        public EVTCInfo                   evtc    { get; set; }
    }

    public sealed class UploadResponse
    {
        public string       id        { get; set; }
        public string       permalink { get; set; }
        public string       userToken { get; set; }
        public string       generator { get; set; }
        public EVTCMetaData metadata  { get; set; }
        public string       error     { get; set; }
    }
}
