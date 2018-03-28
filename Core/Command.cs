using System;
using System.Reflection;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

using Discord.WebSocket;

using DiscordBot.Utils;

//TODO: Replace with state-machine?

namespace DiscordBot.Core
{
    public enum NAME_MATCH : int
    {
        Matched   =  1, //Name matched completely
        Substring =  0, //Name can be found as a substring in the command
        None      = -1, //Didn't match at all
    }

    public sealed class MatchResult
    {
        public Command    cmd;              //Reference to the associated command
        public NAME_MATCH nameMatch;        //Determines to what extent the name matches
        public bool       complete;         //Is it a complete, well-formed command?
        public string     msg;              //If there is an error, it may be stored here
        public int        matchedParams;    //Number of parameters that were matched
        public int        matchedSubstring; //Number of characters that matched
        public string[]   parameters;       //Extracted parameters (size may be greater than 'matchedParams')
    }

    public sealed class CommandCategory
    {
        public string               Name      => this.name;
        public string               NameRegex => this.regex;
        public IEnumerable<Command> Commands  => this.commands;

        public CommandCategory(string name, Type helpClass, string helpMethodName)
        {
            //Copy name and generate regex to match it
            this.name  = (!string.IsNullOrWhiteSpace(name)) ? name : "";
            this.regex = (!string.IsNullOrWhiteSpace(name)) ? (@"^" + Regex.Escape(name) + @"(?:$|\s)") : @"";

            //Get the method that will be called to get help
            if (helpClass != null && !string.IsNullOrWhiteSpace(helpMethodName))
            {
                this.helpMethod = helpClass.GetMethod
                (
                    helpMethodName,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                );
            }
            else this.helpMethod = null;

            //Allocate list to hold commands
            this.commands = new List<Command>();
        }

        public string GetHelp()
        {
            //Check if we don't have a help method
            if (this.helpMethod == null) return null;

            //Capture errors
            try
            {
                //Invoke the help method and capture return value
                var o = this.helpMethod.Invoke(null, null);

                //Check that it's a string
                Debug.Assert(o is System.String, "Return value is not a string.");

                //Return text
                return o as System.String;
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message);
            }

            //Return error
            return "Couldn't get help info.";
        }

        public CommandCategory RegisterCommand(Command cmd)
        {
            //Add the command
            this.commands.Add(cmd);

            //Return 'this' to allow chaining
            return this;
        }

