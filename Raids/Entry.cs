using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Raids
{
    public struct Entry
    {
        public ulong?       user_id   { get; set; }
        public string       user_name { get; set; }
        public bool         backup    { get; set; }
        public List<string> roles     { get; set; }

        public override bool Equals(object obj)
        {
            //Cast to Entry
            var tmp = obj as Entry?;

            //Check if successful
            if (tmp.HasValue)
            {
                //Get the value
                var other = tmp.Value;

                //Check id
                if (this.user_id.HasValue)
                {
                    return (other.user_id.HasValue) && (this.user_id.Value == other.user_id.Value);
                }

                //Check name
                return string.Equals(this.user_name, other.user_name);
            }

            //Different types
            return false;
        }

        public override int GetHashCode()
        {
            //Check id
            if (this.user_id.HasValue)  return this.user_id  .GetHashCode();

            //Check name
            if (this.user_name != null) return this.user_name.GetHashCode();

            return 0;
        }
    }
}
