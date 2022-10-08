using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Text;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Tempo
{
    public sealed partial class SearchResult : Grid
    {
        public SearchResult()
        {
            this.InitializeComponent();

        }

        public FrameworkElement ConnectedAnimationElement {  get { return _textBlock; } }

        //public TypeViewModel Result { get; set; }

        // bugbug: Why is the change tracking necessary?  Value's getting set too soon?
        public BaseViewModel Result
        {
            get { return (BaseViewModel)GetValue(ResultProperty); }
            set { SetValue(ResultProperty, value); }
        }
        public static readonly DependencyProperty ResultProperty =
            DependencyProperty.Register("Result", typeof(BaseViewModel), typeof(SearchResult), 
                new PropertyMetadata(null, (s,e) => (s as SearchResult).ResultChanged()));

        void ResultChanged()
        {
            if (Result is TypeViewModel)
            {
                _textBlock.Margin = new Thickness(0,8,0,0);
                _textBlock.FontWeight = FontWeights.Bold;
            }
            else
            {
                _textBlock.Margin = new Thickness(10, 0, 0, 0);
                _textBlock.FontWeight = FontWeights.Normal;
            }
        }


    }
}