        private string        name;
        private string        regex;
        private MethodBase    helpMethod;
        private List<Command> commands;
    }

    public sealed class Command
    {
        public string Name => this.name;

        //TODO: Allow static methods?

        public Command(string name, object obj, string methodName, string helpMethodName, params string[] regularExpressions)
        {
            //Store the name and target object
            this.name = name;
            this.obj  = obj;

            //Create regex to match name
            this.nameRegex = @"^" + Regex.Escape(this.name);

            //Find the method that will be called to execute a complete command
            this.method = obj.GetType().GetMethod
            (
                methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            );

            //Get the parameters
            this.paramInfo = method.GetParameters();

            //Find the method (if any) that will be called to retrieve help information
            if (!string.IsNullOrWhiteSpace(helpMethodName))
            {
                this.helpMethod = obj.GetType().GetMethod
                (
                    helpMethodName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
            }
            else this.helpMethod = null;

            //Store the regular expressions
            if ((regularExpressions?.Length ?? 0) > 0)
            {
                this.regex = new string[regularExpressions.Length];
                for (int i = 0; i < regularExpressions.Length; i++)
                {
                    this.regex[i] = regularExpressions[i];
                }
            }
            else this.regex = null;
        }

        public string GetHelp(SocketUserMessage msg)
        {
            //Check if we don't have a help method
            if (this.helpMethod == null) return null;

            //Capture errors
            try
            {
                //Invoke the help method and capture return value
                var o = this.helpMethod.Invoke(this.obj, new object[] { msg });

                //Check that it's a string
                Debug.Assert(o is System.String, "Return value is not a string.");

                //Return text
                return o as System.String;
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message);
            }

            //Return error
            return "Couldn't get help info.";
        }

        public bool TryInvoke(SocketUserMessage msg, string[] parameters, out string result)
        {
            //Prepare array to hold SocketUserMessage + any converted parameters
            object[] convertedParams = new object[this.paramInfo.Length];
            convertedParams[0] = msg;

            //Check if at least one parameter was passed along
            if ((parameters?.Length ?? 0) > 0)
            {
                //Iterate over parameters
                for (int i = 1; i < this.paramInfo.Length; i++)
                {
                    //Switch based on type
                    switch (this.paramInfo[i].ParameterType.ToString())
                    {
                        //Int
                        case "System.Int32":
                        {
                            if (Int32.TryParse(parameters[i - 1], out int val))
                            {
                                convertedParams[i] = val;
                            }
                            else
                            {
                                result = "'" + parameters[i - 1] + "' is either not an integer or too big/small.";
                                return false;
                            }
                        } break;

                        //Float
                        case "System.Single":
                        {
                            if (Single.TryParse(parameters[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out float val))
                            {
                                convertedParams[i] = val;
                            }
                            else
                            {
                                result = "'" + parameters[i - 1] + "' is not a single-precision number.";
                                return false;
                            }
                        } break;

                        //Double
                        case "System.Double":
                        {
                            if (Double.TryParse(parameters[i - 1], NumberStyles.Float, CultureInfo.InvariantCulture, out double val))
                            {
                                convertedParams[i] = val;
                            }
                            else
                            {
                                result = "'" + parameters[i - 1] + "' is not a double-precision number.";
                                return false;
                            }
                        } break;

                        //String
                        case "System.String":
                        {
                            convertedParams[i] = parameters[i - 1];
                        } break;

                        //Char
                        case "System.Char":
                        {
                            if (parameters[i].Length == 1)
                            {
                                convertedParams[i] = parameters[i - 1][0];
                            }
                            else
                            {
                                result = "'" + parameters[i - 1] + "' is not a character.";
                                return false;
                            }
                        } break;

                        //Unsupported
                        default:
                        {
                            result = "Cannot convert to '" + this.paramInfo[i].ParameterType.ToString() + "'.";
                            return false;
                        };
                    }
                }
            }

            //Cath any errors
            try
            {
                //Invoke the method with the given parameters
                var ret = this.method.Invoke(this.obj, convertedParams);

                //Check that it's a string
                Debug.Assert(ret is System.String, "Return value is not a string.");

                //Store result
                result = ret as System.String;

                //Return success
                return true;
            }
            catch (AggregateException ae)
            {
                //Get error
                ae.Handle((ex) =>
                {
                    //Log error
                    Logger.Log(LOG_LEVEL.ERROR, ex.Message);

                    //Mark as handled
                    return true;
                });
            }
            catch (TargetInvocationException tix)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, tix.InnerException.Message);
            }
            catch (Exception ex)
            {
                //Log error
                Logger.Log(LOG_LEVEL.ERROR, ex.Message);
            }

            //Return error
            result = "Error in invoked function.";
            return false;
        }

        public MatchResult CheckMatch(string cmd)
        {
            //Prepare the result
            var matchResult = new MatchResult
            {
                cmd              = this,
                nameMatch        = NAME_MATCH.None,
                complete         = false,
                msg              = null,
                matchedParams    = 0,
                matchedSubstring = 0,
                parameters       = null
            };

            //Skip any whitespace
            var skip = Utility.SkipWhitespace(cmd, 0);
            var str  = cmd.Substring(skip);

            //Update length of matched substring
            matchResult.matchedSubstring += skip;

            //Check if the name matches
            var nameMatch = Regex.Match(str, this.nameRegex, RegexOptions.IgnoreCase);
            if (nameMatch.Success)
            {
                //Skip past the name
                str = str.Substring(nameMatch.Length);

                //Update length of matched substring
                matchResult.matchedSubstring += nameMatch.Length;

                //Ensure that there aren't any additional characters after the name besides whitespace
                if (!string.IsNullOrEmpty(str) && !char.IsWhiteSpace(str[0]))
                {
                    //Mark as substring match
                    matchResult.nameMatch = NAME_MATCH.Substring;

                    //Store name
                    matchResult.msg = this.name;

                    //Goto end
                    goto end;
                }

                //Mark that the name matches
                matchResult.nameMatch = NAME_MATCH.Matched;

                //Check if there aren't any regexes to match
                if ((this.regex?.Length ?? 0) == 0)
                {
                    //Mark as a complete match
                    matchResult.complete = true;

                    //Goto end
                    goto end;
                }

                //Check if no parameters are provided
                if (string.IsNullOrWhiteSpace(str))
                {
                    //Write error message
                    matchResult.msg = "No parameters provided.";

                    //Go to end
                    goto end;
                }

                //Prepare array to save parameters (if any)
                matchResult.parameters = (this.paramInfo.Length > 1) ? new string[this.paramInfo.Length - 1] : null;

                //Try to match the parameters
                int offset = 1;
                for (int i = 1; i < this.regex.Length; i += 2)
                {
                    //Skip the whitespace
                    skip = Utility.SkipWhitespace(str, Math.Min(offset, str.Length));
                    str  = str.Substring(skip);

                    //Update length of matched substring
                    matchResult.matchedSubstring += skip;

                    //Check if we've hit the end of the command
                    if (string.IsNullOrEmpty(str))
                    {
                        //Write error message
                        matchResult.msg = "Expected '" + this.regex[i - 1] + "', but reached end of command.";

                        //Go to end
                        goto end;
                    }

                    //Check for match
                    var match = Regex.Match(str, @"^" + this.regex[i], RegexOptions.IgnoreCase | RegexOptions.Multiline);
                    if (match.Success && match.Index == 0)
                    {
                        //Iterate through the groups
                        for (int j = 1; j < match.Groups.Count; j++)
                        {
                            //Extract parameter
                            matchResult.parameters[matchResult.matchedParams] = match.Groups[j].Value;

                            //Increment matched counter
                            matchResult.matchedParams++;
                        }

                        //Set the offset
                        offset = match.Length;
                    }
                    else
                    {
                        //Write error message
                        matchResult.msg = "Expected '" + regex[i - 1] + "'.";

                        //Go to end
                        goto end;
                    }
                }

                //Check that we extracted all parameters
                if (matchResult.matchedParams == (matchResult.parameters?.Length ?? 0))
                {
                    //Update length of matched substring
                    matchResult.matchedSubstring += offset;

                    //Mark as a complete match
                    matchResult.complete = true;

                    //Go to end
                    goto end;
                }
                else
                {
                    //Write error message (This only occurs if the code is wrong)
                    matchResult.msg = "Grim fucked up something.";

                    //Go to end
                    goto end;
                }
            }

            //Return match result
            end:
            return matchResult;
        }

        private string          name;
        private string          nameRegex;
        private object          obj;
        private MethodBase      method;
        private ParameterInfo[] paramInfo;
        private MethodBase      helpMethod;
        private string[]        regex;
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class CommandInit : Attribute
    {
        public static IEnumerable<MethodInfo> GetCommandInitializers<T>() where T : class
        {
            //Enumerate the methods in the class's assembly that have the attribute
            return typeof(T).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(m => m.GetCustomAttribute<CommandInit>() != null);
        }
    }

    /* Maybe one day
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class CommandFunction : Attribute
    {
        public string CommandName => this.cmdName;

        public CommandFunction(string cmdName)
        {
            this.cmdName = cmdName;
        }

        private readonly string cmdName;
    }*/
}
