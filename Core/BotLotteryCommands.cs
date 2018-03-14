using System;
using System.Text.RegularExpressions;
using System.Collections;

using Discord.WebSocket;

using Utility = DiscordBot.Utils.Utility;

namespace DiscordBot.Core
{
    public sealed partial class Bot
    {
        [CommandInit]
        private void ConstructLotteryCommands()
        {
            //Construct the lottery
            this.lottery = new Lottery();
        }

        private static string CmdLotteryAdd(Bot inst, SocketUserMessage _, Match match)
        {
            //Get the name
            string name = match.Groups[1].Value;

            //Grab the numbers
            int[] nums  = Utility.GetNumbers(match.Groups[2].Value);

            //Check if no numbers were provided
            if (nums.Length == 0)
            {
                //Reply with error
                return "No numbers were found!";
            }

            //Try to add the numbers
            Tuple<bool, int> res = inst.lottery.Add(name, nums);
            if (res.Item1)
            {
                //Reply with success
                return "Added " + nums.Length + " number(s) for " + name + ".";
            }
            else
            {
                //Reply with error
                return "The number " + res.Item2 + " has already been entered! (No numbers were added)";
            }
        }

        private static string CmdLotteryRoll(Bot inst, SocketUserMessage _, Match match)
        {
            //Check that we have any numbers
            if (inst.lottery.EntryCount > 0)
            {
                //Roll
                string s = inst.lottery.Roll();

                //Reply with winner
                return "The winner is " + s + "!";
            }
            else
            {
                //Reply with error
                return "No entries in the lottery!";
            }
        }

        private static string CmdLotteryReset(Bot inst, SocketUserMessage _, Match match)
        {
            //Reset the lottery
            inst.lottery.Reset();

            //Reply
            return "Lottery reset.";
        }

        private static string CmdLotteryHelp(Bot inst, SocketUserMessage _, Match match)
        {
            return "The commands are \"$lottery add name - numbers\"," + 
                   "\"$lottery roll\" and \"$lottery reset\".";
        }

        private sealed class Lottery
        {
            public Lottery()
            {
                this.entries = new Hashtable();
                this.low     = int.MaxValue;
                this.high    = int.MinValue;
            }

            public int EntryCount
            {
                get { return this.entries.Count; }
            }

            public Tuple<bool, int> Add(string name, int[] numbers)
            {
                //Make a temp because we're lazy
                Hashtable temp = (Hashtable)this.entries.Clone();

                //Iterate over the numbers to add
                foreach (int n in numbers)
                {
                    //Check if key doesn't exist
                    if (!temp.ContainsKey(n))
                    {
                        //Add number
                        temp.Add(n, name);

                        //Update high/low
                        if (n < low)  this.low  = n;
                        if (n > high) this.high = n;
                    }
                    else
                    {
                        //Return number that was a duplicate
                        return new Tuple<bool, int>(false, n);
                    }
                }

                //Replace existing hashtable with new one
                this.entries = temp;

                //Return success
                return new Tuple<bool, int>(true, 0);
            }

            public string Roll()
            {
                //Prepare our random number generator
                Random r = new Random();

                //Generate a random number between low and high until we get a match
                while (true)
                {
                    //Get number
                    int rand = r.Next(low, high + 1);

                    //Check if it was a hit
                    if (this.entries.ContainsKey(rand))
                    {
                        Console.WriteLine("Picked " + rand);

                        //Get the name
                        string s = (string) this.entries[rand];

                        //Reset
                        this.Reset();

                        //Return name
                        return s;
                    }
                }
            }

            public void Reset()
            {
                this.entries = new Hashtable();
                this.low     = int.MaxValue;
                this.high    = int.MinValue;
            }

            private Hashtable entries;
            private int low;
            private int high;
        }

        private Lottery lottery;
    }
}
