
using System.IO;

namespace Tempo
{
    public class DotNetAppTypeSetLoader : TypeSetLoader
    {
        public DotNetAppTypeSetLoader(
            string[] additionalPaths)
            : base(useWinrtProjections: true,
                  additionalPaths,
                  DotNetTypeSet.StaticName)
        {
            // Not a nuget.org package, so have to calculate the cache location
            CacheDirectoryPath = Path.Combine(
                DesktopManager2.PackagesCachePath,
                DotNetTypeSet.StaticName,
                DotNetTypeSet.DotNetCoreVersion
                );

            // Creates intermediate path if necessary, doesn't fail if already exists
            Directory.CreateDirectory(CacheDirectoryPath);
        }
    }
}
