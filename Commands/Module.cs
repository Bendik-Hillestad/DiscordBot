using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DiscordBot.Utils;

namespace DiscordBot.Commands
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ModuleAttribute : Attribute
    {}

    public static class ModuleSearcher
    {
        public static IEnumerable<Type> GetAllModules(Assembly assembly)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get all modules
                return assembly.GetTypes().Where(t => t.GetCustomAttribute<ModuleAttribute>() != null);
            }, new List<Type>());
        }
    }
}
