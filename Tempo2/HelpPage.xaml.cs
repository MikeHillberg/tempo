// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Tempo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HelpPage : Page
    {
        public HelpPage()
        {
            this.InitializeComponent();

            _markdown.ImageResolving += _markdown_ImageResolving;

            LoadMarkdownAsync();
        }

        async void LoadMarkdownAsync()
        {
            var uri = new Uri("ms-appx:///Assets/Help.md");
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            var openFile = await file.OpenReadAsync();
            var markdown = await openFile.ReadTextAsync(); // Toolkit extension

            _markdown.Text = markdown;
        }

        /// <summary>
        /// Find an image from a relative URL and return as a BitmapImage
        /// </summary>
        private void _markdown_ImageResolving(object sender, CommunityToolkit.WinUI.UI.Controls.ImageResolvingEventArgs e)
        {
            // All the image references are relative URIs to the package
            var uri = new Uri($"ms-appx:///{e.Url}");

            var bi = new BitmapImage(uri);
            e.Image = bi;

            e.Handled = true;
        }

        //
        // This was an experiment to find a hack at getting anchor links to work, but it didn't work out :(
        //
        //private void _markdown_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
        //{
        //    if (e.Link.StartsWith("#"))
        //    {
        //        var link = e.Link.Replace("#", "")
        //                         .Replace("-", " ")
        //                         .Trim()
        //                         .ToLower();

        //        var element = sender as FrameworkElement;
        //        var child = VisualTreeHelper.GetChild(element, 0) as FrameworkElement;
        //        while (child != null)
        //        {
        //            if (child is StackPanel)
        //            {
        //                break;
        //            }
        //            child = VisualTreeHelper.GetChild(child, 0) as FrameworkElement;
        //        }
        //        if (child == null)
        //        {
        //            return;
        //        }

        //        foreach (var entry in (child as StackPanel).Children)
        //        {
        //            // Only looking at RichTextBlocks
        //            var rtb = entry as RichTextBlock;
        //            if (rtb == null)
        //            {
        //                continue;
        //            }

        //            // Look at the RTB's first Block
        //            var blocks = rtb.Blocks;
        //            if (blocks.Count == 0)
        //            {
        //                continue;
        //            }

        //            // It should be a Paragraph
        //            var paragraph = blocks[0] as Paragraph;
        //            if (paragraph == null)
        //            {
        //                continue;
        //            }

        //            // The Paragraph shouldn't be empty
        //            var inlines = paragraph.Inlines;
        //            if (inlines.Count == 0)
        //            {
        //                continue;
        //            }

        //            // Get the text
        //            var t = (inlines[0] as Run)?.Text;
        //            if (string.IsNullOrEmpty(t)) continue;

        //            if (t.Trim().ToLower() == link)
        //            {
        //                rtb.StartBringIntoView();
        //                return;
        //            }

        //        }

        //    }

        //}
    }
}
