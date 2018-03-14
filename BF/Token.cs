using System;
using System.Linq;
using System.Reflection;

namespace DiscordBot.BF
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = true)]
    public sealed class Token : Attribute
    {
        public Token(string regex) => this.regex = regex;

        public static Tuple<T, string>[] GetTokens<T>() where T : struct
        {
            return typeof(T).GetFields()
                            .Where    (f => f.GetCustomAttribute<Token>() != null)
                            .Select   (f => new Tuple<T, string>((T)f.GetValue(null), f.GetCustomAttribute<Token>().regex))
                            .ToArray  ();
        }

        private readonly string regex;
    }
}
