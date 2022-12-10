using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Tempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ColorsIllustration : Page
    {
        public ColorsIllustration()
        {
            this.InitializeComponent();

            var colorSamples = new List<ColorSample>();

            // mikehill_ua: Got an error here because Microsoft.UI using hadn't been added
            var members = typeof(Colors).GetTypeInfo().DeclaredMembers;
            foreach (var member in members)
            {
                var prop = member as PropertyInfo;

                // Ignore the member if it's not a property or a special property
                // Unfortunately there's a bunch of special properties that aren't IsSpecialName
                if (prop == null
                    || prop.IsSpecialName
                    || prop.Name == "ThisPtr"
                    || prop.Name.StartsWith("_")
                    || prop.Name.Contains(".")
                    )
                {
                    continue;
                }

                var sample = new ColorSample()
                {
                    ColorName = prop.Name,
                    ColorValue = (Color)prop.GetValue(null)
                };
                colorSamples.Add(sample);
            }

            ColorSamples = colorSamples;
        }

        public static string Description => "Colors from the Windows.UI.Colors class";


        public IList<ColorSample> ColorSamples
        {
            get { return (IList<ColorSample>)GetValue(ColorSamplesProperty); }
            set { SetValue(ColorSamplesProperty, value); }
        }
        public static readonly DependencyProperty ColorSamplesProperty =
            DependencyProperty.Register("ColorSamples", typeof(IList<ColorSample>), typeof(ColorsIllustration), new PropertyMetadata(null));

        private void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
        {
            App.Navigate(TypeViewModel.LookupByName("Microsoft.UI.Colors"));
        }
    }

    public class ColorSample
    {
        public Color ColorValue { get; set; }
        public string ColorName { get; set; }
    }
}
