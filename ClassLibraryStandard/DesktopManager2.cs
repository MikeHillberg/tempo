using CommonLibrary;
using Microsoft.Win32;
using MiddleweightReflection;
using NuGet.Common;
using NuGet.Packaging;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static MiddleweightReflection.MrLoadContext;

namespace Tempo
{
    public static class DesktopManager2
    {
        static string _windir = System.Environment.ExpandEnvironmentVariables("%SystemRoot%");
        static public string WinMDDir = FindSystemMetadataDirectory();

        // Location where it's OK to write files (package cache)
        static public string PackagesCachePath = null;

        // Bugbug: need to set this internally because UWP app gets confused by the type at build time
        //public static Dispatcher Dispatcher { get; set; }

        public static void Initialize(bool wpfApp, string packagesCachePath)
        {
            PackagesCachePath = packagesCachePath;

            if (wpfApp)
            {
                // Extras for the WPF app (vs the WinUI app)

                Manager.SLTypeSet = new SilverlightTypeSet("SL");
                Manager.WinmdTypeSet = new WinPlatTypeSet();
                Manager.CustomTypeSet = new CustomTypeSet();
                Manager.DotNetTypeSet = new DotNetTypeSet();
            }

            // Set a default version of the override helpers.
            // (This default is no longer changed by the projects)
            OverridableHelpers.Instance = new DefaultOverridableHelpers();


            Manager.Settings = new Settings();

            LoadWinLegacyAPIsAsync();

            // Initialize ReleaseInfo.VersionFriendlyNames
            // This has to be deferred to see if WinUI2 gets loaded
            ReleaseInfo.IntializeFriendlyVersionNames();

        }

        /// <summary>
        /// Get the version of the local machine in the form of the display version (e.g. 24H2) and build number.
        /// </summary>
        public static void GetLocalWindowsVersion(
            out string displayVersion,
            out string currentBuildNumber,
            out string updateBuildRevision)
        {
            displayVersion = "";
            currentBuildNumber = "";
            updateBuildRevision = "";

            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
            {
                if (key == null)
                {
                    return;
                }

                displayVersion = key.GetValue("DisplayVersion") as string;
                displayVersion = displayVersion ?? "";

                currentBuildNumber = key.GetValue("CurrentBuildNumber") as string;
                currentBuildNumber = currentBuildNumber ?? "";

                var ubrInt = key.GetValue("UBR");
                if (ubrInt != null && ubrInt is int)
                {
                    updateBuildRevision = ubrInt.ToString();
                }
            }
        }


        /// <summary>
        /// Load System32 types with MR reflection, using either a C++ projection or a C# projection
        /// </summary>
        static public TypeSet LoadWindowsTypes(
            bool useWinRTProjections,
            Func<string, string> assemblyLocator = null,
            string winUIWinMDFilename = null,
            string cacheFolder = null)
        {
            // Early out if already loaded
            if (Manager.WindowsTypeSet?.Types != null
                && useWinRTProjections == Manager.WindowsTypeSet.UsesWinRTProjections)
            {
                return Manager.WindowsTypeSet;
            }

            var typeSet = new MRTypeSet(
                WinPlatTypeSet.StaticName,
                useWinRTProjections);

            var projectionString = useWinRTProjections ? "c#" : "c++";
            DebugLog.Append($"Loading from {WinMDDir} for {projectionString}");

            // Create a LoadContext and set the callbacks that it needs
            var loadContext = new MrLoadContext(useWinRTProjections);
            loadContext.AssemblyPathFromName = (assemblyName) => ResolveMRAssembly(assemblyName, assemblyLocator); // Find an assembly
            loadContext.FakeTypeRequired += LoadContext_FakeTypeRequired;

            // Load from System32
            foreach (var winmdFile in Directory.EnumerateFiles(WinMDDir, @"*.winmd"))
            {
                var index = winmdFile.LastIndexOf('\\');
                if (index == -1)
                {
                    throw new Exception("Can't find system metadata files");
                }

                DebugLog.Append($"Loading: '{winmdFile}'");
                loadContext.LoadAssemblyFromPath(winmdFile);
                typeSet.AssemblyLocations.Add(new AssemblyLocation(winmdFile));
            }

            // Load the latest WinUI framework package
            //var winUIWinMDFilename = GetWinUIWinMDFilename();
            if (winUIWinMDFilename != null)
            {
                DebugLog.Append($"Loading from '{winUIWinMDFilename}'");
                loadContext.LoadAssemblyFromPath(winUIWinMDFilename);
                typeSet.AssemblyLocations.Add(new AssemblyLocation(winUIWinMDFilename));
            }

            // After everything's loaded you have to commit it before using it
            loadContext.FinishLoading();

            typeSet.Types = (from a in loadContext.LoadedAssemblies
                             from t in a.GetAllTypes()
                             where t.GetFullName() != "<Module>"
                             let tvm = MRTypeViewModel.GetFromCache(t, typeSet)
                             orderby tvm.Name
                             select tvm).ToList();

            (typeSet as MRTypeSet).SetIsWinmd(true);

            typeSet.Namespaces = Types2Namespaces.Convert(typeSet.Types);

            //// Set the version for WinUI
            SetWinUIVersions(winUIWinMDFilename, typeSet.Types);

            // This has to happen after the WinUI versions are set above
            //DesktopTypeSet.LoadContracts(typeSet);
            typeSet.LoadContracts();

            return typeSet;
        }



