using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;

namespace Tempo;

/// <summary>
/// UserControl to show a list of filenames with delete buttons
/// </summary>
public sealed partial class FilenameList : UserControl
{
    public FilenameList()
    {
        this.InitializeComponent();
    }

    public string[] Filenames
    {
        get { return (string[])GetValue(FilenamesProperty); }
        set { SetValue(FilenamesProperty, value); }
    }
    public static readonly DependencyProperty FilenamesProperty =
        DependencyProperty.Register("Filenames", typeof(string[]), typeof(FilenameList), new PropertyMetadata(null));

    /// <summary>
    /// TextElement type doesn't have a Tag property, so add one
    /// </summary>
    public static object GetTextElementTag(DependencyObject obj)
    {
        return (object)obj.GetValue(TextElementTagProperty);
    }
    public static void SetTextElementTag(DependencyObject obj, object value)
    {
        obj.SetValue(TextElementTagProperty, value);
    }
    public static readonly DependencyProperty TextElementTagProperty =
        DependencyProperty.RegisterAttached("Tag", typeof(object), typeof(FilenameList),
                new PropertyMetadata(null));

    /// <summary>
    /// Split an array of filenames into name/path tuples
    /// </summary>
    IEnumerable<SplitFilename> SplitFilenames(string[] filenames)
    {
        var splitFilenames = new List<SplitFilename>();
        if (filenames == null)
        {
            // First time startup
            return splitFilenames;
        }

        foreach (var filename in filenames)
        {
            if (string.IsNullOrEmpty(filename))
            {
                continue;
            }

            var index = filename.LastIndexOf('\\');

            splitFilenames.Add(new SplitFilename()
            {
                FilePart = filename.Substring(index + 1),
                PathPart = filename.Substring(0, index)
            });
        }

        return splitFilenames;
    }

    public event EventHandler<string> FilenameRemoved;

    private void RemoveFile_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        var split = GetTextElementTag(sender) as SplitFilename;
        var path = $@"{split.PathPart}\{split.FilePart}";

        FilenameRemoved?.Invoke(this, path);
    }
}
