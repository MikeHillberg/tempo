// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Windows.ApplicationModel.DataTransfer;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo
{

    /// <summary>
    /// CommandBar that can be used anywhere (configurable)
    /// </summary>
    public sealed partial class CommonCommandBar : UserControl
    {
        public CommonCommandBar()
        {
            this.InitializeComponent();
            this.Loaded += CommonCommandBar_Loaded;
        }

        private void CommonCommandBar_Loaded(object sender, RoutedEventArgs e)
        {
            ShowTeachingTips();
        }



        public bool CanGoBack
        {
            get
            {
                return App.CanGoBack;
            }
        }

        public bool IsBackEnabled
        {
            get { return (bool)GetValue(IsBackEnabledProperty); }
            set { SetValue(IsBackEnabledProperty, value); }
        }
        public static readonly DependencyProperty IsBackEnabledProperty =
            DependencyProperty.Register("IsBackEnabled", typeof(bool), typeof(CommonCommandBar),
                new PropertyMetadata(true, (s, e) => (s as CommonCommandBar).IsBackEnabledChanged()));

        private void IsBackEnabledChanged()
        {
            ShowHome = IsBackEnabled;
        }



        //public bool IsDocsPaneEnabled
        //{
        //    get { return (bool)GetValue(IsDocsPaneEnabledProperty); }
        //    set { SetValue(IsDocsPaneEnabledProperty, value); }
        //}
        //public static readonly DependencyProperty IsDocsPaneEnabledProperty =
        //    DependencyProperty.Register("IsDocsPaneEnabled", typeof(bool), typeof(CommonCommandBar), new PropertyMetadata(false));


        public bool ShowHome
        {
            get { return (bool)GetValue(ShowHomeProperty); }
            private set { SetValue(ShowHomeProperty, value); }
        }

        public static readonly DependencyProperty ShowHomeProperty =
            DependencyProperty.Register("ShowHome", typeof(bool), typeof(CommonCommandBar), new PropertyMetadata(true));


        public Visibility UpButtonVisibility
        {
            get { return (Visibility)GetValue(UpButtonVisibilityProperty); }
            set { SetValue(UpButtonVisibilityProperty, value); }
        }
        public static readonly DependencyProperty UpButtonVisibilityProperty =
            DependencyProperty.Register("UpButtonVisibility", typeof(Visibility), typeof(CommonCommandBar),
                new PropertyMetadata(Visibility.Collapsed));


        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            UpButtonClick?.Invoke(null, null);
        }

        public event EventHandler UpButtonClick;

        public Visibility FilterVisibility
        {
            get { return (Visibility)GetValue(FilterVisibilityProperty); }
            set { SetValue(FilterVisibilityProperty, value); }
        }
        public static readonly DependencyProperty FilterVisibilityProperty =
            DependencyProperty.Register("FilterVisibility", typeof(Visibility), typeof(CommonCommandBar), new PropertyMetadata(Visibility.Collapsed));

        public bool IsExportVisible
        {
            get { return (bool)GetValue(IsExportVisibleProperty); }
            set { SetValue(IsExportVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsExportVisibleProperty =
            DependencyProperty.Register("IsExportVisible", typeof(bool), typeof(CommonCommandBar), new PropertyMetadata(false));



        public bool IsScopeVisible
        {
            get { return (bool)GetValue(IsScopeVisibleProperty); }
            set { SetValue(IsScopeVisibleProperty, value); }
        }
        public static readonly DependencyProperty IsScopeVisibleProperty =
            DependencyProperty.Register("IsScopeVisible", typeof(bool), typeof(CommonCommandBar), new PropertyMetadata(false));



        public string ApiLabel
        {
            get { return (string)GetValue(ApiLabelProperty); }
            set { SetValue(ApiLabelProperty, value); }
        }
        public static readonly DependencyProperty ApiLabelProperty =
            DependencyProperty.Register("ApiLabel", typeof(string), typeof(CommonCommandBar), new PropertyMetadata("Windows"));

        private void CopyToClipboardAsNameNamespace(object sender, RoutedEventArgs e)
        {
            var content = CopyExport.ConvertItemsToABigString(Results, asCsv: false, flat: false); // this.Settings.Flat);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void CopyToClipboardAsFlat(object sender, RoutedEventArgs e)
        {
            var content = CopyExport.ConvertItemsToABigString(Results, asCsv: false, flat: true);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void CopyToClipboardGroupedByNamespace(object sender, RoutedEventArgs e)
        {
            var content = CopyExport.ConvertItemsToABigString(Results, asCsv: false, flat: false, groupByNamespace: true);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        private void CopyToClipboardCompact(object sender, RoutedEventArgs e)
        {
            CopyToClipboardCompactHelper(Results);
        }

        public static void CopyToClipboardCompactHelper(IList<MemberOrTypeViewModelBase> results)
        {
            var content = CopyExport.ConvertItemsToABigString(results, asCsv: false, flat: false, groupByNamespace: true, compressTypes: true);
            var dataPackage = new DataPackage();
            dataPackage.SetText(content);
            Clipboard.SetContent(dataPackage);
        }

        public IList<MemberOrTypeViewModelBase> Results
        {
            get { return (IList<MemberOrTypeViewModelBase>)GetValue(ResultsProperty); }
            set { SetValue(ResultsProperty, value); }
        }
        public static readonly DependencyProperty ResultsProperty =
            DependencyProperty.Register("Results", typeof(IList<MemberOrTypeViewModelBase>), typeof(CommonCommandBar), new PropertyMetadata(null));

        private void OpenInExcel(object sender, RoutedEventArgs e)
        {
            if (Results == null || Results.FirstOrDefault() == null)
            {
                return;
            }

            var items = from object i in Results
                        select i as BaseViewModel;

            var csv = CopyExport.GetItemsAsCsv(items);

            if (!CopyExport.OpenInExcel(csv, out var errorMessage))
            {
                _ = MyMessageBox.Show(
                        $"{errorMessage}\n\nPutting onto clipboard instead",
                        "Failed to open results in Excel");
                Utils.SetClipboardText(csv);
            }

        }

        void ShowTeachingTips()
        {
            // Filter button
            var shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.Filters, _root, _filterButton,
                () => new TeachingTip()
                {
                    Title = "Filter your search",
                    Subtitle = "Reduce results by filtering what you search. For example only search properties, or ignore base types. The Home button (Alt+Home) resets everything.",
                });
            if (!shouldContinue)
                return;

            // API scope selector
            // This is a separate method because it has an extra test/demo feature
            shouldContinue = ShowScopeTeachingTip();
            if (!shouldContinue)
                return;

            // New window button
            shouldContinue = TeachingTips.TryShow(
                TeachingTipIds.NewWindow, _root, _newWindowButton,
                () => new TeachingTip()
                {
                    Title = NewWindowTipText.Title,
                    Subtitle = NewWindowTipText.Subtitle
                });
            if (!shouldContinue)
                return;

        }

        // Content for the teach tip and tool tip of the New Window button
        (string Title, string Subtitle) NewWindowTipText = (
            "Create a new window (Ctrl+Shift+N)",
            "Clone this window to a second copy");


        bool ShowScopeTeachingTip(bool force = false)
        {
            return TeachingTips.TryShow(
                TeachingTipIds.ApiScopeSwitcher, _root, _apiScopeButton,
                () => new TeachingTip()
                {
                    Title = ApiScopeTipText.Title,
                    Subtitle = ApiScopeTipText.Subtitle
                },
                force: force);

        }

        (string Title, string Subtitle) ApiScopeTipText = (
            "Change your API scope", 
            "Change the APIs you're searching, for example search WinAppSDK rather than Windows APIs");



        // bugbug
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.GoBack();
        }

        private void AppBarButton_Home(object sender, RoutedEventArgs e)
        {
            App.ResetAndGoHome();
        }

        private void GoToMsdn(object sender, RoutedEventArgs e)
        {
            App.GoToMsdn();
        }

        private void CopyRichTextToClipboard(object sender, RoutedEventArgs e)
        {
            App.CopyMsdnLink(asMarkdown: false);
        }

        private void CopyMarkdownToClipboard(object sender, RoutedEventArgs e)
        {
            App.CopyMsdnLink(asMarkdown: true);
        }

        /// <summary>
        /// Member being displayed, used to enable MSDN
        /// </summary>
        public MemberOrTypeViewModelBase MemberVM
        {
            get { return (MemberOrTypeViewModelBase)GetValue(MemberVMProperty); }
            set { SetValue(MemberVMProperty, value); }
        }
        public static readonly DependencyProperty MemberVMProperty =
            DependencyProperty.Register("MemberVM", typeof(MemberOrTypeViewModelBase), typeof(CommonCommandBar),
                new PropertyMetadata(null, (s, e) => (s as CommonCommandBar).MemberVMChanged()));

        private void MemberVMChanged()
        {
            MsdnVisibility = (MemberVM == null) ? Visibility.Collapsed : Visibility.Visible;

            // Use this to always keep a global copy of what's being viewed
            App.CurrentItem = MemberVM;
        }



        public Visibility MsdnVisibility
        {
            get { return (Visibility)GetValue(MsdnVisibilityProperty); }
            set { SetValue(MsdnVisibilityProperty, value); }
        }

        public static readonly DependencyProperty MsdnVisibilityProperty =
            DependencyProperty.Register("MsdnVisibility", typeof(Visibility), typeof(CommonCommandBar), new PropertyMetadata(Visibility.Collapsed));

        private void GoToDocs_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            App.GoToMsdn();
        }

        private void SelectWindowsApis(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsWinPlatformScope)
            {
                return;
            }

            App.Instance.IsWinPlatformScope = true;
            App.Instance.GotoSearch();
        }

        private void SelectWinAppSdkApis(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsWinAppScope)
            {
                return;
            }

            App.Instance.IsWinAppScope = true;
            App.Instance.GotoSearch();
        }

        private void SelectWin32Apis(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsWin32Scope)
            {
                return;
            }

            App.Instance.IsWin32Scope = true;
            App.Instance.GotoSearch();
        }

        private void SelectDotNetApis(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsDotNetScope)
            {
                return;
            }

            App.Instance.IsDotNetScope = true;
            App.Instance.GotoSearch();
        }

        private void SelectDotNetWindowsApis(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsDotNetWindowsScope)
            {
                return;
            }

            App.Instance.IsDotNetWindowsScope = true;
            App.Instance.GotoSearch();
        }


        private void SelectWebView2Apis(object sender, RoutedEventArgs e)
        {
            if (App.Instance.IsWebView2Scope)
            {
                return;
            }

            App.Instance.IsWebView2Scope = true;
            App.Instance.GotoSearch();
        }


        private void SelectCustomApiScope(object sender, RoutedEventArgs e)
        {
            App.Instance.IsCustomApiScope = true;
            App.Instance.GotoSearch();


            // Might need to show the file picker

        }

        private void ShowHelp(object sender, RoutedEventArgs e)
        {
            App.Instance.ShowHelp();
        }

        private void _apiScopeButton_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
        {
            // Show the API scope button teaching tip (just here to be demo-able)
            ShowScopeTeachingTip(force: true);
        }

        /// <summary>
        /// Create a new window (really, a new process), with the same state as the current one
        /// </summary>
        private void NewWindow(object sender, RoutedEventArgs e)
        {
            // We'll launch "tempo:..." to launch the new process.
            // Pass all the state as parameters. Basically
            // "tempo:button?selection=type:Windows.UI.Xaml.Controls&settings=[json]"

            // Encode the search string
            var encodedSearchText = WebUtility.UrlEncode(App.Instance.SearchText);

            // Pass the name of the current selected item so that that can be
            // selected again in the new window

            string currentItemString = "";
            var currentItem = App.CurrentItem;
            if(currentItem != null)
            {
                if(currentItem is TypeViewModel currentType)
                {
                    currentItemString = $"type:{currentType.FullName}";
                }
                else if(currentItem is MemberViewModelBase currentMember)
                {
                    currentItemString = $"member:{currentMember.FullName}";
                }
            }
            currentItemString = $"selection={WebUtility.UrlEncode(currentItemString)}";

            // Carry the Settings across as Json
            var settings = Manager.Settings.ToJson();
            settings = $"settings={WebUtility.UrlEncode(settings)}";

            var uri = new Uri($"tempo:{encodedSearchText}?{currentItemString}&{settings}");
            _ = Launcher.LaunchUriAsync(uri);
        }
    }
}
