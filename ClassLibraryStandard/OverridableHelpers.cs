using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    // These are helpers for CommonLibrary to call but can't because it's built against the Portable .Net
    public class OverridableHelpers
    {
        public static IEnumerable<string> EmptyStringEnumerable = new List<string>(0);

        public static OverridableHelpers Instance { get; set; }

        // We need this because Portable .Net's WebRequest can't set the UserAgent.
        public virtual Task<StringReader> GetUriAsync(string uriString)
        {
            return Task<StringReader>.FromResult(new StringReader(""));
        }

        // From a query of the method markdown files, get out the filenames. Need this here
        // because .Net Portable doesn't have great JQuery support.
        public virtual IEnumerable<string> GetMethodMarkdownFilenamesFromQuery(StringReader reader)
        {
            return EmptyStringEnumerable;
        }
    }
}
