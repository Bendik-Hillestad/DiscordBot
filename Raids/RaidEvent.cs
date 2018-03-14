using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Linq;

namespace DiscordBot.Raids
{
    [DataContract]
    public sealed class RaidEvent : IComparable<RaidEvent>
    {
        public int ID
        {
            get { return this.id; }
        }

        public int Day
        {
            get { return this.day; }
        }

        public int Month
        {
            get { return this.month; }
        }

        public int Year
        {
            get { return this.year; }
        }

        public int Hour
        {
            get { return this.hour; }
        }

        public int Minute
        {
            get { return this.minute; }
        }

        public int Timezone
        {
            get { return this.timezone; }
        }

        public string Description
        {
            get { return this.desc; }
        }

        public ulong Owner
        {
            get { return this.ownerID; }
        }

        public List<Raider> Roster
        {
            get { return this.roster; }
        }

        public RaidEvent(int id, int day, int month, int year, int hour, int minute, int timezone, string desc, ulong ownerID)
        {
            this.id         = id;
            this.day        = day;
            this.month      = month;
            this.year       = year;
            this.hour       = hour;
            this.minute     = minute;
            this.timezone   = timezone;
            this.desc       = desc;
            this.ownerID    = ownerID;
            this.roster     = new List<Raider>();
        }

        public bool IsInGroup(Raider raider)
        {
            //Iterate over roster
            foreach (Raider r in this.roster)
            {
                //Check if the ID matches
                if (r.ID == raider.ID) return true;
            }

            //Return false
            return false;
        }

        public bool IsInGroup(ulong userID)
        {
            //Iterate over roster
            foreach (Raider r in this.roster)
            {
                //Check if the ID matches
                if (r.ID == userID) return true;
            }

            //Return false
            return false;
        }

        public void UpdateRaider(Raider raider)
        {
            //Iterate over roster
            for (int i = 0; i < this.roster.Count; i++)
            {
                //Check if the ID matches
                if (this.roster[i].ID == raider.ID)
                {
                    //Update values
                    this.roster[i].nick  = raider.nick;
                    this.roster[i].roles = raider.roles;
                }
            }
        }

        public void AddRaider(ulong userID, string nick, string[] roles)
        {
            //Create raider
            Raider r = new Raider(userID, nick, roles);

            //Check if already in the group
            if (IsInGroup(r))
            {
                //Just update our entry
                this.UpdateRaider(r);
            }
            else
            {
                //Add to roster
                this.roster.Add(r);
            }
        }

        public bool RemoveRaider(ulong userID)
        {
            //Iterate over raiders
            for (int i = 0; i < this.roster.Count; i++)
            {
                //Check if ID matches
                if (this.roster[i].ID == userID)
                {
                    //Remove from roster
                    this.roster.RemoveAt(i);

                    //Return success
                    return true;
                }
            }

            //Return failure
            return false;
        }

        public string RenderRoster(out int rosterCount)
        {
            //Check if there are any raiders
            if (this.roster.Count > 0)
            {
                //Prepare result
                string ret   = "";
                int    count = 0;

                //Iterate over roster
                foreach (Raider r in this.roster)
                {
                    //Append nick and roles
                    ret += $"{count + 1} - " + r.nick + " - " + string.Join(", ", r.roles) + "\n";
                    count++;
                }

                //Return result
                rosterCount = count;
                return ret;
            }

            //Return empty
            rosterCount = 0;
            return null;
        }

        public string RenderRoster(string[] filter, out int rosterCount)
        {
            //Check if there are any raiders
            if (this.roster.Count > 0)
            {
                //Prepare result
                string ret    = "";
                int    count  = 0;

                //Iterate over roster
                foreach (Raider r in this.roster)
                {
                    //Check if the raider has one of the roles we're looking for
                    string[] temp = r.roles.Intersect<string>(filter).ToArray();
                    if ((temp?.Length ?? 0) > 0)
                    {
                        //Append index, nick and roles
                        ret += $"{count + 1} - " + r.nick + " - " + string.Join(", ", r.roles) + "\n";
                        count++;
                    }
                }

                //Return result
                rosterCount = count;
                return ret;
            }

            //Return empty
            rosterCount = 0;
            return null;
        }

        int IComparable<RaidEvent>.CompareTo(RaidEvent other)
        {
            return (
                    (new DateTime(this.year, this.month, this.day, this.hour, this.minute, this.id, DateTimeKind.Utc)) -
                    (new TimeSpan(this.timezone, 0, 0))
                   )
                   .CompareTo
                   (
                    (new DateTime(other.year, other.month, other.day, other.hour, other.minute, other.id, DateTimeKind.Utc)) -
                    (new TimeSpan(other.timezone, 0, 0))
                   );
        }

        [DataMember(Name = "id")]
        private int          id;
        [DataMember(Name = "day")]
        private int          day;
        [DataMember(Name = "month")]
        private int          month;
        [DataMember(Name = "year")]
        private int          year;
        [DataMember(Name = "hour")]
        private int          hour;
        [DataMember(Name = "minute")]
        private int          minute;
        [DataMember(Name = "timezone")]
        private int          timezone;
        [DataMember(Name = "desc")]
        private string       desc;
        [DataMember(Name = "ownerID")]
        private ulong        ownerID;
        [DataMember(Name = "roster")]
        private List<Raider> roster;
    }
}
