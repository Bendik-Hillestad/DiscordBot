﻿using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ComponentModel;

namespace DiscordBot.Raids
{
    [DataContract]
    public sealed class Raider
    {
        public Raider(ulong userID, string nick, IEnumerable<string> roles, bool backup)
        {
            this.ID     = userID;
            this.nick   = nick;
            this.roles  = roles.ToArray();
            this.backup = backup;
        }

        public bool HasRole(string role)
        {
            //Check if our roles is null
            if (this.roles == null) return false;

            //Search through roles
            foreach (string s in this.roles)
            {
                //Check for match
                if (s == role) return true;
            }

            //Role not found
            return false;
        }

        public float GetRoleWeight(string role)
        {
            //Check if we don't have the role
            if (!this.HasRole(role)) return 0.0f;

            //If backup just divide the final score by this value
            float d = (this.IsBackup() ? 3.0f : 1.0f);

            //Return a simple weighting
            return (1.0f + (string.Equals(role, this.roles[0]) ? 0.5f : 0.0f)) / d;
        }

        public bool IsBackup()
        {
            return this.backup;
        }

        [DataMember(Name = "ID")]
        public ulong ID;
        [DataMember(Name = "nick")]
        public string nick;
        [DataMember(Name = "roles")]
        public string[] roles;
        [DataMember(Name = "backup"), DefaultValue(false)]
        public bool backup;
    }
}
