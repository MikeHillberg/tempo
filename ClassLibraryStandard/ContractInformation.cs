using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Tempo
{
    static public class ContractInformation
    {

        public static void Load()
        {
            try
            {
                LoadContracts(ClassLibraryStandard.Properties.Resources.UapPreviousPlatforms, SdkPlatform.Universal);
                LoadContracts(ClassLibraryStandard.Properties.Resources.DesktopPreviousPlatforms, SdkPlatform.Desktop);
                LoadContracts(ClassLibraryStandard.Properties.Resources.IoTPreviousPlatforms, SdkPlatform.Iot);
                LoadContracts(ClassLibraryStandard.Properties.Resources.MobilePreviousPlatforms, SdkPlatform.Mobile);
                LoadContracts(ClassLibraryStandard.Properties.Resources.TeamPreviousPlatforms, SdkPlatform.Team);
            }
            catch (Exception)
            {
                // bugbug
            }
        }

        static public Dictionary<SdkPlatform, List<string>> ContractsPerPlatform = new Dictionary<SdkPlatform, List<string>>();

        static void LoadContracts(string xml, SdkPlatform sdkPlatform)
        {
            var platformContracts = new List<string>();

            var doc = XDocument.Load(new StringReader(xml));

            XNamespace ns = "http://microsoft.com/schemas/Windows/SDK/PreviousPlatforms";
            var applicationPlatforms = doc.Descendants(ns + "ApplicationPlatform");

            foreach (var applicationPlatform in applicationPlatforms)
            {
                var version = applicationPlatform.Attribute("version").Value;

                // If this throws, we're catching anyway
                var parts = version.Split('.');
                version = parts[2];

                List<string> contracts = ReleaseInfo.GetContractsFromBuildNumber(version);

                if (contracts == null)
                {
                    Debug.Assert(false);
                    continue;
                }

                // Following line isn't always working for me, something about the namespaces isn't working
                // var apiContracts = applicationPlatform.Descendants(ns + "ApiContract");
                // Workaround is to get everything and the filter:
                var descendents = applicationPlatform.Descendants();
                foreach (var descendent in descendents)
                {
                    if (descendent.Name.LocalName != "ApiContract")
                        continue;

                    var contractName = descendent.Attribute("name").Value;
                    parts = contractName.Split('.');
                    contractName = parts[parts.Length - 1];

                    var contractVersion = descendent.Attribute("version").Value;
                    parts = contractVersion.Split('.');
                    contractVersion = parts[0];

                    var contract = $"{contractName}, {contractVersion}";
                    contracts.Add(contract);
                    platformContracts.Add(contract);
                }
            }

            ContractsPerPlatform[sdkPlatform] = platformContracts;
        }

    }
}
