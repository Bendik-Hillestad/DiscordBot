using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml;

using DiscordBot.Utils;

namespace DiscordBot.Raids
{
    [DataContract]
    public sealed class RaidCalendar
    {
        public RaidCalendar()
        {
            this.raidEvents  = new SortedSet<RaidEvent>();
            this.idGenerator = new IDGenerator();
            this.reminders   = new List<Reminder>();
        }

        public int CreateRaid(int day, int month, int year, int hour, int minute, int timezone, string desc, ulong ownerID)
        {
            //Get a new ID
            int id = this.idGenerator.NewID();

            //Create event
            this.raidEvents.Add(new RaidEvent(id, day, month, year, hour, minute, timezone, desc, ownerID));

            //Return id
            return id;
        }

        public bool DeleteRaid(int raidID, ulong userID)
        {
            //Iterate over raid events
            foreach (RaidEvent e in this.raidEvents)
            {
                //Check if id matches
                if (e.ID == raidID)
                {
                    //Check if the user is the owner
                    if (e.Owner == userID)
                    {
                        //Remove it
                        this.raidEvents.Remove(e);

                        //Release ID
                        this.idGenerator.ReleaseID(raidID);

                        //Return success
                        return true;
                    }

                    //No need to keep searching
                    break;
                }
            }

            //Return failure
            return false;
        }

        public string ListRaidEvents()
        {
            //Check if there is at least one event
            if (this.raidEvents.Count > 0)
            {
                //Prepare string
                string ret = "";

                //Iterate over raid events
                foreach (RaidEvent e in this.raidEvents)
                {
                    //Append to string
                    ret += string.Format("[ID: {0}] - {1}\n    Time: {2} of {3} {4} at {5}:{6} {7}\n",
                           e.ID, e.Description, e.Day + Utility.GetOrdinal(e.Day), Utility.GetMonth(e.Month),
                           e.Year, Utility.PadNum(e.Hour), Utility.PadNum(e.Minute), Utility.RenderTimezone(e.Timezone));
                }

                //Return with result
                return ret;
            }

            //Return empty
            return "";
        }

        public bool ListRaiders(int raidID, out string roster, out int rosterCount)
        {
            //Iterate over raid events
            foreach (RaidEvent e in this.raidEvents)
            {
                //Check if id matches
                if (e.ID == raidID)
                {
                    //Render the roster to the output string
                    roster = e.RenderRoster(out rosterCount);

                    //Return success
                    return true;
                }
            }

            //Return failure
            roster      = null;
            rosterCount = 0;
            return false;
        }

        public bool ListRaiders(int raidID, string[] filter, out string roster, out int rosterCount)
        {
            //Iterate over raid events
            foreach (RaidEvent e in this.raidEvents)
            {
                //Check if id matches
                if (e.ID == raidID)
                {
                    //Render the roster to the output string
                    roster = e.RenderRoster(filter, out rosterCount);

                    //Return success
                    return true;
                }
            }

            //Return failure
            roster      = null;
            rosterCount = 0;
            return false;
        }

        public bool AddRaider(int raidID, ulong userID, string nick, string[] roles)
        {
            //Iterate over raid events
            foreach (RaidEvent e in this.raidEvents)
            {
                //Check if id matches
                if (e.ID == raidID)
                {
                    //Add to roster
                    e.AddRaider(userID, nick, roles);

                    //Return success
                    return true;
                }
            }

            //Return failure
            return false;
        }

        public bool RemoveRaider(int raidID, ulong userID)
        {
            //Iterate over raid events
            foreach (RaidEvent e in this.raidEvents)
            {
                //Check if id matches
                if (e.ID == raidID)
                {
                    //Try to remove raider
                    if (e.RemoveRaider(userID))
                    {
                        //Return success
                        return true;
                    }
                }
            }

            //Return failure
            return false;
        }

        public List<Raider> GetRaiders(int raidID)
        {
            //Iterate over raid events
            foreach (RaidEvent e in this.raidEvents)
            {
                //Check if id matches
                if (e.ID == raidID)
                {
                    //Return raiders
                    return e.Roster;
                }
            }

            //Return null
            return null;
        }

        public int GetNumberOfRaidEvents()
        {
            return this.raidEvents.Count;
        }

