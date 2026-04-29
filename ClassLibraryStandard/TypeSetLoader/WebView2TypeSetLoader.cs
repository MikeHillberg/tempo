
namespace Tempo
{
    public class WebView2TypeSetLoader : TypeSetLoader
    {
        public WebView2TypeSetLoader(
            WebView2Channel channel,
            bool useWinrtProjections)
            : base(useWinrtProjections,
                  WebView2TypeSet.PackageName,
                  WebView2TypeSet.StaticName)
        {
            if (channel == WebView2Channel.Preview)
            {
                PrereleaseTag = "prerelease";
            }
        }
    }
}
