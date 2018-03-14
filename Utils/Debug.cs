using System;
using System.Runtime.CompilerServices;

namespace DiscordBot.Utils
{
    public static class Debug
    {
        public static void Assert(bool condition, string msg)
        {
            //Check if condition failed
            if (!condition)
            {
                //Throw exception
                throw new Exception($"Assertion failed! {msg}");
            }
        }

        public static bool Try
        (
            Action f,
            LOG_LEVEL severity = LOG_LEVEL.ERROR,
            [CallerMemberName] string method  = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            //Catch any errors
            try
            {
                //Do action
                f();

                //Return success
                return true;
            }
            catch (AggregateException ae)
            {
                //Get errors
                ae.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message, method, lineNumber);

                    //Mark as handled
                    return true;
                });
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message, method, lineNumber);
            }

            //Return failure
            return false;
        }

        public static T Try<T>
        (
            Func<T> f,
            T defaultValue,
            LOG_LEVEL severity = LOG_LEVEL.ERROR,
            [CallerMemberName] string method  = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            //Catch any errors
            try
            {
                //Run the function and return the value from it
                return f();
            }
            catch (AggregateException ae)
            {
                //Get errors
                ae.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message, method, lineNumber);

                    //Mark as handled
                    return true;
                });
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message, method, lineNumber);
            }

            //Return the default value
            return defaultValue;
        }
    }
}
