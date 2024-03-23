using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Tempo
{
    // The OverridableHelpers concept was created to be able to share between WPF and UWP.
    // Between WPF and WinAppSDK we don't need the abstraction, so this is used by both now,
    // and the abstraction could go away.
    public class DefaultOverridableHelpers : OverridableHelpers
    {
        async public override Task<StringReader> GetUriAsync(string uriString)
        {
            // This isn't very robust to errors, it's all wrapped in a try/catch

            var http = (HttpWebRequest)WebRequest.Create(uriString);

            // GitHub APIs return 403 (Forbidden) if the request header doesn't have a user agent specified
            http.UserAgent = "Tempo";

            try
            {
                var response = await http.GetResponseAsync();

                var stream = response.GetResponseStream();
                var streamReader = new StreamReader(stream);
                var content = streamReader.ReadToEnd();

                return new StringReader(content);
            }
            catch (Exception e)
            {
                DebugLog.Append($"Can't get doc page: {e.GetType().ToString()}, '{e.Message}'");
                return new StringReader("");
            }
        }

        // Get the markdown filenames for method pages from a GitHub query. This is necessary because we have to search
        // for the filenames, since I don't know how to generate the correct filename.
        public override IEnumerable<string> GetMethodMarkdownFilenamesFromQuery(StringReader reader)
        {
            // Sometimes we don't get anything back from GitHub
            var rawBytes = Encoding.UTF8.GetBytes(reader.ReadToEnd());
            if(rawBytes.Length == 0)
            {
                yield break;
            }

            // Example query
            // https://api.github.com/search/code?q=HttpResponseMessage+repo:MicrosoftDocs/winrt-api+path:/windows.web.http/+filename:httpresponsemessage_close_*

            /* Produces this result (looking for the 'name' value):
                {
                  "total_count": 2,
                  "incomplete_results": false,
                  "items": [
                    {
                      "name": "cameraintrinsics_distortpoints_88187186.md",
                      "path": "windows.media.devices.core/cameraintrinsics_distortpoints_88187186.md",
                      "sha": "11e023a0fb5305efe28c8caab282a6ffa9f45e05",
                      "url": "https://api.github.com/repositories/79375310/contents/windows.media.devices.core/cameraintrinsics_distortpoints_88187186.md?ref=3d6d229aedf888920778dcd04b15f41025ca45ef",
                      "git_url": "https://api.github.com/repositories/79375310/git/blobs/11e023a0fb5305efe28c8caab282a6ffa9f45e05",
                      "html_url": "https://github.com/MicrosoftDocs/winrt-api/blob/3d6d229aedf888920778dcd04b15f41025ca45ef/windows.media.devices.core/cameraintrinsics_distortpoints_88187186.md",
              ...

            */

            // Load the Json from string
            var jsonReader = JsonReaderWriterFactory.CreateJsonReader(
                rawBytes,
                new System.Xml.XmlDictionaryReaderQuotas());

            // Convert Json to XML
            var root = XElement.Load(jsonReader);

            // Return the 'name' element for each item in the 'items' array
            foreach (var element in root.Elements("items").First().Elements())
            {
                yield return element.Elements("name").First().Value;
            }
        }


    }
}
