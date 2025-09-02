
namespace Tempo
{
    /// <summary>
    /// Loader for the WinAppSdk TypeSet
    /// </summary>
    public class WinAppTypeSetLoader : TypeSetLoader
    {
        public WinAppTypeSetLoader(
            WinAppSDKChannel channel,
            bool useWinrtProjections)
            : base(useWinrtProjections,
                  WinAppTypeSet.PackageName,
                  WinAppTypeSet.StaticName,
                  loadDependentPackages: true) // WinAppSDK is a metapackage
        {
            // WinAppSdk versions for preview & experimental have versions like
            // 1.2.3-preview1 and 1.2.3-experimental3

            if (channel == WinAppSDKChannel.Preview)
            {
                PrereleaseTag = "preview";
            }
            else if (channel == WinAppSDKChannel.Experimental)
            {
                PrereleaseTag = "experimental";
            }
        }
    }
}
