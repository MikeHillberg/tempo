using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public static class UnhandledExceptionManager
    {
        public static event EventHandler<CommonUnhandledExceptionArgs> UnhandledException;
        public static void ProcessException(Exception e)
        {
            UnhandledException?.Invoke(null, new CommonUnhandledExceptionArgs(e));
        }
    }

    public class CommonUnhandledExceptionArgs : EventArgs
    {
        public CommonUnhandledExceptionArgs(Exception e)
        {
            Exception = e;
        }

        public Exception Exception { get; private set; }
    }
}
