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
    internal class ApiScopeLoader
    {
        bool _isLoaded = false;
        bool _isCanceled = false;

        ManualResetEvent _loadCompletedEvent = null;
        ManualResetEvent _loadingEvent = null;

        // Load the scope by running an action off thread
        // Completed actions run on the UI thread. It's done here rather than in the async completion
        // to get them run at the right time.
        internal async void StartLoad(
            Action offThreadLoadAction,     // How to load (runs off thread)
            Action uiThreadCompletedAction, // What to do when load completes (runs on UI thread)
            Action uiThreadCanceledAction)  // What to do if load is canceled (runs on UI thread)
        {
            if(_isLoaded)
            {
                return;
            }

            if (_loadCompletedEvent != null)
            {
                // Already a load in progress
                return;
            }

            _loadingEvent = new ManualResetEvent(false);
            _loadCompletedEvent = new ManualResetEvent(false);

            _isCanceled = false;

            // Fork the load action
            _ = Task.Run(() =>
            {
                try
                {
                    offThreadLoadAction();

                    // Must set this before releasing the event
                    _isLoaded = true;
                }
                catch(Exception)
                {
                    _isLoaded = false;
                    _isCanceled = true;
                }

                // Run a completion on the UI thread
                _loadingEvent.Set();
            });

            // The event will be released either by the load completing or by the Ensure method
            // bugbug: there must be a better way to do this than forking another thread?
            await Task.Run(() => _loadingEvent.WaitOne());

            _loadingEvent = null;

            if (_isCanceled)
            {
                // The "loading ..." dialog was canceled
                _isLoaded = false;
                uiThreadCanceledAction();
            }
            else
            {
                // Completed successfully
                uiThreadCompletedAction();
            }

            _loadCompletedEvent.Set();
            _loadCompletedEvent = null;

            return;
        }

        Task _contentDialogTask = null;
        LoadingDialog _contentDialog = null;

        // Make sure loading has completed, showing a dialog if necessary
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
                _isCanceled = true;

                // Complete the load call
                _loadingEvent.Set();

                return false;
            }

            return _isLoaded;
        }

    }



}
