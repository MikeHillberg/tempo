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

            SizeChanged += OnSizeChanged;
        }

        private async void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(e.NewSize != Size.Empty)
            {
                SizeChanged -= OnSizeChanged;


                // We're about to load up the WebView, which might take longer than a normal
                // page navigation. Show show a progress ring this time.
                IsInitialLoading = true;

                // Create and initialize the web view
                IsWebViewLoaded = true;

                await _webView.EnsureCoreWebView2Async();
                var wv2 = _webView.CoreWebView2;
                //wv2.Settings.UserAgent = "Tempo";

                //wv2.AddWebResourceRequestedFilter("*docs.microsoft.com*", CoreWebView2WebResourceContext.All);
                //wv2.WebResourceRequested += (_, args) =>
                //{
                //    Debug.WriteLine($"WRR: {args.Request.Uri}");

                //    Launcher.LaunchUriAsync(new Uri(args.Request.Uri));
                //};

                //wv2.NavigationStarting += (_, args) =>
                //{
                //    Debug.WriteLine(args.Uri.ToString());
                //    if (args.Uri.ToString().Contains("login.microsoftonline.com"))
                //    {
                //        args.Cancel = true;
                //        Foo(args.Uri);
                //    }
                //};
            }
        }

        void Foo(string uri)
        {
            var w = new Window();
            var wv = new WebView2();
            wv.Loaded += async (_, __) =>
            {
                await wv.EnsureCoreWebView2Async();

                wv.CoreWebView2.NavigationStarting += (_, args) =>
                {
                    var s = "";
                    if (args != null && args.Uri != null)
                    {
                        s = args.Uri;
                    }
                    Debug.WriteLine($"temp: {s}");
                };

                //wv.CoreWebView2.Settings.UserAgent = "Tempo";
                wv.CoreWebView2.Navigate(uri);
            };


            w.Activate();
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
