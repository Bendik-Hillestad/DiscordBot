using System;
using System.Runtime.Serialization;

namespace DiscordBot.Raids
{
    [DataContract]
    public sealed class Reminder
    {
        public ulong UserID  => this.userID;
        public int   Hours   => this.time.Hours;
        public int   Minutes => this.time.Minutes;

        public Reminder(ulong userID, int hours, int minutes)
        {
            this.userID = userID;
            this.time   = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
        }

        public DateTime GetNotifyTimeForDate(DateTime date)
        {
            return date.Subtract(this.time);
        }

        [DataMember(Name = "userID")]
        private ulong userID;
        [DataMember(Name = "time")]
        private TimeSpan time;
    }
}
