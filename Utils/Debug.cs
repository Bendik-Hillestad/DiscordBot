using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

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
            return $"{ex.GetType().Name}\n{ex.Message}\n\t    At: {ex.TargetSite?.Name}";
        }

        public static bool Try
        (
            Action f,
            bool verbose = false,
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
            }, false, verbose, severity, method, lineNumber);
        }

        public static T Try<T>
        (
            Func<T> f,
            T defaultValue,
            bool verbose = false,
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
                    //Rethrow the actual exception
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                }
            }
            catch (AggregateException ae)
            {
                //Get errors
                ae.Handle((ex) =>
                {
                    //Check if verbose logging is desired
                    if (verbose)
                    {
                        //Log error with full stacktrace
                        Logger.Log(severity, ex.ToString(), method, lineNumber);
                    }
                    else
                    {
                        //Log error without full stacktrace
                        Logger.Log(severity, FormatExceptionMessage(ex), method, lineNumber);
                    }
                    
                    //Mark as handled
                    return true;
                });
            }
            catch (Exception ex)
            {
                //Check if verbose logging is desired
                if (verbose)
                {
                    //Log error with full stacktrace
                    Logger.Log(severity, ex.ToString(), method, lineNumber);
                }
                else
                {
                    //Log error without full stacktrace
                    Logger.Log(severity, FormatExceptionMessage(ex), method, lineNumber);

                    //Check for inner exception
                    while (ex.InnerException != null)
                    {
                        //Log inner exception
                        Logger.Log(severity, FormatExceptionMessage(ex.InnerException), method, lineNumber);

                        //Unwrap the exception
                        ex = ex.InnerException;
                    }
                }
            }

            //Return the default value
            return defaultValue;
        }
    }
}
