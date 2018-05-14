using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
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

    public static class CommandManager
    {
        private static string FormatCommandSignature(string signature, IEnumerable<string> parameterNames)
        {
            //Replace the {} in the signature with the parameter names
            var n = 0; return Regex.Replace(signature, @"\{\}", e =>
            {
                return $"{parameterNames.ElementAt(n++)}";
            });
        }

        public static string FormatCommandSignature(MethodInfo method, int offset = 0)
        {
            //Get the signature
            var signature = GetCommandSignature(method);
            var tmp = signature.Substring(0, offset);

            //Get the parameters
            var parameters = method.GetParameters().Skip(1)
                                   .Select(p => $"[{p.Name}]")
                                   .ToList();

            //Format the signature
            signature = FormatCommandSignature(signature, parameters);
            tmp       = FormatCommandSignature(tmp, parameters);

            //Skip part of the signature we don't want
            return signature.Substring(tmp.Length);
        }

        public static string GetCommandSignature(MethodInfo method)
        {
            //Get the attribute
            var attr = method.GetCustomAttribute<CommandAttribute>();
            if (attr == null) return null;

            //Grab the signature
            return attr.Signature;
        }

        public static List<MethodInfo> GetAllCommands(CommandModuleBase module)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get all commands
                return module.GetType   ()
                             .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                             .Where     (m => GetCommandSignature(m) != null)
                             .ToList    ();
            }, new List<MethodInfo>());
        }
    }
}
