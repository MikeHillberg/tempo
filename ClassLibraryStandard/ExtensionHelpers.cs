using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    static public class CommonExtensionHelpers
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

        public static bool EqualsInsensitive(this string str1, string str2)
        {
            return 0 == String.Compare(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool StartsWithInsensitive(this string str1, string str2)
        {
            return str1.StartsWith(str1, StringComparison.OrdinalIgnoreCase);
        }

    }

}