        /// <summary>
        /// Set the WinUI version on the WinUI types
        /// </summary>
        public static void SetWinUIVersions(string winUIWinMDFilename, IEnumerable<TypeViewModel> types)
        {
            if (!string.IsNullOrEmpty(winUIWinMDFilename))
            {
                // Quick & dirty; if the format somehow changes, just call it "WinUI"
                string winUIVersion = "WinUI";
                try
                {
                    var parts = winUIWinMDFilename.Split('\\');
                    winUIVersion = parts[parts.Length - 2];

                    parts = winUIVersion.Split('.');
                    if (parts.Length >= 5)
                    {
                        var parts2 = parts[4].Split('_');
                        winUIVersion = $"WinUI {parts[3]}.{parts2[0]}";
                    }
                }
                catch (Exception)
                {
                }

                // Set the version on the types, and the members will defer to there.
                foreach (var type in types)
                {
                    if (type.AssemblyLocation.Contains("Microsoft.UI.Xaml"))
                    {
                        type.SetVersion(ReleaseInfo.OldOSVersionWinUI);
                        type.SetContract(winUIVersion);

                        type.SetIsPreview(false);
                    }
                }
                ReleaseInfo.IntializeFriendlyVersionNames(winUIVersion);
            }
        }



        // Sync helper for the async version



        public static string FindSystemMetadataDirectory()
        {
            // On a 32 bit machine, WinMetadata is in \windows\system32\WinMetadata
            var winMDDir32 = _windir + @"\System32\WinMetadata\";

            // On a 64 bit machine, since this is a 32 bit app, \windows\system32 redirects to \windows\syswow64.
            // To get at the actual system32 directory, look in the secret \windows\SYSNATIVE.
            var winMDDir64 = _windir + @"\SysNative\WinMetadata\";

            // Find the real System32\WinMetadata directory
            string winMDDir = winMDDir32;
            if (!Directory.Exists(winMDDir))
            {
                // We're running on 64 bit machine
                winMDDir = winMDDir64;
            }

            return winMDDir;

        }

        // Command-line mode meaning started from the command-lind rather than launched from 
        // the Fusion .application file.
        static bool _commandLineMode = false;
        static public bool CommandLineMode
        {
            get { return _commandLineMode; }
            set
            {
                _commandLineMode = value;
                if (value)
                {
                    Settings.SyncMode = true;
                }
            }
        }

        // When in sync mode, do everything sync, rather than forking things to a worker thread.
        static bool _syncMode = false;
        static public bool SyncMode
        {
            get { return _syncMode || CommandLineMode; }
        }
        static public void SetSyncMode(bool value)
        {
            _syncMode = value;
            Settings.SyncMode = _syncMode || CommandLineMode;
        }





