using System.Linq;
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
            if (this.HasRole(role)) return 0.0f;

            //Return a simple weighting
            return 1.0f + (string.Equals(role, this.roles[0]) ? 0.5f : 0.0f);
        }

        public bool IsBackup()
        {
            return this.backup;
        }

        /*public float GetPreference(string role)
        {
            //Check if we don't have the role
            if (!this.HasRole(role)) return 0.0f;

            //Set starting preference
            float pref = 1.4f;

            //Iterate through the roles
            for (int i = 0; i < this.roles.Length; i++, pref -= (0.9f) / (roles.Length - 1))
            {
                //Check for match
                if (this.roles[i].ToLower() == role.ToLower()) break;
            }

            //Return the score
            return pref;
        }*/

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