        public RaidEvent GetFirstEvent()
        {
            //Check if there are any
            if (this.raidEvents.Count > 0)
            {
                //Get the first one
                return this.raidEvents.Min;
            }

            //Return nothing
            return null;
        }

        public void AddReminder(Reminder n)
        {
            //Remove any existing reminders
            this.RemoveReminders(n.UserID);

            //Add to list
            this.reminders.Add(n);
        }

        public void AddReminders(Reminder[] n)
        {
            //Remove any existing reminders
            this.RemoveReminders(n[0].UserID);

            //Add to list
            this.reminders.AddRange(n);
        }

        public void RemoveReminders(ulong userID)
        {
            //Iterate over reminders
            for (int i = 0; i < this.reminders.Count; i++)
            {
                //Check if the ID matches
                if (this.reminders[i].UserID == userID)
                {
                    //Remove it
                    this.reminders.RemoveAt(i);

                    //Don't step forward
                    i--;
                }
            }
        }

        public List<Notification> CheckReminders(DateTime now, DateTime next)
        {
            //HACK: For now I'm just piggybacking on this function to delete old raids, maybe move it somewhere else?
            this.raidEvents.RemoveWhere((r) =>
            {
                //Calculate the date and time for the event
                var eventTime = new DateTime(r.Year, r.Month, r.Day, r.Hour, r.Minute, 0, DateTimeKind.Utc) - (TimeSpan.FromHours(r.Timezone));

                //Check if the event is more than 12 hours in the past
                if ((DateTime.UtcNow - TimeSpan.FromHours(12)) > eventTime)
                {
                    //Log it
                    Logger.Log(LOG_LEVEL.INFO, $"Cleaned up raid with id {r.ID} after it was {(DateTime.UtcNow - eventTime).TotalHours} hours old.");

                    //Release ID
                    this.idGenerator.ReleaseID(r.ID);

                    //Remove raid
                    return true;
                }

                //Do not remove
                return false;
            });

            //Setup list to contain any notifications that need to be sent
            var list = new List<Notification>();

            //Iterate over raid events
            foreach (RaidEvent r in this.raidEvents)
            {
                //Calculate the date and time for the event
                var eventTime = new DateTime(r.Year, r.Month, r.Day, r.Hour, r.Minute, 0, DateTimeKind.Utc) - (TimeSpan.FromHours(r.Timezone));

                //Iterate over reminders
                foreach (Reminder n in this.reminders)
                {
                    //Check if the user is signed up
                    if (r.IsInGroup(n.UserID))
                    {
                        //Get the time for when the notification will fire
                        var notificationTime = n.GetNotifyTimeForDate(eventTime);

                        //Check that we haven't passed it
                        if (notificationTime > now)
                        {
                            Console.WriteLine("[DEBUG]: Notifying user #" + n.UserID + " in " + (notificationTime - now).TotalMinutes + " minutes.");

                            //Check if we pass it within the next timestep
                            if ((next > notificationTime) && (notificationTime > now))
                            {
                                //Add to list
                                list.Add(new Notification
                                {
                                    raidID  = r.ID,
                                    userID  = n.UserID,
                                    time    = notificationTime,
                                    hours   = n.Hours,
                                    minutes = n.Minutes
                                });
                            }
                        }
                    }
                }
            }

            //Return the list
            return list;
        }

        public static RaidCalendar LoadFromFile(string path)
        {
            //Prepare result
            RaidCalendar calendar;

            //Create serialiser
            var serialiser = new DataContractSerializer(typeof(RaidCalendar));

            //Open file
            using (XmlReader xr = XmlReader.Create(path))
            {
                //Deserialise
                calendar = (RaidCalendar) serialiser.ReadObject(xr);
            }

            //Return calendar
            return calendar;
        }

        public static void SaveToFile(RaidCalendar calendar, string path)
        {
            //Create serialiser
            var serialiser = new DataContractSerializer(typeof(RaidCalendar));

            //Open file
            using (XmlWriter xw = XmlWriter.Create(path))
            {
                //Serialise
                serialiser.WriteObject(xw, calendar);
            }
        }

        [DataMember(Name = "raidEvents")]
        private SortedSet<RaidEvent> raidEvents;
        [DataMember(Name = "idGenerator")]
        private IDGenerator          idGenerator;
        [DataMember(Name = "notifications")]
        private List<Reminder>       reminders;
    }
}
