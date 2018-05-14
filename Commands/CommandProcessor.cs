using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Globalization;

namespace DiscordBot.Commands
{
    public struct CommandMatch
    {
        public CommandModuleBase module;
        public MethodInfo        cmd;
        public int               inputMatch;
        public int               signatureMatch;
        public int               signatureLength;
        public object[]          extractedParams;
        public int               count;
    }

    public static class CommandProcessor
    {
        public static List<CommandMatch> ProcessCommand(CommandModuleBase module, string inputString)
        {
            //Prepare a list to hold our candidates
            var candidates = new List<CommandMatch>();

            //Go through each command in the module
            module.Commands.ForEach(cmd =>
            {
                //Grab the signature to match
                var signature = CommandManager.GetCommandSignature(cmd);

                //Prepare our iterators
                var inputIterator     = new StringIterator(inputString);
                var signatureIterator = new StringIterator(signature);

                //Prepare candidate
                var paramInfo = cmd.GetParameters();
                var candidate = new CommandMatch
                {
                    module          = module,
                    cmd             = cmd,
                    inputMatch      = 0,
                    signatureMatch  = 0,
                    signatureLength = signature.Length,
                    extractedParams = new object[paramInfo.Length],
                    count           = 1
                };

                //Iterate through the strings
                while (true)
                {
                    //Check for valid iterators
                    if (!inputIterator.Current().HasValue || !signatureIterator.Current().HasValue) break;

                    //Get the next characters
                    var ch1 = inputIterator.    Current().Value;
                    var ch2 = signatureIterator.Current().Value;

                    //Check if we hit a whitespace character in the signature
                    if (char.IsWhiteSpace(ch2))
                    {
                        //Check that the input string has at least one whitespace
                        if (char.IsWhiteSpace(ch1))
                        {
                            //Skip the whitespace
                            inputIterator.    SkipWhitespace();
                            signatureIterator.SkipWhitespace();

                            //Continue without advancing iterators
                            continue;
                        }
                        else break;
                    }

                    //Check if we reached a parameter
                    if ((ch2 == '{') && (signatureIterator.Peek() == '}'))
                    {
                        //Skip any whitespace before our parameter
                        inputIterator.SkipWhitespace();

                        //Try to match the input against our expected parameter
                        if (CheckMatch(paramInfo[candidate.count], inputIterator, signatureIterator, out object result))
                        {
                            //Store the result
                            candidate.extractedParams[candidate.count++] = result;

                            //Skip over the brackets in the signature
                            signatureIterator.Skip();
                            signatureIterator.Skip();

                            //Check if the signature is not explicitly requiring a whitespace
                            var tmp = signatureIterator.Current();
                            if (tmp.HasValue && !char.IsWhiteSpace(tmp.Value))
                            {
                                //Allow optional whitespace which we skip
                                inputIterator.SkipWhitespace();
                            }

                            //Continue without advancing iterators
                            continue;
                        }
                        break;
                    }

                    //Normal case-insensitive string comparison
                    if (char.ToLowerInvariant(ch1) != char.ToLowerInvariant(ch2)) break;

                    //Advance our iterators
                    inputIterator.    Next();
                    signatureIterator.Next();
                }

                //Update how much we matched of the input and signature
                candidate.inputMatch     = inputIterator.Index;
                candidate.signatureMatch = signatureIterator.Index;

                //Push candidate into list
                candidates.Add(candidate);
            });

            //Return the candidates
            return candidates;
        }

