using System;
using System.Collections.Generic;
using System.Text;

namespace Tempo
{
    public class ClipboardHelper3
    {

        // Build a clipboard data string from HTML

        public static string BuildClipboardData(string htmlFragment, string title, Uri sourceUrl)
        {
            if (title == null) title = "From Clipboard";

            System.Text.StringBuilder htmlBuilder = new System.Text.StringBuilder();

            if (htmlFragment != null)
            {

                // Builds the CF_HTML header. See format specification here:
                // http://msdn.microsoft.com/library/default.asp?url=/workshop/networking/clipboard/htmlclipboard.asp

                // The string contains index references to other spots in the string, so we need placeholders so we can compute the offsets.
                // The <<<<<<<_ strings are just placeholders. We'll backpatch them actual values afterwards.
                // The string layout (<<<) also ensures that it can't appear in the body of the html because the <
                // character must be escaped.

                string header =

    @"Format:HTML Format
Version:1.0
StartHTML:<<<<<<<1
EndHTML:<<<<<<<2
StartFragment:<<<<<<<3
EndFragment:<<<<<<<4
StartSelection:<<<<<<<3
EndSelection:<<<<<<<3
";


                string pre =

    @"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">
<HTML><HEAD><TITLE>" + title + @"</TITLE></HEAD><BODY><!--StartFragment-->";

                string post = @"<!--EndFragment--></BODY></HTML>";

                htmlBuilder.Append(header);

                if (sourceUrl != null)
                {
                    htmlBuilder.AppendFormat("SourceURL:{0}", sourceUrl);
                }

                int startHTML = htmlBuilder.Length;
                htmlBuilder.Append(pre);

                int fragmentStart = htmlBuilder.Length;
                htmlBuilder.Append(htmlFragment);

                int fragmentEnd = htmlBuilder.Length;
                htmlBuilder.Append(post);

                int endHTML = htmlBuilder.Length;

                // Backpatch offsets
                htmlBuilder.Replace("<<<<<<<1", To8DigitString(startHTML));
                htmlBuilder.Replace("<<<<<<<2", To8DigitString(endHTML));
                htmlBuilder.Replace("<<<<<<<3", To8DigitString(fragmentStart));
                htmlBuilder.Replace("<<<<<<<4", To8DigitString(fragmentEnd));
            }

            return htmlBuilder.ToString();
        }

        // Helper to convert an integer into an 8 digit string.
        // String must be 8 characters, because it will be used to replace an 8 character string within a larger string.
        static string To8DigitString(int x)

        {
            return String.Format("{0,8}", x);
        }


    }
}
