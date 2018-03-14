using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DiscordBot.BF
{
    public static class Tokenizer
    {
        public static T[] Tokenize<T>(string str, Tuple<T, string>[] tokenList) where T : struct
        {
            //Prepare list
            var ret  = new List<T>();

            //Make a copy of the input string
            var temp = string.Copy(str);

            //While there are characters
            while (!string.IsNullOrEmpty(temp))
            {
                //Prepare best match
                int    offset = temp.Length - 1;
                int    len    = 0;
                object token  = null;

                //Go through token list
                for (int i = 0; i < tokenList.Length; i++)
                {
                    //Check for match
                    var match = Regex.Match(temp, tokenList[i].Item2);
                    if (match.Success)
                    {
                        //Check if it's better
                        if (match.Index < offset || (match.Index == offset && match.Length > len))
                        {
                            //Save token, offset and length
                            offset = match.Index;
                            len    = match.Length;
                            token  = tokenList[i].Item1;
                        }
                    }
                }

                //Check that we found a match
                if (token != null)
                {
                    //Add token
                    ret.Add((T)token);

                    //Move through string
                    temp = temp.Substring(offset + 1);
                }
                else break;
            }

            //Return tokens
            return ret.ToArray();
        }
    }
}
