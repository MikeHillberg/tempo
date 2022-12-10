using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SymbolsIllustration : Page
    {
        public SymbolsIllustration()
        {
            this.InitializeComponent();

            var colorSamples = new List<SymbolSample>();

            var fields = typeof(Symbol).GetTypeInfo().DeclaredFields;

            foreach (var field in fields)
            {
                if (field.Name == "value__")
                    continue;

                var sample = new SymbolSample()
                {
                    SymbolName = field.Name,
                    SymbolValue = (Symbol)field.GetValue(null)
                };
                colorSamples.Add(sample);
            }

            SymbolSamples = colorSamples;
        }

        public IList<SymbolSample> SymbolSamples
        {
            get { return (IList<SymbolSample>)GetValue(SymbolSamplesProperty); }
            set { SetValue(SymbolSamplesProperty, value); }
        }
        public static readonly DependencyProperty SymbolSamplesProperty =
            DependencyProperty.Register("SymbolSamples", typeof(IList<SymbolSample>), typeof(SymbolsIllustration), new PropertyMetadata(null));

        private void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            App.Navigate(TypeViewModel.LookupByName("Microsoft.UI.Xaml.Controls.Symbol"));
        }

        public static string Description => "Symbols from the Windows.UI.Xaml.Controls.Symbol enum";
    }

    public class SymbolSample
    {
        public Symbol SymbolValue { get; set; }
        public string SymbolName { get; set; }
    }
}
