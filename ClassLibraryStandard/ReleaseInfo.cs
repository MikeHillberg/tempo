using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tempo
{
    public class ReleaseInfo
    {
        public static bool IsKnownVersion(string version)
        {
            switch (version)
            {
                case OldOSVersionWin8:
                case OldOSVersionWinBlue:
                case OldOSVersionPhoneBlue:
                case OldOSVersionTH1:
                case OldOSVersionTH2:
                case OldOSVersionRS1:
                case OldOSVersionRS2:
                case OldOSVersionRS3:
                case OldOSVersionRS4:
                case OldOSVersionRS5:
                case OldOSVersion19h1:
                case OldOSVersion20h1:
                case OldOSVersion21h2:
                case OldOSVersion11:
                case OldOSVersionWinUI:
                    return true;

                default:return false;
            }
        }

        // Convert e.g. "0A000005" to "Anniversay Update ..."
        public static string FriendlyBuildNameFromUglyVersionName(string version)
        {
            var build = "";

            if (!string.IsNullOrEmpty(version))
            {
                if (version.StartsWith("06")) // Win8, WinBlue, PhoneBlue
                    build = "10240";
                else if (version == ReleaseInfo.OldOSVersionTH1)
                    build = "10240";
                else if (version == ReleaseInfo.OldOSVersionTH2)
                    build = "10586";
                else if (version == ReleaseInfo.OldOSVersionRS1)
                    build = "14393 (Anniversary Update)";
                else if (version == ReleaseInfo.OldOSVersionRS2)
                    build = "15063 (Creators Update)";
                else if (version == ReleaseInfo.OldOSVersionRS3)
                    build = "16299 (Fall Creators Update)";
                else if (version == ReleaseInfo.OldOSVersionRS4)
                    build = "17134 (1803)";
                else if (version == ReleaseInfo.OldOSVersionRS5)
                    build = "17763 (1809)";
                else if (version == ReleaseInfo.OldOSVersion19h1)
                    build = "18362 (1903)";
                else if (version == ReleaseInfo.OldOSVersion20h1)
                    build = "19041 (2004)";
                else if (version == ReleaseInfo.OldOSVersion21h2)
                    build = "20348 (2104)";
                else if (version == ReleaseInfo.OldOSVersion11)
                    build = "22000";
                else if (version == ReleaseInfo.OldOSVersionWinUI)
                    build = "WinUI";
                else
                    build = PreviewBuildString;
            }

            return build;
        }

        public static string PreviewBuildString = "";//"(Prerelease)";

        public static List<string> TH1Contracts = new List<string>();
        public static List<string> TH2Contracts = new List<string>();
        public static List<string> RS1Contracts = new List<string>();
        public static List<string> RS2Contracts = new List<string>();
        public static List<string> RS3Contracts = new List<string>();
        public static List<string> RS4Contracts = new List<string>();
        public static List<string> RS5Contracts = new List<string>();
        public static List<string> NineteenH1Contracts = new List<string>();
        public static List<string> TwentyH1Contracts = new List<string>();
        public static List<string> TwentyOneH2Contracts = new List<string>();
        public static List<string> ElevenContracts = new List<string>();

        public static string GetVersionFromContract(string contract)
        {
            // Contracts don't have contracts, so assume TH1 for now until I think can think of something better
            if (string.IsNullOrEmpty(contract))
                return OldOSVersionTH1;

            else if (TH1Contracts.Contains(contract))
                return OldOSVersionTH1;
            else if (TH2Contracts.Contains(contract))
                return OldOSVersionTH2;
            else if (RS1Contracts.Contains(contract))
                return OldOSVersionRS1;
            else if (RS2Contracts.Contains(contract))
                return OldOSVersionRS2;
            else if (RS3Contracts.Contains(contract))
                return OldOSVersionRS3;
            else if (RS4Contracts.Contains(contract))
                return OldOSVersionRS4;
            else if (RS5Contracts.Contains(contract))
                return OldOSVersionRS5;
            else if (NineteenH1Contracts.Contains(contract))
                return OldOSVersion19h1;
            else if (TwentyH1Contracts.Contains(contract))
                return OldOSVersion20h1;
            else if (TwentyOneH2Contracts.Contains(contract))
                return OldOSVersion21h2;
            else if (ElevenContracts.Contains(contract))
                return OldOSVersion11;
            else
                return OldOSVersionFuture;

        }


        public static List<string> GetContractsFromBuildNumber(string version)
        {
            if (version == BuildNumberTH1)
                return TH1Contracts;
            else if (version == BuildNumberTH2)
                return TH2Contracts;
            else if (version == BuildNumberRS1)
                return RS1Contracts;
            else if (version == BuildNumberRS2)
                return RS2Contracts;
            else if (version == BuildNumberRS3)
                return RS3Contracts;
            else if (version == BuildNumberRS4)
                return RS4Contracts;
            else if (version == BuildNumberRS5)
                return RS5Contracts;
            else if (version == BuildNumber19h1)
                return NineteenH1Contracts;
            else if (version == BuildNumber20h1)
                return TwentyH1Contracts;
            else if (version == BuildNumber21h2)
                return TwentyOneH2Contracts;
            else if (version == BuildNumber11)
                return ElevenContracts;

            return null;
        }


        //
        // For a new release, also update ContractInformation class
        //

        public static string BuildNumberTH1 = "10240";
        public static string BuildNumberTH2 = "10586";
        public static string BuildNumberRS1 = "14393";
        public static string BuildNumberRS2 = "15063";
        public static string BuildNumberRS3 = "16299";
        public static string BuildNumberRS4 = "17134";
        public static string BuildNumberRS5 = "17763";
        public static string BuildNumber19h1 = "18362";
        public static string BuildNumber20h1 = "19041";
        public static string BuildNumber21h2 = "20348";
        public static string BuildNumber11 = "22000";

        // The first 3 are real, the rest are made up just to have a unique number internally
        public const string OldOSVersionWin8 = "06020000";
        public const string OldOSVersionWinBlue = "06030000";
        public const string OldOSVersionPhoneBlue = "06030100";
        public const string OldOSVersionTH1 = "0A000000";
        public const string OldOSVersionTH2 = "0A000001";
        public const string OldOSVersionRS1 = "0A000002";
        public const string OldOSVersionRS2 = "0A000003";
        public const string OldOSVersionRS3 = "0A000004";
        public const string OldOSVersionRS4 = "0A000005";
        public const string OldOSVersionRS5 = "0A000006";
        public const string OldOSVersion19h1 = "0A000007";
        public const string OldOSVersion20h1 = "0A000008";
        public const string OldOSVersion21h2 = "0A000009";
        public const string OldOSVersion11 = "0A00000A";
        public const string OldOSVersionFuture = "0AFFFFFF";
        public const string OldOSVersionWinUI = "0AFFFFFE";
        // When updating here, also update VersionIsUnknown property

        static public void IntializeFriendlyVersionNames(string winUIVersion = null)
        {
            VersionFriendlyNames.Clear();

            VersionFriendlyNames.Add(OldOSVersionWin8, "8.0 (6.2)");
            VersionFriendlyNames.Add(OldOSVersionWinBlue, "8.1 (6.3)");
            VersionFriendlyNames.Add(OldOSVersionPhoneBlue, "Phone 8.1 (6.3.1)");
            VersionFriendlyNames.Add(OldOSVersionTH1, $"10 ({BuildNumberTH1}, TH1)");
            VersionFriendlyNames.Add(OldOSVersionTH2, $"10 ({BuildNumberTH2}, TH2)");
            VersionFriendlyNames.Add(OldOSVersionRS1, $"1607, Anniversary Update ({BuildNumberRS1}, RS1)");
            VersionFriendlyNames.Add(OldOSVersionRS2, $"1703, Creators Update ({BuildNumberRS2}, RS2)");
            VersionFriendlyNames.Add(OldOSVersionRS3, $"1709, Fall Creators Update ({BuildNumberRS3}, RS3)");
            VersionFriendlyNames.Add(OldOSVersionRS4, $"1803, Windows 10 April 2018 Update ({BuildNumberRS4}, RS4)");
            VersionFriendlyNames.Add(OldOSVersionRS5, $"1809, October 2018 Update ({BuildNumberRS5}, RS5)");
            VersionFriendlyNames.Add(OldOSVersion19h1, $"1903, May 2019 Update ({BuildNumber19h1}, 19h1)");
            VersionFriendlyNames.Add(OldOSVersion20h1, $"2004, May 2020 Update ({BuildNumber20h1}, 20h1)");
            VersionFriendlyNames.Add(OldOSVersion21h2, $"2104, Windows Server 2022 ({BuildNumber21h2}, 21h2)");
            VersionFriendlyNames.Add(OldOSVersion11, $"Windows 11 ({BuildNumber11})"); //$"21H2, Windows 11 ({BuildNumber11}, 21h2)");

            if (winUIVersion != null)
            {
                VersionFriendlyNames.Add(OldOSVersionWinUI, winUIVersion);
            }

            VersionFriendlyNames.Add(OldOSVersionFuture, _prereleaseVersionName);

            _versionFriendlyNameValues = AnyVersionFriendlyName.Union(VersionFriendlyNames.Values);
        }

        static string _prereleaseVersionName = "(Prerelease)";


        public static string[] AnyVersionFriendlyName = { "Any version" };
        static IEnumerable<string> _versionFriendlyNameValues = null;

        static public IEnumerable<string> VersionFriendlyNameValues
        {
            get
            {
                return _versionFriendlyNameValues;
            }
        }



        static public Dictionary<string, string> VersionFriendlyNames = new Dictionary<string, string>();
        static public void UpdateFriendlyVersionNames(string key, string value)
        {
            if (!VersionFriendlyNames.ContainsKey(key))
            {
                VersionFriendlyNames.Add(key, value);
                _versionFriendlyNameValues = AnyVersionFriendlyName.Union(VersionFriendlyNames.Values);
            }
        }



    }
}
