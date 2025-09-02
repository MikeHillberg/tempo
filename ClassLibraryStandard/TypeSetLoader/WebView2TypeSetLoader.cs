
namespace Tempo
{
    public class WebView2TypeSetLoader : TypeSetLoader
    {
        public WebView2TypeSetLoader(
            bool useWinrtProjections)
            : base(useWinrtProjections,
                  WebView2TypeSet.PackageName,
                  WebView2TypeSet.StaticName)
        {
            PrereleaseTag = "prerelease";
        }
    }
}
