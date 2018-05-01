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

        public string PreferredRole
        {
            get
            {
                //Check that we even have a role
                if (this.roles != null && this.roles.Length > 0)
                {
                    //Get the first role
                    return this.roles[0];
                }

                //Nothing to return
                return null;
            }
        }

        public float GetRoleWeight(string role, List<string> roleList)
        {
            //Filter our roles first
            var filteredRoles = this.roles.Union(roleList);

            //Check if we don't have the role
            if (!filteredRoles.Contains(role)) return 0.0f;

            //If backup just divide the final score by 3
            float d = (this.IsBackup() ? 3.0f : 1.0f);

            //Return a simple weighting
            return (1.0f + (string.Equals(role, filteredRoles.ElementAt(0)) ? 0.5f : 0.0f)) / d;
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
