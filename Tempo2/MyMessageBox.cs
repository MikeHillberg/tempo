using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    // Simple wrapper to show a traditional message box
    internal class MyMessageBox
    {
        internal static async Task<ContentDialogResult> Show(
            string message,
            string title = null,
            bool isOKEnabled = false,
            bool isCloseEnabled = false)
        {
            var contentDialog = new ContentDialog()
            {
                XamlRoot = App.MainPage.XamlRoot,
                Content = new StackPanel()
                {
                    Children =
                    {
                        new TextBlock() { Text = message }
                    }
                },
                Title = title
            };

            if(isOKEnabled)
            {
                contentDialog.PrimaryButtonText = "OK";
            }

            if(isCloseEnabled)
            {
                contentDialog.SecondaryButtonText = "Cancel";
            }

            contentDialog.CloseButtonText = "Close";

            var result = await contentDialog.ShowAsync();
            return result;
        }
    }
}
