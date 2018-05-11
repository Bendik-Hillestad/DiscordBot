using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DiscordBot.Utils;

namespace DiscordBot.Commands
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CommandAttribute : Attribute
    {
        public CommandAttribute(string value)
        {
            this.Signature = value;
        }

        public string Signature
        {
            get; private set;
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class RegexParameterAttribute : Attribute
    {
        public RegexParameterAttribute(string pattern)
        {
            this.Pattern = pattern;
        }

        public string Pattern
        {
            get; private set;
        }
    }

    public static class CommandSearcher
    {
        public static IEnumerable<MethodInfo> GetAllCommands(Type type)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get all commands
                return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                           .Where(m => m.GetCustomAttribute<CommandAttribute>() != null);
            }, new List<MethodInfo>());
        }
    }
}
