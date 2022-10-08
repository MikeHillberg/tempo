using System;
using System.Collections.Generic;
using System.Text;

namespace Tempo
{
    static public class ExtensionHelpers2
    {
        public static void AppendQuoted(this StringBuilder sb, string value)
        {
            sb.Append($"\"{value}\"");
        }
        public static void AppendCell(this StringBuilder sb, string value)
        {
            sb.Append($"\"{value},\"");
        }
    }
}
