﻿using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace DiscordBot.Utils
{
    public static class Debug
    {
        public sealed class AssertionFailureException : Exception
        {
            public AssertionFailureException(string msg) : base(msg)
            { }
        }

        public static void Assert(bool condition, string msg)
        {
            //Check if condition failed
            if (!condition)
            {
                //Throw exception
                throw new AssertionFailureException($"Assertion failed! {msg}");
            }
        }

        public static string FormatExceptionMessage(Exception ex)
        {
            return $"{ex.GetType().Name}\n{ex.Message}\n\t    At: {ex.TargetSite.Name}";
        }

        public static bool Try
        (
            Action f,
            LOG_LEVEL severity = LOG_LEVEL.ERROR,
            [CallerMemberName] string method  = "",
            [CallerLineNumber] int lineNumber = 0
        )
        {
            //Catch any errors and return true if no errors, false otherwise
            return Try<bool>(() =>
            {
                //Run the function
                f();

                //No errors
                return true;
            }, false);
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
                //Catch target invocation exceptions
                try
                {
                    //Run the function and return the value from it
                    return f();
                }
                catch (TargetInvocationException tie)
                {
                    //Rethrow the real exception
                    throw tie.InnerException;
                }
            }
            catch (AggregateException ae)
            {
                //Get errors
                ae.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(severity, FormatExceptionMessage(ex), method, lineNumber);

                    //Mark as handled
                    return true;
                });
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(severity, FormatExceptionMessage(ex), method, lineNumber);

                //Check for inner exception
                if (ex.InnerException != null)
                {
                    //Log inner exception
                    Logger.Log(severity, FormatExceptionMessage(ex.InnerException), method, lineNumber);
                }
            }

            //Return the default value
            return defaultValue;
        }
    }
}
