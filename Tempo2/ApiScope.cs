using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tempo
{
    /// <summary>
    /// Methods to load an API Scope (Windows, WinAppSDK, Custom)
    /// </summary>
    internal class ApiScopeLoader
    {
        bool _isLoaded = false;
        bool _isCanceled = false;

        ManualResetEvent _loadCompletedEvent = null;
        ManualResetEvent _loadingThreadEvent = null;

        /// <summary>
        /// Load the scope by running an action off thread
        /// Completed actions run on the UI thread. It's done here rather than in the async completion
        /// to get them run at the right time.
        /// </summary>
        internal async void StartLoad(
            Action offThreadLoadAction,     // How to load (runs off thread)
            Action uiThreadCompletedAction, // What to do when load completes (runs on UI thread)
            Action uiThreadCanceledAction)  // What to do if load is canceled (runs on UI thread)
        {
            // At this point we're on the UI thread

            if(_isLoaded)
            {
                return;
            }

            if (_loadCompletedEvent != null)
            {
                // Already a load in progress
                return;
            }

            // This is signaled when the loading worker thread finishes
            _loadingThreadEvent = new ManualResetEvent(false);

            // This is signaled when the load is complete (which happens on the UI thread)
            _loadCompletedEvent = new ManualResetEvent(false);

            _isCanceled = false;

            // Fork the load action
            _ = Task.Run(() =>
            {
                // This is the loading thread

                try
                {
                    offThreadLoadAction();
                }
                catch(Exception)
                {
                    _isLoaded = false;
                    _isCanceled = true;
                }

                // Run a completion on the UI thread
                _loadingThreadEvent.Set();
            });

            // The event will be released either by the load completing or by the Ensure method canceling the load
            // bugbug: there must be a better way to do this than forking another thread?
            await Task.Run(() => _loadingThreadEvent.WaitOne());

            // We're on the UI thread after the load thread has completed, or we're canceling

            if (_isCanceled)
            {
                // The "loading ..." dialog was canceled
                uiThreadCanceledAction();
            }
            else
            {
                // Completed successfully
                uiThreadCompletedAction();

                // Now we're fully loaded
                _isLoaded = true;
            }

            _loadingThreadEvent = null;

            // This will release Ensure()
            _loadCompletedEvent.Set();
            _loadCompletedEvent = null;

            return;
        }

        Task _contentDialogTask = null;
        LoadingDialog _contentDialog = null;

        /// <summary>
        //// Make sure loading has completed, showing a dialog if necessary
        /// </summary>
        internal async Task<bool> EnsureLoadedAsync(string loadingMessage)
        {
            if (_isLoaded)
            {
                return true;
            }

            // Show a "Loading" message while we're downloading from nuget.org
            _contentDialog = new LoadingDialog();
            _contentDialog.Message = loadingMessage;
            _contentDialog.XamlRoot = App.HomePage.XamlRoot;
            _contentDialogTask = _contentDialog.ShowAsync().AsTask();
            _contentDialog.CloseButtonText = "Cancel";

            // Convert the load-completed event to a task (must be a better way to do this?)
            var loadCompletedEventTask = Task.Run(() => _loadCompletedEvent.WaitOne());

            // Wait for either the load to complete, or the dialog to be canceled by the user
            await Task.WhenAny(new Task[] { loadCompletedEventTask, _contentDialogTask });
            _contentDialog.Hide();

            if (!_isLoaded)
            {
                // _contentDialogTask signaled, meaning that the dialog was canceled
                _isCanceled = true;

                // Complete the load call
                // (Null check shouldn't be necessary)
                if (_loadingThreadEvent != null)
                {
                    _loadingThreadEvent.Set();
                }

                return false;
            }

            return _isLoaded;
        }

    }



}
