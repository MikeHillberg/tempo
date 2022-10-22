using Microsoft.UI.Dispatching;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.Storage;

namespace Tempo
{
    // Copied & modified from
    // https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/AppLifecycle/Instancing/cs-winui-packaged/CsWinUiDesktopInstancing/CsWinUiDesktopInstancing

    public static class Program
    {
        private static int activationCount = 1;
        public static List<string> OutputStack { get; private set; }

        // Replaces the standard App.g.i.cs.
        // Note: We can't declare Main to be async because in a WinUI app
        // this prevents Narrator from reading XAML elements.
        [STAThread]
        static void Main(string[] args)
        {
            WinRT.ComWrappersSupport.InitializeComWrappers();

            AppActivationArguments activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();

            //// Is this the first app instance or a secondary?
            //// If it's a secondary we'll forward to the existing and then exit
            //var keyInstance = AppInstance.FindOrRegisterForKey("main");
            //if (!keyInstance.IsCurrent)
            //{
            //    // This isn't the existing app instance
            //    // Call RedirectActivationToAsync to forward this activation to the existing instance.
            //    // We need to ensure that that completes before we exit this process.
            //    // We can't wait for the async call to complete on this thread, because we don't have 
            //    // a dispatcher running (for the async to raise Completed on).
            //    // So run it on a separate thread, and use a semaphore to block waiting on it to complete.

            //    var sem = new Semaphore(0, 1);
            //    Task.Run(() =>
            //    {
            //        keyInstance.RedirectActivationToAsync(activationArgs).AsTask().Wait();
            //        sem.Release();
            //    });

            //    // Wait on the main thread for the Redirect thread to complete
            //    sem.WaitOne();

            //    // Return from main, meaning that the process will end
            //    return;
            //}

            //// If we get to this point, this process is the first instance of this app

            //// The Activated event will raise if another process calls RedirectActivationToAsync to redirect
            //// activation to here
            //keyInstance.Activated += OnActivated;

            //ExtendedActivationKind kind = activationArgs.Kind;
            //var protocol = activationArgs.Data as IProtocolActivatedEventArgs;

            //while(!Debugger.IsAttached)
            //{
            //    Thread.Sleep(500);
            //}

            Microsoft.UI.Xaml.Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(
                    DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }

        #region Report helpers


        private static void OnActivated(object sender, AppActivationArguments args)
        {
            App.MainPage.DispatcherQueue.TryEnqueue(() =>
            {
                App.Instance.ProcessActivationArgs(args);
            });

        }

        public static void GetActivationInfo()
        {
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            ExtendedActivationKind kind = args.Kind;
            //ReportInfo($"ActivationKind: {kind}");

            if (kind == ExtendedActivationKind.Launch)
            {
                if (args.Data is ILaunchActivatedEventArgs launchArgs)
                {
                    string argString = launchArgs.Arguments;
                    string[] argStrings = argString.Split();
                    foreach (string arg in argStrings)
                    {
                        if (!string.IsNullOrWhiteSpace(arg))
                        {
                            //ReportInfo(arg);
                        }
                    }
                }
            }
            else if (kind == ExtendedActivationKind.File)
            {
                if (args.Data is IFileActivatedEventArgs fileArgs)
                {
                    IStorageItem file = fileArgs.Files.FirstOrDefault();
                    if (file != null)
                    {
                        //ReportInfo(file.Name);
                    }
                }
            }
        }

        #endregion


        #region Redirection

        // Decide if we want to redirect the incoming activation to another instance.
        private static bool DecideRedirection()
        {
            bool isRedirect = false;

            // Find out what kind of activation this is.
            AppActivationArguments args = AppInstance.GetCurrent().GetActivatedEventArgs();
            ExtendedActivationKind kind = args.Kind;

            {

                try
                {
                    AppInstance keyInstance = AppInstance.FindOrRegisterForKey("main");

                    // If we successfully registered the file name, we must be the
                    // only instance running that was activated for this file.
                    if (keyInstance.IsCurrent)
                    {
                        keyInstance.Activated += OnActivated;
                    }
                    else
                    {
                        isRedirect = true;
                        RedirectActivationTo(args, keyInstance);
                    }
                }
                catch (Exception ex)
                {
                }
            }

            return isRedirect;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr CreateEvent(
            IntPtr lpEventAttributes, bool bManualReset,
            bool bInitialState, string lpName);

        [DllImport("kernel32.dll")]
        private static extern bool SetEvent(IntPtr hEvent);

        [DllImport("ole32.dll")]
        private static extern uint CoWaitForMultipleObjects(
            uint dwFlags, uint dwMilliseconds, ulong nHandles,
            IntPtr[] pHandles, out uint dwIndex);

        private static IntPtr redirectEventHandle = IntPtr.Zero;

        // Do the redirection on another thread, and use a non-blocking
        // wait method to wait for the redirection to complete.
        public static void RedirectActivationTo(
            AppActivationArguments args, AppInstance keyInstance)
        {
            var sem = new Semaphore(0, 1);
            //redirectEventHandle = CreateEvent(IntPtr.Zero, true, false, null);
            Task.Run(() =>
            {
                keyInstance.RedirectActivationToAsync(args).AsTask().Wait();
                sem.Release();
                //SetEvent(redirectEventHandle);
            });

            sem.WaitOne();

            //uint CWMO_DEFAULT = 0;
            //uint INFINITE = 0xFFFFFFFF;
            //_ = CoWaitForMultipleObjects(
            //   CWMO_DEFAULT, INFINITE, 1,
            //   new IntPtr[] { redirectEventHandle }, out uint handleIndex);
        }

        #endregion

    }
}
