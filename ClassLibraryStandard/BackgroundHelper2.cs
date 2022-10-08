using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    static public class BackgroundHelper2
    {
        static public Task<T> DoWorkAsync<T>(Func<T> func)
        {
            if (Settings.SyncMode)
            {
                var result = func();
                return Task<T>.FromResult(result); // Don't have Task.CompletedTask yet
            }
            else
            {
                var t = Task<T>.Run(() =>
                {
                    try
                    {
                        return func();
                    }
                    catch (Exception ex)
                    {
                        UnhandledExceptionManager.ProcessException(ex);
                        //MainWindow.InstanceOld.CatchException(ex);
                        return default(T);
                    }
                });

                return t;
            }
        }

    }
}