        private static bool CheckMatch(ParameterInfo param, StringIterator inputIterator, StringIterator signatureIterator, out object result)
        {
            //Get the type of the parameter
            Type paramType = param.ParameterType;

            //Check for integer numbers
            if (IsInteger(paramType))
            {
                //Read the integer
                return ReadInteger(paramType, inputIterator, out result);
            }

            //Check for floating point numbers
            if (IsFloatingPoint(paramType))
            {
                //Read the floating point
                return ReadFloatingPoint(paramType, inputIterator, out result);
            }

            //Check for string
            if (IsString(paramType))
            {
                //Get the pattern for the regex
                var pattern = param.GetCustomAttribute<RegexParameterAttribute>().Pattern;

                //Check what is expected after our string
                var ch = signatureIterator.Peek(2);
                int offset = (ch.HasValue) ? inputIterator.Find(ch.Value) : 0;

                //If the input does not have the expected character we
                //continue anyway. The match will fail after this.
                if (offset < 0) offset = 0;

                //Read the string
                return ReadString(pattern, inputIterator, offset, out result);
            }

            //We do not recognize the type
            throw new Exception($"Unknown type {param.ParameterType.Name}!");
        }

        private static bool IsInteger(Type t)
        {
            //Match against the type codes for integers
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFloatingPoint(Type t)
        {
            //Match against the type codes for floats
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsString(Type t)
        {
            return Type.GetTypeCode(t) == TypeCode.String;
        }

        private static bool ReadInteger(Type expectedType, StringIterator inputIterator, out object result)
        {
            //Prepare a simple regex
            var regex = new Regex(@"^[+-]?\d+");

            //Send it to our iterator
            var number = inputIterator.ReadRegex(regex);

            //Setup the style and culture for parsing the float
            var style  = NumberStyles.AllowLeadingSign;
            var format = CultureInfo.InvariantCulture.NumberFormat;

            //Get the appropriate parser based on type
            switch (Type.GetTypeCode(expectedType))
            {
                case TypeCode.Byte:   { bool e = Byte.  TryParse(number, style, format, out Byte   tmp); result = tmp; return e; }
                case TypeCode.SByte:  { bool e = SByte. TryParse(number, style, format, out SByte  tmp); result = tmp; return e; }
                case TypeCode.UInt16: { bool e = UInt16.TryParse(number, style, format, out UInt16 tmp); result = tmp; return e; }
                case TypeCode.UInt32: { bool e = UInt32.TryParse(number, style, format, out UInt32 tmp); result = tmp; return e; }
                case TypeCode.UInt64: { bool e = UInt64.TryParse(number, style, format, out UInt64 tmp); result = tmp; return e; }
                case TypeCode.Int16:  { bool e = Int16. TryParse(number, style, format, out Int16  tmp); result = tmp; return e; }
                case TypeCode.Int32:  { bool e = Int32. TryParse(number, style, format, out Int32  tmp); result = tmp; return e; }
                case TypeCode.Int64:  { bool e = Int64. TryParse(number, style, format, out Int64  tmp); result = tmp; return e; }
            }

            //Should never reach this
            throw new Exception("Hit unreachable code.");
        }

        private static bool ReadFloatingPoint(Type expectedType, StringIterator inputIterator, out object result)
        {
            //Prepare a simple regex
            var regex = new Regex(@"^[+-]?\d+(\.\d+)?");

            //Send it to our iterator
            var number = inputIterator.ReadRegex(regex);

            //Setup the style and culture for parsing the float
            var style  = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            var format = CultureInfo.InvariantCulture.NumberFormat;

            //Get the appropriate parser based on type
            switch (Type.GetTypeCode(expectedType))
            {
                case TypeCode.Single:  { bool e = Single. TryParse(number, style, format, out Single  tmp); result = tmp; return e; }
                case TypeCode.Double:  { bool e = Double. TryParse(number, style, format, out Double  tmp); result = tmp; return e; }
                case TypeCode.Decimal: { bool e = Decimal.TryParse(number, style, format, out Decimal tmp); result = tmp; return e; }
            }

            //Should never reach this
            throw new Exception("Hit unreachable code.");
        }

        private static bool ReadString(string pattern, StringIterator inputIterator, int limit, out object result)
        {
            //Prepare the regex
            var regex = new Regex(pattern);

            //Send it to our iterator
            var tmp = inputIterator.ReadRegex(regex, limit);
            result  = tmp;

            //Check that it's not null or empty
            return !string.IsNullOrWhiteSpace(tmp);
        }
    }
}
