
using System;
using System.IO;

namespace Tempo
{
    /// <summary>
    /// Loads the Windows Platform SDK type set
    /// </summary>
    public class WinPlatTypeSetLoader : TypeSetLoader
    {
        public WinPlatTypeSetLoader(
            bool useWinrtProjections,
            string[] allPaths,
            string version)
            : base(useWinrtProjections,
                  allPaths,
                  WinPlatTypeSet.StaticName)
        {
            // Cache path is calculated if a nuget.org package, otherwise must be set here
            CacheDirectoryPath = Path.Combine(
                DesktopManager2.PackagesCachePath,
                "System32", // Ordinarily the package name
                version
                );

            // Creates intermediate path if necessary, doesn't fail if already exists
            Directory.CreateDirectory(CacheDirectoryPath);

            AssemblyPathFromNameCallback = (assemblyName) => ResolveMRAssembly(assemblyName, null);
        }

        /// <summary>
        /// Help the metadata loader find assemblies by looking in the System32 WinMD directory
        /// </summary>
        static string ResolveMRAssembly(string assemblyName, Func<string, string> assemblyLocator = null)
        {
            var location = $@"{DesktopManager2.WinMDDir}\{assemblyName}.winmd";

            if (File.Exists(location))
            {
                DebugLog.Append("Loading " + location);
                return location;
            }
            else if (assemblyLocator != null)
            {
                return assemblyLocator(assemblyName);
            }
            else
            {
                DebugLog.Append($"Couldn't find assembly for namespace '{assemblyName}'");
                return null;
            }
        }
    }
}
