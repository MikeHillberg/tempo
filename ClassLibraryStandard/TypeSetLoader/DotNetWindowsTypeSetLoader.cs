

using System.IO;

namespace Tempo
{
    public class DotNetWindowsTypeSetLoader : TypeSetLoader
    {
        public DotNetWindowsTypeSetLoader(
            string[] additionalPaths)
            : base(useWinrtProjections: true,
                  additionalPaths,
                  DotNetWindowsTypeSet.StaticName)
        {
            // Not a nuget.org package, so have to calculate the cache location
            CacheDirectoryPath = Path.Combine(
                DesktopManager2.PackagesCachePath,
                DotNetWindowsTypeSet.StaticName,
                DotNetTypeSet.DotNetCoreVersion
                );

            // Creates intermediate path if necessary, doesn't fail if already exists
            Directory.CreateDirectory(CacheDirectoryPath);
        }
    }
}
