using System;

namespace DiscordBot.Commands
{
    public sealed class PreconditionException : Exception
    {
        public PreconditionException(string msg) : base(msg)
        { }
    }

    public static class Precondition
    {
        public static void Assert(bool b, string msg)
        {
            //Check condition
            if (!b)
            {
                //Throw precondition exception
                throw new PreconditionException(msg);
            }
        }
    }
}
