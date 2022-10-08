using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class CommonAppBar : CommandBar
    {
        public CommonAppBar()
        {
            this.InitializeComponent();
        }
        private void AppBarButton_Home(object sender, RoutedEventArgs e)
        {
            App.GoHome();
        }

        public bool IsFilterEnabled
        {
            get { return FilterVisibility == Visibility.Visible; }
            set { FilterVisibility = value ? Visibility.Visible : Visibility.Collapsed; }
        }

        private void GoToMsdn(object sender, RoutedEventArgs e)
        {
            //var msdnAddress = @"http://msdn.microsoft.com/en-us/library/windows/apps/xaml/" 
            //        + MemberVM.MsdnRelativePath;


            var t = Launcher.LaunchUriAsync(new Uri(MsdnHelper.CalculateWinMDMsdnAddress(MemberVM)));
        }

        private async void GotoSamples(object sender, RoutedEventArgs e)
        {
            var uri = ViewHelpers.GetSearchSampleUri(MemberVM);
            await Launcher.LaunchUriAsync(new Uri(uri));
            Debug.WriteLine("Launched");
        }
        private void GotoSamplesConcordance(object sender, RoutedEventArgs e)
        {
            var uri = ViewHelpers.GetIndexedSampleUri(MemberVM);
            var t = Launcher.LaunchUriAsync(new Uri(uri));
        }


        public MemberViewModel MemberVM
        {
            get { return (MemberViewModel)GetValue(MemberVMProperty); }
            set { SetValue(MemberVMProperty, value); }
        }
        public static readonly DependencyProperty MemberVMProperty =
            DependencyProperty.Register("MemberVM", typeof(MemberViewModel), typeof(CommonAppBar), 
                new PropertyMetadata(null, (s,e) => (s as CommonAppBar).MemberVMChanged()));

        private void MemberVMChanged()
        {
            MsdnVisibility = (MemberVM == null) ? Visibility.Collapsed : Visibility.Visible;
        }

        public Visibility MsdnVisibility
        {
            get { return (Visibility)GetValue(MsdnVisibilityProperty); }
            set { SetValue(MsdnVisibilityProperty, value); }
        }
        public static readonly DependencyProperty MsdnVisibilityProperty =
            DependencyProperty.Register("MsdnVisibility", typeof(Visibility), typeof(CommonAppBar), new PropertyMetadata(Visibility.Collapsed));




        public Visibility HomeVisibility
        {
            get { return (Visibility)GetValue(HomeVisibilityProperty); }
            set { SetValue(HomeVisibilityProperty, value); }
        }
        public static readonly DependencyProperty HomeVisibilityProperty =
            DependencyProperty.Register("HomeVisibility", typeof(Visibility), typeof(CommonAppBar), new PropertyMetadata(Visibility.Visible));





        public Visibility FilterVisibility
        {
            get { return (Visibility)GetValue(FilterVisibilityProperty); }
            set { SetValue(FilterVisibilityProperty, value); }
        }
        public static readonly DependencyProperty FilterVisibilityProperty =
            DependencyProperty.Register("FilterVisibility", typeof(Visibility), typeof(CommonAppBar), new PropertyMetadata(Visibility.Collapsed));


        private void SendFeedback(object sender, RoutedEventArgs e)
        {
            MainPage.SendFeedbackHelper();
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }



}
