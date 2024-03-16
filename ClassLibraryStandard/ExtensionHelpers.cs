using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    // Extension method helpers. Named with "My" to help me remember which methods are real vs custom
    static public class MyExtensionHelpers
    {

        public static string[] MySplit(this string toSplit, string splitter)
        {
            return toSplit.Split(new string[] { splitter }, StringSplitOptions.None);
        }

        public static string[] MySplitOnFirst(this string toSplit, string splitter)
        {
            var index = toSplit.IndexOf(splitter);
            if (index == -1)
                return null;

            var left = toSplit.Substring(0, index);
            var right = toSplit.Substring(index + splitter.Length);
            return new string[] { left, right };
        }

        /// <summary>
        /// Remove internal duplicate spaces. Like Trim() but on the inside.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string MyCompressSpaces(this string str)
        {
            var split = str.Split(' ');

            var sb = new StringBuilder();
            foreach(var part in split)
            {
                // If there are multiple spaces, the split will include empty strings.
                if(string.IsNullOrEmpty(part))
                {
                    continue;
                }

                // Don't add a prefix space on the first part
                if(sb.Length == 0)
                {
                    sb.Append(part);
                }
                else
                { 
                    sb.Append($" {part}");
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Get the next item in an array or default(T)
        /// </summary>
        public static T MyPeekNext<T>(this T[] tokenStrings, int tokenIndex)
        {
            if (tokenIndex + 1 < tokenStrings.Length)
            {
                return tokenStrings[tokenIndex + 1];
            }
            else
            {
                return default(T);
            }
        }


        public static bool MyEqualsInsensitive(this string str1, string str2)
        {
            return 0 == String.Compare(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool MyStartsWithInsensitive(this string str1, string str2)
        {
            return str1.StartsWith(str1, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Return Peek() if stack isn't empty, default(T) otherwise
        /// </summary>
        public static T MyPeekOrDefault<T>(this Stack<T> stack)
        {
            if(stack.Count > 0)
            {
                return stack.Peek();
            }
            else
            {
                return default(T);
            }
        }

        public static bool MyTryPop<T>(this Stack<T> stack, out T value)
        {
            if(stack.Count > 0)
            {
                value = stack.Pop();
                return true;
            }

            value = default(T);
            return false;
        }

    }

}
