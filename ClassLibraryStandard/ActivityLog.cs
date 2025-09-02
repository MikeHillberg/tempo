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



        public static void Append(string message)
        {
            var timestamp = DateTime.Now.ToString("HHmmss");
            var thread = System.Threading.Thread.CurrentThread.ManagedThreadId.ToString("X4");
            lock (_log)
            {
                Debug.WriteLine(message);
                _log.Add($"[{timestamp}], [{thread,4}] {message}");
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

        public static string GetLog() 
        {
            var sb = new StringBuilder();
            foreach (var line in _log)
                sb.AppendLine(line);

            return sb.ToString();
        }

    }
}