        static string ResolveMRAssembly(string assemblyName, Func<string, string> assemblyLocator = null)
        {
            var location = $@"{WinMDDir}\{assemblyName}.winmd";

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

        static private void LoadContext_FakeTypeRequired(object sender, MrLoadContext.FakeTypeRequiredEventArgs e)
        {
            // The Windows Platform SDK has naming mismatches between what's in the SDK and what's on
            // the system at runtime. For example Windows.Foundation types are in Windows.Foundation.WinMD at runtime
            // but Windows.Foundation.FoundationContract.WinMD in the SDK. If we have what looks like one of those
            // cases, just look for the type in all loaded assemblies.
            if (e.AssemblyName != null
                && !e.AssemblyName.StartsWith("Windows.")
                && !e.AssemblyName.Contains("Contract.")
                && !e.TypeName.StartsWith("Windows.Foundation."))
            {
                return;
            }

            var loadContext = sender as MrLoadContext;

            if (loadContext.TryFindMrType(e.TypeName, out var type))
            {
                e.ReplacementType = type;
            }
        }

        // Get the list of pre-Win10 APIs (we can't use contract names/versions to figure out the versions of these)
        public static void LoadWinLegacyAPIsAsync()
        {
            BackgroundHelper.DoWorkAsyncOld(
                () =>
                {
                    try
                    {
                        ImportedApis.Initialize(
                            ClassLibraryStandard.Properties.Resources.Win8,
                            ClassLibraryStandard.Properties.Resources.WinBlue,
                            ClassLibraryStandard.Properties.Resources.PhoneBlue,
                            null);
                    }
                    catch (Exception)
                    {
                    }
                },

                () =>
                {
                });
        }

        /// <summary>
        /// Get the dependencies of a pckage, as defined in the nuspec
        /// </summary>
        public static List<(string id, VersionRange versionRange)> GetDependenciesforNupkg(string packageFilename)
        {
            // Return value
            var dependencyNames = new List<(string, VersionRange)>();

            try
            {
                // Open the package
                using (var reader = new PackageArchiveReader(packageFilename))
                {
                    // Get the nuspec
                    var nuspecReader = reader.NuspecReader;

                    // Get the dependency groups from the nuspec
                    var dependencyGroups = nuspecReader?.GetDependencyGroups();
                    if (dependencyGroups != null)
                    {
                        // Find the highest version dependency group
                        var maxDependency = dependencyGroups.Where(d => d.Packages.Any()).Max();
                        if (maxDependency != null)
                        {
                            DebugLog.Append($"Checking dependencies for {packageFilename}");

                            // Get each dependency
                            foreach (var dependency in maxDependency.Packages)
                            {
                                dependencyNames.Add((dependency.Id, dependency.VersionRange));
                                DebugLog.Append($" - {dependency.Id}, {dependency.VersionRange}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                DebugLog.Append(e, $"Couldn't get dependencies for {packageFilename}");
            }

            return dependencyNames;
        }

        public static void LoadFromNupkg(
            string packageFilename,
            MRTypeSet typeSet,
            MrLoadContext loadContext,
            out bool hasWinmd)
        {
            DebugLog.Append($"Loading nupkg: '{packageFilename}'");
            hasWinmd = false;

            // Open the nupkg zip file
            ZipArchive packageZip;
            packageZip = ZipFile.OpenRead(packageFilename);

            // Find the winmds in the nupkg file
            var entryNames = new Dictionary<string, string>();
            using (packageZip)
            {
                bool isUap = false;
                foreach (var entry in packageZip.Entries)
                {
                    var entryName = entry.Name.ToLower();

                    // Bugbug: what exactly are the rules for the organization inside a .nupkg?
                    // Only looking in the lib directory (pattern I mostly see)
                    // plus the root directory (Win32Metadata is located here)
                    // I've seen the ZipFile API sometimes use slashes and sometimes whacks, 
                    // so checking for both
                    var fullName = entry.FullName.ToLower().Replace('\\', '/');
                    if (!fullName.StartsWith("lib/")      // Not in lib folder
                        && !fullName.StartsWith("metadata/") // Not in metadata folder (WinAppSDK 1.8 goes here)
                        && fullName.Contains("/"))        // Not at the root
                    {
                        continue;
                    }

                    //  We only care about winmds and dlls
                    var isWinmd = fullName.EndsWith(".winmd");
                    hasWinmd |= isWinmd;
                    var isDll = fullName.EndsWith(".dll");
                    if (!isWinmd && !isDll)
                    {
                        continue;
                    }

                    // Is this the first winmd in the package?
                    if (isWinmd && !isUap)
                    {
                        // We'll consider the whole package a Uap now and only look at winmds
                        // Throw away any dll names we've collecte
                        isUap = true;
                        entryNames.Clear();
                    }

                    // If we only care about winmds, then we don't care about dlls
                    if (isUap && isDll)
                    {
                        continue;
                    }

                    // See if this is a dup name
                    if (entryNames.TryGetValue(entryName, out var existing))
                    {
                        // Pick the dup with the larger full name.
                        // For example, net5.0-windows10.0.18362.0 is preferred over net5.0-windows10.0.17763.0
                        // bugbug: Should be more thorough about components, e.g. netframework vs netstandard vs net5.
                        if (entry.FullName.CompareTo(existing) > 0)
                        {
                            entryNames[entryName] = entry.FullName;
                        }
                    }
                    else
                    {
                        // First time we've seen this winmd
                        entryNames[entryName] = entry.FullName;
                    }
                }

                // Load all the DLLs or WinMDs we found
                foreach (var entryName in entryNames.Values)
                {
                    DebugLog.Append($"Loading from nupkg: '{entryName}'");
                    var entry = packageZip.GetEntry(entryName);
                    using (var entryStream = entry.Open())
                    {
                        var length = (int)entry.Length;
                        var buffer = new byte[length];

                        // In .Net5 the entryStream.Read doesn't return all the bytes requested,
                        // so need to loop until complete

                        var totalBytesRead = 0;
                        var bytesRead = -1;
                        while (bytesRead != 0)
                        {
                            bytesRead = entryStream.Read(buffer, totalBytesRead, length - totalBytesRead);
                            totalBytesRead += bytesRead;
                            if (totalBytesRead == length)
                            {
                                break;
                            }
                        }

                        // Add the assembly to the LoadContext
                        var assembly = loadContext.LoadAssemblyFromBytes(
                            buffer,
                            $"{packageFilename}!{entryName}");

                        if (assembly != null)
                        {
                            typeSet.AssemblyLocations.Add(new AssemblyLocation(entryName, packageFilename));
                        }
                        else
                        {
                            DebugLog.Append($"Not an assembly: {packageFilename}!{entryName}");
                        }
                    }
                }
            }
        }


        public class PackageLocationAndVersion
        {
            // For Nuget, this is the location of the first package, not the dependency packages
            public string PrimaryPath;
            public string Version;
        }

        // Download the latest version of a package, returning it's location as a directory name
        // (The package will be a file therein named {packageName}.nupkg)
        public static async Task<PackageLocationAndVersion> DownloadLatestPackageFromNugetToDirectory(
            string packageName,
            Task task, // can be null
            string prereleaseTag = null,
            VersionRange versionRange = null)
        {
            string nupkgFilename = null;

            try
            {
                ILogger logger = NullLogger.Instance;
                CancellationToken cancellationToken = CancellationToken.None;

                // Get all versions of the package
                SourceCacheContext cache = new SourceCacheContext();
                SourceRepository repository = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
                FindPackageByIdResource resource = await repository.GetResourceAsync<FindPackageByIdResource>();

                IEnumerable<NuGetVersion> versions = await resource.GetAllVersionsAsync(
                    packageName,
                    cache,
                    logger,
                    cancellationToken);

                if (versionRange == null)
                {
                    // Filter either to not a prerelease, or to the right prerelease (experimental vs preview)
                    if (string.IsNullOrEmpty(prereleaseTag))
                    {
                        versions = versions.Where(v => !v.IsPrerelease);
                    }
                    else
                    {
                        versions = versions.Where(v => v.OriginalVersion.Contains($"-{prereleaseTag}"));
                    }
                }
                else
                {
                    // Filter to the version range specified
                    versions = versions.Where(versionRange.Satisfies);
                }

                // bugbug: test
                if (task != null && task.IsCanceled)
                {
                    DebugLog.Append($"Download canceled for {packageName}");
                    return new PackageLocationAndVersion();
                }

                // Get the highest version from the filtered list
                NuGetVersion highestVersion = null;
                if (versions != null)
                {
                    highestVersion = versions.Max();
                }

                if (highestVersion == null)
                {
                    DebugLog.Append($"Couldn't find last version of {packageName}");
                    return new PackageLocationAndVersion();
                }
                DebugLog.Append($"{packageName} version is {highestVersion.OriginalVersion}");

                // Calculate the full path to where the nupkg will be downloaded
                // Something like {PackageCachePath}\WindowsAppSdk\1.7.0.0
                var packagePath = $@"{PackagesCachePath}\{packageName}\{highestVersion.OriginalVersion}";
                nupkgFilename = $@"{packagePath}\{packageName}.nupkg";

                // Create the directory if necessary, and early out if the package is already there
                if (Directory.Exists(packagePath))
                {
                    if (File.Exists(nupkgFilename))
                    {
                        return new PackageLocationAndVersion()
                        {
                            PrimaryPath = nupkgFilename,
                            Version = highestVersion.OriginalVersion
                        };
                    }
                }
                else
                {
                    Directory.CreateDirectory(packagePath);
                }

                // Downlaod the nupkg
                var packageUrl = $"https://www.nuget.org/api/v2/package/{packageName}/{highestVersion.OriginalVersion}";
                var http = (HttpWebRequest)WebRequest.Create(packageUrl);
                http.UserAgent = "Tempo"; // Server rejects the request if there's no UserAgent string set
                var response = await http.GetResponseAsync();
                var stream = response.GetResponseStream();

                // Write the nupkg to a file
                using (var fileStream = File.Create(nupkgFilename))
                {
                    stream.CopyTo(fileStream);
                    fileStream.Flush();
                }

                DebugLog.Append($"Downloaded {nupkgFilename}");

                return new PackageLocationAndVersion() { PrimaryPath = nupkgFilename, Version = highestVersion.OriginalVersion };
            }
            catch (Exception e)
            {
                DebugLog.Append(e, $"Couldn't download {packageName}");

                if (nupkgFilename != null)
                {
                    SafeDelete(nupkgFilename);
                }

                throw;
            }
        }


        public static void GetEntryFromZipFile(string zipFilename, string entryPath, string extractedFilename)
        {
            FileStream fileStream;
            // Open the nupkg and get out the file we want
            var zip = ZipFile.OpenRead(zipFilename);
            var entry = zip.GetEntry(entryPath); // @"Windows.Win32.winmd"
            var entryStream = entry.Open();

            // Write the zip stream to a file on disk
            fileStream = File.Create(extractedFilename);
            entryStream.CopyTo(fileStream);
            fileStream.Flush();
            fileStream.Close();

            // Delete the nupkg
            zip.Dispose();
            SafeDelete(zipFilename);
        }

        // Load a set of filenames into a TypeSet, using MR code
        public static void LoadTypeSetMiddleweightReflection(
            MRTypeSet typeSet,
            string[] typeSetFileNames,
            AssemblyPathFromNameCallback assemblyPathFromName = null,
            Task task = null)
        {
            try
            {
                var loadContext = new MrLoadContext(typeSet.UsesWinRTProjections);
                var resolver = new MRAssemblyResolver();

                if (assemblyPathFromName != null)
                {
                    // If the caller provided a callback to resolve assemblies, use that
                    loadContext.AssemblyPathFromName = assemblyPathFromName;
                }
                else
                {
                    // Otherwise use a general-purpose resolver
                    loadContext.AssemblyPathFromName = resolver.ResolveCustomAssembly;
                }

                loadContext.FakeTypeRequired += LoadContext_FakeTypeRequired;

                // Sleep for testing the dialog
                //Thread.Sleep(5000);

                bool isWinmd = false;

                foreach (var filename in typeSetFileNames)
                {
                    if (task != null && task.IsCanceled)
                    {
                        DebugLog.Append($"Load canceled for {typeSet.Name}");
                        return;
                    }

                    if (File.Exists(filename))
                    {
                        if (filename.EndsWith(".nupkg"))
                        {
                            DesktopManager2.LoadFromNupkg(filename, typeSet, loadContext, out var hasWinmd);
                            isWinmd |= hasWinmd;
                        }
                        else
                        {
                            // Either a .DLL or a .WinMD

                            resolver.DirectoryName = System.IO.Path.GetDirectoryName(filename);
                            DebugLog.Append($"Loading '{filename}'");
                            var assembly = loadContext.LoadAssemblyFromPath(filename);
                            if (assembly == null)
                            {
                                DebugLog.Append($"Not a metadata file: {filename}");
                            }
                            else
                            {
                                DebugLog.Append($"Loading {filename}");
                                typeSet.AssemblyLocations.Add(new AssemblyLocation(filename));
                            }

                            if (filename.ToLower().EndsWith(".winmd"))
                            {
                                isWinmd = true;
                            }
                        }
                    }
                    else
                    {
                        DebugLog.Append("Couldn't find custom file " + filename);
                    }
                }

                loadContext.FinishLoading();


                if (loadContext.LoadedAssemblies == null || loadContext.LoadedAssemblies.Count == 0)
                {
                    typeSet.Types = (new List<TypeViewModel>()).AsReadOnly();
                    return;
                }

                if (task != null && task.IsCanceled)
                {
                    DebugLog.Append($"Load canceled for {typeSet.Name} (2)");
                    return;
                }

                // We got something. Finish loading it up.

                var typeSetT = typeSet;
                typeSet.Types
                            = (from a in loadContext.LoadedAssemblies
                               from t in a.GetAllTypes()
                               where !t.GetNamespace().StartsWith("ABI.") // Ignore cswinrt implementation detail
                               let tvm = MRTypeViewModel.GetFromCache(t, typeSetT)
                               where tvm.Name != "<Module>"
                               orderby tvm.Name
                               select tvm).ToList();

                typeSet.Namespaces = Types2Namespaces.Convert(typeSet.Types);

                typeSet.SetIsWinmd(isWinmd);

                // Bugbug: need better detection for winmd

                // Find the list of contracts used by this TypeSet
                typeSet.LoadContracts();
            }
            catch (Exception e)
            {
                typeSet.Types = null;
                UnhandledExceptionManager.ProcessException(e);
            }

            return;
        }


        public static void LoadCustomWinMDFileNamesFromRegistry()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(_regKeyName);
                if (key != null)
                {
                    CustomApiScopeFileNames.Value = key.GetValue(_customWinMDRegKeyValue, null) as string[];
                }


            }
            catch (Exception e)
            {
                CustomApiScopeFileNames.Value = null;
                UnhandledExceptionManager.ProcessException(e);
            }
        }

        public static void SaveCustomWinMDFileNamesToRegistry()
        {
            var key = Registry.CurrentUser.CreateSubKey(_regKeyName);
            key.SetValue(_customWinMDRegKeyValue, CustomApiScopeFileNames.Value);

        }

        public static void ClearCustomWinMDFilNames()
        {
            CustomApiScopeFileNames.Value = null;
            var key = Registry.CurrentUser.CreateSubKey(DesktopManager2._regKeyName);

            // bugbug: In .Net6 this throws not-found even though it's there in regedit
            key.DeleteValue(_customWinMDRegKeyValue);
        }


        public static readonly ReifiedProperty<string[]> CustomApiScopeFileNames = new ReifiedProperty<string[]>();

        public const string _regKeyName = @"Software\ToolboxTempo";
        public const string _customWinMDRegKeyValue = "CustomWinMDs";
        public const string _custom2WinMDRegKeyValue = "Custom2WinMDs";

        static public bool HaveCustomFilenames
        {
            get
            {
                return CustomApiScopeFileNames.Value != null && CustomApiScopeFileNames.Value.Length != 0;
            }
        }

        /// <summary>
        /// Download the specified package from nuget, load it, and return it as a TypeSet
        /// </summary>
        internal static TypeSet LoadNugetHelper(
            string typeSetName,
            string cacheFolderName,
            bool useWinRTProjections,
            string packageName,
            Task task,
            string prereleasePrefix = "",
            bool loadDependencies = false)
        {
            DebugLog.Append($"Loading {packageName}, {prereleasePrefix}");

            // Download the nupkg from nuget.org (or return the cached copy)
            var packageLocationAndVersion = DownloadNugetPackageAndDependencies(
                packageName,
                prereleasePrefix,
                loadDependencies,
                task,
                out var dependencyFilenames);
            if (packageLocationAndVersion == null)
            {
                return null;
            }

            // Wherever the nupkg is, use that directory for caching other things
            var cachePath = Path.GetDirectoryName(packageLocationAndVersion.PrimaryPath);

            // Initialize the type set (this won't load anything yet)
            var typeSet = new MRTypeSet(
                typeSetName,
                useWinRTProjections,
                cachePath);

            typeSet.Version = $"{packageName},{packageLocationAndVersion.Version}";

            // Load the pimary nupkg as well as dependencies (if requested)
            // Catch exceptions so we can fail gracefully if there's any kind of expected exception cases that I don't know about
            var allPackageFilenames = new[] { packageLocationAndVersion.PrimaryPath }.Concat(dependencyFilenames).ToArray();
            try
            {
                // Load all the freshly downloaded packages into the TypeSet
                LoadTypeSetMiddleweightReflection(typeSet, allPackageFilenames);

                if (typeSet.Types != null)
                {
                    return typeSet;
                }
                else
                {
                    // Must be a corrupt download
                    throw new Exception("Couldn't open nupkg");
                }
            }
            catch (Exception)
            {
                // Maybe a corrupted download, so remove cached copies

                foreach (var filename in allPackageFilenames)
                {
                    try
                    {
                        SafeDelete(filename);
                    }
                    catch (Exception e)
                    {
                        DebugLog.Append(e, $"Couldn't delete {filename}");
                    }
                }

                throw;
            }
        }

        /// <summary>
        /// Download a nuget package from nuget.org, optionally as well as one level of dependencies
        /// </summary>
        public static PackageLocationAndVersion DownloadNugetPackageAndDependencies(
            string packageName,
            string prereleaseTag,
            bool loadDependencies,
            Task task, // can be null
            out string[] dependencyFilenames)
        {
            dependencyFilenames = null;

            // Download the nupkg from nuget.org (or use the cached copy)
            var downloadTask = DesktopManager2.DownloadLatestPackageFromNugetToDirectory(packageName, task, prereleaseTag);
            downloadTask.Wait();
            var packageLocationAndVersion = downloadTask.Result;
            if (packageLocationAndVersion == null || string.IsNullOrEmpty(packageLocationAndVersion.PrimaryPath))
            {
                // The user probably canceled the operation
                return null;
            }

            var packageFilename = packageLocationAndVersion.PrimaryPath;

            if (loadDependencies)
            {
                var packageFileNames = new List<string>() { };

                // Read out the nuspec and find out what dependencies this has
                var deps = GetDependenciesforNupkg(packageFilename);

                // Loop through the dependencies and download them too
                // (Just one level deep for now)
                foreach (var dep in deps)
                {
                    // Download the dependency package (synchronously)
                    downloadTask = DesktopManager2.DownloadLatestPackageFromNugetToDirectory(dep.id, task, prereleaseTag: null, dep.versionRange);
                    downloadTask.Wait();
                    packageLocationAndVersion = downloadTask.Result;
                    Debug.Assert(packageLocationAndVersion != null);

                    if (string.IsNullOrEmpty(packageLocationAndVersion.PrimaryPath))
                    {
                        // The user probably canceled the operation
                        return null;
                    }

                    packageFileNames.Add(packageLocationAndVersion.PrimaryPath);
                }

                // [out]
                dependencyFilenames = packageFileNames.ToArray();
            }

            return packageLocationAndVersion;
        }


        /// <summary>
        /// This is just a wrapper for File.Delete with an extra defense-in-depth check to make sure
        /// we're only deleting a Tempo file (has "tempo" in its name somewhere).
        /// </summary>
        static public void SafeDelete(string filename)
        {
            if (!filename.Contains("Tempo"))
            {
                DebugLog.Append($"Bad file delete: {filename}");
                throw new Exception($"Bad file delete: {filename}");
            }

            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
        }


        // Create a string with a type definition in C# syntax
        static public string GetCsTypeDefinition(TypeViewModel type, string msdnAddress)
        {
            var sb = new StringBuilder();

            if (type.IsDelegate)
            {
                sb.Append($"{type.CodeModifiers} delegate void {type} (");

                var firstParameter = true;
                foreach (var parameter in type.DelegateParameters)
                {
                    if (!firstParameter)
                        sb.Append(", ");
                    firstParameter = false;

                    sb.Append(parameter.ParameterType.CSharpName + " " + parameter.PrettyName);
                }
                sb.AppendLine(");");

                sb.AppendLine("");
                sb.AppendLine(msdnAddress);
                return sb.ToString();
            }

            if (type.IsFlagsEnum)
            {
                sb.AppendLine("[flags]");
            }

            sb.Append($"{type.CodeModifiers}");
            sb.Append($"{type}");

            bool finishedFirstLine = false;
            var baseType = type.BaseType;
            if (baseType != null && !baseType.ShouldIgnore)
            {
                sb.Append($" : {baseType.CSharpName}");
                finishedFirstLine = true;
            }

            //foreach (var iface in Type2Interfaces.GetInterfaces(Type))
            foreach (var iface in type.Interfaces)
            {
                if (!finishedFirstLine)
                {
                    sb.AppendLine(" :");
                    finishedFirstLine = true;
                }
                else
                {
                    sb.AppendLine(",");
                }

                sb.Append($"    {iface.PrettyName}");
            }

            sb.AppendLine("");
            sb.Append("{");

            var first = true;
            foreach (var constructor in type.Constructors)
            {
                if (first)
                {
                    sb.AppendLine("");
                    first = false;
                }

                sb.Append($"    {constructor.ModifierCodeString} {type.CSharpName} (");

                var firstParameter = true;
                foreach (var parameter in constructor.Parameters)
                {
                    if (!firstParameter)
                        sb.Append(", ");
                    firstParameter = false;

                    sb.Append(parameter.ParameterType.CSharpName + " " + parameter.PrettyName);
                }
                sb.AppendLine(")");
            }

            first = true;
            foreach (var prop in type.Properties)
            {
                if (prop.IsDependencyPropertyField)
                {
                    // Do DPs at the end
                    continue;
                }

                if (first)
                {
                    sb.AppendLine("");
                    first = false;
                }

                sb.Append($"    {prop.ModifierCodeString} {prop.PropertyType.CSharpName} {prop.PrettyName} {{");

                if (prop.CanRead)
                    sb.Append(" get; ");
                if (prop.CanWrite)
                    sb.Append(" set; ");
                sb.AppendLine("}");
            }

            first = true;
            foreach (var method in type.Methods)
            {
                if (first)
                {
                    sb.AppendLine("");
                    first = false;
                }

                sb.Append($"    {method.ModifierCodeString} {method.ReturnType.CSharpName} {method.PrettyName} (");

                var firstParameter = true;
                foreach (var parameter in method.Parameters)// GetParameters())
                {
                    if (!firstParameter)
                    {
                        sb.Append(", ");
                    }
                    firstParameter = false;

                    //sb.Append(TypeValueConverter.ToString(parameter.ParameterType) + " " + parameter.Name);
                    sb.Append($"{parameter.ParameterType} {parameter.Name}");
                }
                sb.AppendLine(")");
            }

            first = true;
            foreach (var ev in type.Events)
            {
                if (first)
                {
                    sb.AppendLine("");
                    first = false;
                }

                sb.Append($"    {ev.ModifierCodeString} {ev.EventHandlerType.CSharpName} {ev.PrettyName};");

                sb.AppendLine("");
            }

            first = true;
            foreach (var f in type.Fields)
            {
                if (first)
                {
                    sb.AppendLine("");
                    first = false;
                }

                if (type.IsEnum)
                {
                    sb.Append($"    {f.PrettyName} = {f.RawConstantValueString},");
                }
                else
                {
                    sb.Append($"    {f.ModifierCodeString} {f.FieldType.CSharpName} {f.PrettyName};");
                }

                sb.AppendLine("");
            }

            // Write out DependencyProperties (they got skipped in the property section above)
            first = true;
            foreach (var prop in type.Properties)
            {
                if (!prop.IsDependencyPropertyField)
                {
                    continue;
                }

                if (first)
                {
                    sb.AppendLine("");
                    first = false;
                }

                sb.Append($"    {prop.ModifierCodeString} {prop.PropertyType.CSharpName} {prop.PrettyName} {{");

                if (prop.CanRead)
                    sb.Append(" get; ");
                if (prop.CanWrite)
                    sb.Append(" set; ");
                sb.AppendLine("}");
            }


            sb.AppendLine("}");
            sb.AppendLine("");

            sb.AppendLine(msdnAddress);

            //Clipboard.SetText(sb.ToString());
            return sb.ToString();
        }



    }

    public enum WinAppSDKChannel
    {
        Stable,
        Preview,
        Experimental
    }

}
