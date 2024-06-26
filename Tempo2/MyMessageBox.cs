﻿using Microsoft.UI.Xaml.Controls;
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
            string closeButtonText = null)
            //bool isOKEnabled = false,
            //bool isCancelEnabled = false)
        {
            var contentDialog = new ContentDialog()
            {
                XamlRoot = App.HomePage.XamlRoot,
                Content = new StackPanel()
                {
                    Children =
                    {
                        new TextBlock() { Text = message }
                    }
                },
                Title = title
            };

            //if(isOKEnabled)
            //{
            //    contentDialog.PrimaryButtonText = "OK";
            //}

            //if(isCancelEnabled)
            //{
            //    contentDialog.SecondaryButtonText = "Cancel";
            //}

            if(closeButtonText == null)
            {
                contentDialog.CloseButtonText = "Close";
            }
            else
            {
                contentDialog.CloseButtonText = closeButtonText;
            }

            var result = await contentDialog.ShowAsync();
            return result;
        }
    }
}
