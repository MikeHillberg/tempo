using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public static class UnhandledExceptionManager
    {
        public static event EventHandler<CommonUnhandledExceptionArgs> UnhandledException;
        public static void ProcessException(Exception e, [CallerMemberName] string location = null)
        {
            UnhandledException?.Invoke(null, new CommonUnhandledExceptionArgs(e, location));
        }
    }

    public class CommonUnhandledExceptionArgs : EventArgs
    {
        public CommonUnhandledExceptionArgs(Exception e, string location)
        {
            Exception = e;
            Location = location;
        }

        public Exception Exception { get; private set; }
        public string Location { get; private set; }
    }
}
