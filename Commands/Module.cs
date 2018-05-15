using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using DiscordBot.Utils;

namespace DiscordBot.Commands
{
    public abstract class CommandModuleBase
    {
        public abstract string           ModuleName { get; }
        public abstract List<MethodInfo> Commands   { get; }

        public abstract void   OnInit();

        public abstract string HelpMessage(Context ctx);
    }

    public abstract class CommandModule<T> : CommandModuleBase
        where T : CommandModule<T>, new()
    {
        public static T Instance { get; } = new T();

        public override List<MethodInfo> Commands { get; }

        public CommandModule()
        {
            //Log the loading
            Logger.Log(LOG_LEVEL.INFO, $"Loading module {this.ModuleName}");

            //Get the commands
            this.Commands = CommandManager.GetAllCommands(this);

            //Run module-specific initialization logic
            this.OnInit();
        }
    }

    public static class ModuleManager
    {
        public static CommandModuleBase GetModuleInstance(Type t)
        {
            //Invoke the getter for the CommandModule's Instance property
            return t.BaseType.GetProperty("Instance")
                    .GetMethod.Invoke(null, null) as CommandModuleBase;
        }

        public static List<CommandModuleBase> GetAllModules(Assembly assembly)
        {
            //Catch any errors
            return Debug.Try(() =>
            {
                //Get all modules
                return assembly.GetTypes().Where(t =>
                {
                    //Get the base type
                    var baseType = t.BaseType;

                    //Check that it has a basetype
                    if (baseType != null)
                    {
                        //Check if it's the right base type
                        return baseType.IsGenericType && (baseType.GetGenericTypeDefinition() == typeof(CommandModule<>));
                    }

                    //No match
                    return false;
                })
                //Instantiate the modules
                .Select(t => GetModuleInstance(t)).ToList();
            }, new List<CommandModuleBase>());
        }
    }
}
