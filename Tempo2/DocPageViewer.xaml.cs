// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;

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

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(e.NewSize != Size.Empty)
            {
                SizeChanged -= OnSizeChanged;


                // We're about to load up the WebView, which might take longer than a normal
                // page navigation. Show show a progress ring this time.
                IsInitialLoading = true;

                // Create and initialize the web view
                IsWebViewLoaded = true;
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

            // Put up a no-doc message if it failed
            HasNavigationError = !args.IsSuccess;

            // We turned on a progress ring when the WebView was being created.
            // Turn it off now (for good)
            IsInitialLoading = false;
        }
    }
}
