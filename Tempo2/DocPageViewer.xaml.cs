// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using System;
using Windows.System;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo
{
    /// <summary>
    /// Control that shows an API's documentation page in a WebView
    /// </summary>
    public sealed partial class DocPageViewer : UserControl
    {
        public DocPageViewer()
        {
            this.InitializeComponent();

            // Don't create the WebView unless/until necessary (wait for non-zero size)
            SizeChanged += OnSizeChanged1;
        }
        private void OnSizeChanged1(object sender, SizeChangedEventArgs e)
        {
            // If we initialize the WebView too soon during load it's no op'd,
            // so wait a beat
            DispatcherQueue.TryEnqueue(() => OnSizeChanged2(null, e));
        }

        private async void OnSizeChanged2(object sender, SizeChangedEventArgs e)
        {
            if(e.NewSize != Size.Empty)
            {
                SizeChanged -= OnSizeChanged1;

                // We're about to load up the WebView, which might take longer than a normal
                // page navigation. Show show a progress ring this time.
                IsInitialLoading = true;

                // Create and initialize the web view
                IsWebViewLoaded = true;

                await _webView.EnsureCoreWebView2Async();
                var wv2 = _webView.CoreWebView2;

                // Inject JS on every page load to forward keyboard shortcuts back to the app.
                // WebView2 captures keyboard input and doesn't bubble it out,
                // so app accelerators (like Ctrl+Shift+M) don't work when focus is in the WebView.
                var accelScript = @"
                    document.addEventListener('keydown', (e) => {
                        // Only send when a modifier is held and a non-modifier key is pressed
                        const modifierKeys = ['Control', 'Shift', 'Alt', 'Meta'];
                        if ((e.ctrlKey || e.altKey) && !modifierKeys.includes(e.key)) {
                            window.chrome.webview.postMessage(JSON.stringify({
                                type: 'accel',
                                keyCode: e.keyCode,
                                ctrl: e.ctrlKey,
                                shift: e.shiftKey,
                                alt: e.altKey
                            }));
                        }
                    });
                ";

                // Register for all future navigations
                await wv2.AddScriptToExecuteOnDocumentCreatedAsync(accelScript);

                // Also inject into the current page immediately
                await wv2.ExecuteScriptAsync(accelScript);

                wv2.WebMessageReceived += (s, args) =>
                {
                    try
                    {
                        // Use TryGetWebMessageAsString because the JS sends a string via JSON.stringify().
                        // WebMessageAsJson would double-encode it as a JSON string value.
                        var message = args.TryGetWebMessageAsString();
                        if (string.IsNullOrEmpty(message))
                            return;

                        var json = System.Text.Json.JsonDocument.Parse(message);
                        var root = json.RootElement;

                        if (root.GetProperty("type").GetString() != "accel")
                            return;

                        var keyCode = root.GetProperty("keyCode").GetInt32();
                        var ctrl = root.GetProperty("ctrl").GetBoolean();
                        var shift = root.GetProperty("shift").GetBoolean();
                        var alt = root.GetProperty("alt").GetBoolean();

                        var modifiers = VirtualKeyModifiers.None;
                        if (ctrl) modifiers |= VirtualKeyModifiers.Control;
                        if (shift) modifiers |= VirtualKeyModifiers.Shift;
                        if (alt) modifiers |= VirtualKeyModifiers.Menu;

                        var virtualKey = (VirtualKey)keyCode;

                        App.TryInvokeAccelerator(virtualKey, modifiers);
                    }
                    catch { }
                };
            }
        }

        /// <summary>
        /// True between NavigationStarting and NavigationCompleted
        /// </summary>
        public bool IsNavigating
        {
            get { return (bool)GetValue(IsNavigatingProperty); }
            set { SetValue(IsNavigatingProperty, value); }
        }
        public static readonly DependencyProperty IsNavigatingProperty =
            DependencyProperty.Register("IsNavigating", typeof(bool), typeof(DocPageViewer), 
                new PropertyMetadata(false));

        /// <summary>
        /// True the first time we get displayed, while the WebView is being created
        /// </summary>
        public bool IsInitialLoading
        {
            get { return (bool)GetValue(IsInitialLoadingProperty); }
            set { SetValue(IsInitialLoadingProperty, value); }
        }
        public static readonly DependencyProperty IsInitialLoadingProperty =
            DependencyProperty.Register("IsInitialLoading", typeof(bool), typeof(DocPageViewer), 
                new PropertyMetadata(false));

        /// <summary>
        /// Triggers the x:Load to create the WebView2
        /// </summary>
        public bool IsWebViewLoaded
        {
            get { return (bool)GetValue(IsWebViewLoadedProperty); }
            set { SetValue(IsWebViewLoadedProperty, value); }
        }
        public static readonly DependencyProperty IsWebViewLoadedProperty =
            DependencyProperty.Register("IsWebViewLoaded", typeof(bool), typeof(DocPageViewer), 
                new PropertyMetadata(false));


        /// <summary>
        /// This is set when a navigation fails
        /// </summary>
        public bool HasNavigationError
        {
            get { return (bool)GetValue(HasNavigationErrorProperty); }
            set { SetValue(HasNavigationErrorProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HasNavigationError.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HasNavigationErrorProperty =
            DependencyProperty.Register("HasNavigationError", typeof(bool), typeof(DocPageViewer), 
                new PropertyMetadata(false));


        private void WebView2_NavigationStarting(
            WebView2 sender, 
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
        {
            // Display the smoke layer
            IsNavigating = true;
        }

        private void WebView2_NavigationCompleted(
            WebView2 sender, 
            Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
        {
            // Clear the smoke layer
            IsNavigating = false;

            // Put up a no-doc message if it failed. Redirects show up as a failure though,
            // but Unknown is the case where the page doesn't exist.
            HasNavigationError = !args.IsSuccess 
                && args.WebErrorStatus == CoreWebView2WebErrorStatus.Unknown;

            // We turned on a progress ring when the WebView was being created.
            // Turn it off now (for good)
            IsInitialLoading = false;
        }
    }
}
