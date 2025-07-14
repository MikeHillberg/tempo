using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo;

public sealed partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
    }



    public bool SlowSearchInProgress
    {
        get { return (bool)GetValue(SlowSearchInProgressProperty); }
        set { SetValue(SlowSearchInProgressProperty, value); }
    }
    public static readonly DependencyProperty SlowSearchInProgressProperty =
        DependencyProperty.Register("SlowSearchInProgress", typeof(bool), typeof(StatusBar), new PropertyMetadata(false));



    public int SearchDelay
    {
        get { return (int)GetValue(SearchDelayProperty); }
        set { SetValue(SearchDelayProperty, value); }
    }
    public static readonly DependencyProperty SearchDelayProperty =
        DependencyProperty.Register("SearchDelay", typeof(int), typeof(StatusBar), new PropertyMetadata(0));

    private void ShowDebugLog_Click(object sender, RoutedEventArgs e)
    {
        DebugLogViewer.Show();
    }

    public bool ShowingSearchResults
    {
        get { return (bool)GetValue(ShowingSearchResultsProperty); }
        set { SetValue(ShowingSearchResultsProperty, value); }
    }
    public static readonly DependencyProperty ShowingSearchResultsProperty =
        DependencyProperty.Register("ShowingSearchResults", typeof(bool), typeof(StatusBar), new PropertyMetadata(false));
}
