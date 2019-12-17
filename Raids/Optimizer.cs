using System;

#nullable enable

namespace DiscordBot.Raids
{
    internal static class Optimizer
    {
        public enum Role : byte { }

        public struct Assignment
        {
            public byte id;
            public Role role;
        }

        public readonly struct Player
        {
            public Player(Role[] roles)
            {
                this.roles = roles;
            }

            public readonly Role[] roles;
        }

        public static byte[] PrepareComposition(ReadOnlySpan<string> layout, ReadOnlySpan<string> roles)
        {
            //Create an array to hold the number of times each role appears in the composition
            var roleCounts = new byte[roles.Length];

            //Go through each role in the composition
            foreach (string role in layout)
            {
                //Get the index for this role
                int index = roles.IndexOf(role);

                //Increment the count for that role
                ++roleCounts[index];
            }

            return roleCounts;
        }

        public static Player[] PrepareRoster(ReadOnlySpan<Entry> roster, ReadOnlySpan<string> roles)
        {
            //Allocate an array to hold the simplified roster
            var players = new Player[roster.Length];

            //Go through each entry in the roster
            for (int i = 0; i < roster.Length; ++i)
            {
                //Get the entry
                var entry = roster[i];

                //Create an array to hold the roles as indices
                var playerRoles = new Role[entry.roles.Length];

                //Map each role into an index
                for (int j = 0; j < entry.roles.Length; ++j)
                {
                    playerRoles[j] = (Role)roles.IndexOf(entry.roles[j]);
                }

                //Save the simplified player info
                players[i] = new Player(playerRoles);
            }

            return players;
        }

        public static Assignment[] Optimize(ReadOnlySpan<byte> composition, ReadOnlySpan<Player> roster)
        {
            //Validate the inputs
            if ((composition.Length > 0) && (roster.Length > 0))
            {
                //Make a copy of the composition so that we can modify it during optimization
                Span<byte> roleCounters = stackalloc byte[composition.Length];
                composition.CopyTo(roleCounters);

                //Count the total number of players that can appear in the comp
                int maxTotalPlayers = 0;
                foreach (byte b in roleCounters) maxTotalPlayers += b;

                //Setup the buffers for holding intermediate and final results
                Span<Assignment> currentAssignments = stackalloc Assignment[maxTotalPlayers];
                Span<Assignment> finalAssignments = stackalloc Assignment[maxTotalPlayers];
                int finalTotalPlayers = 0;
                int finalDepth = 0;

                //Start the recursive optimizing algorithm
                recurse
                (
                    currentAssignments,
                    finalAssignments,
                    0,
                    ref finalTotalPlayers,
                    ref finalDepth,
                    maxTotalPlayers,
                    0,
                    roster,
                    roleCounters
                );

                //Check if we were able to assign any roles
                if (finalTotalPlayers > 0)
                {
                    //Return the resulting assignments
                    return finalAssignments.Slice(0, finalTotalPlayers).ToArray();
                }
            }

            return Array.Empty<Assignment>();
        }

        private static void recurse
        (
            Span<Assignment> currentAssignments,
            Span<Assignment> finalAssignments,
            int currentTotalPlayers,
            ref int finalTotalPlayers,
            ref int finalDepth,
            int maxTotalPlayers,
            int depth,
            ReadOnlySpan<Player> roster,
            Span<byte> roleCounters
        )
        {
            //Check that we're not full and that we didn't hit the end of our roster
            if ((currentTotalPlayers < maxTotalPlayers) && (depth < roster.Length))
            {
                //Get the player at this depth
                ref readonly Player currentPlayer = ref roster[depth];

                //Track whether this player is used
                bool playerUnused = true;

                //Get the slot this player would be assigned to
                ref Assignment slot = ref currentAssignments[currentTotalPlayers];

                //Go through each role this player has
                foreach (Role role in currentPlayer.roles)
                {
                    //Get the counter for this role
                    ref byte roleCounter = ref roleCounters[(int)role];

                    //Check if this role is available
                    if (roleCounter > 0)
                    {
                        //Claim this role
                        --roleCounter;

                        //Assign the player to the open slot
                        slot.id = (byte)depth;
                        slot.role = role;
                        playerUnused = false;

                        //Continue to the next player
                        recurse(currentAssignments, finalAssignments, currentTotalPlayers + 1, ref finalTotalPlayers, ref finalDepth, maxTotalPlayers, depth + 1, roster, roleCounters);

                        //Release this role
                        ++roleCounter;
                    } 
                }

                //If we couldn't use this player, then replace them with the next one
                if (playerUnused)
                {
                    recurse(currentAssignments, finalAssignments, currentTotalPlayers, ref finalTotalPlayers, ref finalDepth, maxTotalPlayers, depth + 1, roster, roleCounters);
                }
            }
            else
            {
                //Check if we've found a clearly better result
                if (currentTotalPlayers > finalTotalPlayers)
                {
                    //Save these assignments
                    finalTotalPlayers = currentTotalPlayers;
                    finalDepth = depth;
                    currentAssignments.Slice(0, currentTotalPlayers).CopyTo(finalAssignments);
                }
                //Check if we've found a slightly better result 
                else if ((currentTotalPlayers == finalTotalPlayers) && (depth < finalDepth))
                {
                    //Save these assignments
                    finalDepth = depth;
                    currentAssignments.Slice(0, currentTotalPlayers).CopyTo(finalAssignments);
                }
            }
        }
    }
}
