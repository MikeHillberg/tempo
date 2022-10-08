using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;

namespace Tempo
{
    // bugbug: share with desktop
    public static class ExtensionHelpers
    {
        static public void Add( this InlineCollection inlines, string text)
        {
            inlines.Add( new Run() { Text = text });
        }

        static public void AddWithSearchHighlighting(
            this InlineCollection inlines, 
            string str,
            TypeViewModel hyperlinkTarget = null )
        {
            SearchHighlighter.InsertSearchHighlightedString(inlines, str, hyperlinkTarget);
        }


    }
}
