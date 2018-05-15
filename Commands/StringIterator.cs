using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace DiscordBot.Commands
{
    public sealed class StringIterator
    {
        /// <summary>
        /// Constructs a new string iterator.
        /// </summary>
        /// <param name="str">The string to iterate over.</param>
        public StringIterator(string str)
        {
            //Initialise our iterator
            this.str   = str;
            this.Index = 0;
        }

        /// <summary>
        /// Gets the current position in the string.
        /// </summary>
        public int Index { get; private set; }

        /// <summary>
        /// Moves the iterator forwards and returns 
        /// if it's still valid
        /// </summary>
        public bool Next()
        {
            //Check if we have more characters left
            if (this.Index < this.str.Length)
            {
                //Increment our index
                this.Index++;

                //Still valid
                return true;
            }

            //We're now no longer valid
            return false;
        }

        /// <summary>
        /// Returns the current character in the iterator.
        /// </summary>
        [Pure]
        public char? Current()
        {
            //Check if we have more characters left
            if (this.Index < this.str.Length)
            {
                //Return the character
                return this.str[this.Index];
            }

            //No character so return null
            return null;
        }

        /// <summary>
        /// Checks for and returns the next character in the iterator
        /// without altering the state of the iterator.
        /// </summary>
        [Pure]
        public char? Peek()
        {
            //Check if we have more characters left
            if ((this.Index + 1) < this.str.Length)
            {
                //Return the character
                return this.str[this.Index + 1];
            }

            //No character so return null
            return null;
        }

        /// <summary>
        /// Checks for and returns the next character in the iterator
        /// without altering the state of the iterator.
        /// <param name="offset"/>How far ahead to look.</param>
        /// </summary>
        [Pure]
        public char? Peek(int offset)
        {
            //Check if we have more characters left
            if ((this.Index + offset) < this.str.Length)
            {
                //Return the character
                return this.str[this.Index + offset];
            }

            //No character so return null
            return null;
        }

        /// <summary>
        /// Looks for and retrieves the index of the first
        /// occurence of a character in the iterator,
        /// without advancing the iterator.
        /// </summary>
        /// <param name="ch">The character to look for.</param>
        [Pure]
        public int Find(char value)
        {
            //Check if we're searching for whitespace
            if (char.IsWhiteSpace(value))
            {
                //Iterate over the characters
                for (int i = Index; i < this.str.Length; i++)
                {
                    //Check for whitespace
                    if (char.IsWhiteSpace(this.str[i]))
                    {
                        //Return the offset from our current index
                        return i - Index;
                    }
                }

                //Whitespace not found
                return -1;
            }

            //Just use the default way
            return this.str.IndexOf(value, Index) - Index;
        }

        /// <summary>
        /// Skips a character in the iterator.
        /// </summary>
        public void Skip()
        {
            //Just increment our index
            this.Index++;
        }

        /// <summary>
        /// Moves the iterator forwards until there is no more whitespace.
        /// </summary>
        public void SkipWhitespace()
        {
            //Continue iterating until we hit the end or no more whitespace
            while ((this.Index < this.str.Length) && char.IsWhiteSpace(this.str[this.Index]))
                this.Index++;
        }

        /// <summary>
        /// Retrieves all characters that match the regular expression
        /// and advances the iterator to the end of the match.
        /// </summary>
        /// <param name="regex">The regular expression that will do the matching.</param>
        public string ReadRegex(Regex regex, int limit = 0)
        {
            //Check if we have more characters left
            if (this.Index < this.str.Length)
            {
                //Get the substring we're working with
                var sub = (limit > 0) ? this.str.Substring(this.Index, limit)
                                      : this.str.Substring(this.Index);

                //Try to match the regex
                if (regex.IsMatch(sub))
                {
                    //Get the first match
                    var match = regex.Match(sub).Value;

                    //Move our index
                    this.Index += match.Length;

                    //Return the match
                    return match;
                }

                //No match so we return empty string
                return string.Empty;
            }

            //No character so return null
            return null;
        }

        /// <summary>
        /// Returns the remaining characters in the
        /// iterator.
        /// </summary>
        public override string ToString()
        {
            //Check if we have more characters left
            if (this.Index < this.str.Length)
            {
                //Return the remaining characters
                return this.str.Substring(this.Index);
            }

            //Return an empty string
            return string.Empty;
        }

        private readonly string str;
    }
}
