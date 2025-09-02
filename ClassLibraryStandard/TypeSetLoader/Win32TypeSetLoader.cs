
namespace Tempo
{
    public class Win32TypeSetLoader : TypeSetLoader
    {
        public Win32TypeSetLoader(bool useWinrtProjections)
            : base(useWinrtProjections,
                  Win32TypeSet.PackageName,
                  Win32TypeSet.StaticName)
        {
            PrereleaseTag = "preview";
        }
    }
}