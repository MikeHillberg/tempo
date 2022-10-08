using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    static public class RestrictedApiList
    {
        static public Dictionary<string, RestrictedApiInfo> RestrictedApis = new Dictionary<string, RestrictedApiInfo>();

        static public string LoadRestrictedApiList(string restrictedApiList)
        {
            var reader = new StringReader(restrictedApiList);
            var sb = new StringBuilder();

            while (true)
            {
                var line = reader.ReadLine();
                if (line == null)
                    break;

                if (line.StartsWith(";"))
                    continue;

                var parts = line.Split(',');
                if (parts.Length == 0)
                    continue;

                else if (parts.Length == 1)
                {
                    sb.Append("Bad item in restricted file list: " + line);
                    continue;
                }

                var info = new RestrictedApiInfo() { Restriction = parts[0] };
                if (parts.Length > 2)
                {
                    info.Indirect = parts[2] == "indirect";
                    info.Restriction = parts[0];
                }

                RestrictedApis[parts[1]] = info;

            }

            return sb.ToString();

        }

    }


    public class RestrictedApiInfo
    {
        public string Restriction { get; set; }
        public bool Indirect { get; set; }
    }
}
