using CommonLibrary;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Threading;

namespace Tempo
{
    public delegate void BackgroundHelperHandler();
    static public class BackgroundHelper
    {

        static public void DoWorkAsyncOld(
            BackgroundHelperHandler worker,
            BackgroundHelperHandler completion = null,
            bool? sync = null)
        {
            if (sync == null)
                sync = DesktopManager2.SyncMode == true;

            if (sync == true)
            {
                try
                {
                    worker.Invoke();

                    if (completion != null)
                        completion.Invoke();
                }
                catch (Exception ex)
                {
                    //MainWindow.InstanceOld.CatchException(ex);
                    UnhandledExceptionManager.ProcessException(ex);
                }
            }

            else
            {

                var bw = new BackgroundWorker();

                bw.DoWork += (s, e) =>
                    {
                        try
                        {
                            worker.Invoke();
                        }
                        catch (Exception)
                        {
                            e.Cancel = true;
                            //MainWindow.InstanceOld.CatchException(ex);
                        }
                    };

                bw.RunWorkerCompleted += (s, e) =>
                    {
                        try
                        {
                            if (!e.Cancelled) // Don't go into an infinite loop
                                completion.Invoke();
                            else
                            {
                                DebugLog.Append("Canceled DoWork");
                            }
                        }
                        catch (Exception ex)
                        {
                            //MainWindow.InstanceOld.CatchException(ex);
                            UnhandledExceptionManager.ProcessException(ex);
                        }
                    };

                bw.RunWorkerAsync();
            }
        }
    }


}
