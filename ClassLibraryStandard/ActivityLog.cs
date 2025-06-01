using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Tempo
{
    public static class DebugLog
    {
        static List<string> _log = new List<string>();

        public static void Start(string message)
        {
            lock (_log)
            {
                // Sanity check for robustness
                if (_log.Count > 50000)
                    _log.Clear();

                _log.Add("\r\n" + Thread + message);
            }
        }

        public static void Append(string message)
        {
            lock (_log)
            {
                Debug.WriteLine(message);
                _log.Add(Thread + message);
            }
        }

        public static void Append(Exception e)
        {
            Append(e.Message);
            if (e.StackTrace != null)
            {
                Append(e.StackTrace);
            }
        }

        public static void Append(Exception e, string message)
        {
            Append(message);
            Append(e);
        }

        static string Thread
        {
            get
            {
                return ""; // No Thread available in portable library
            }
        }



        public static string GetLog() 
        {
            var sb = new StringBuilder();
            foreach (var line in _log)
                sb.AppendLine(line);

            return sb.ToString();
        }

    }
}
