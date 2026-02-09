using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static MiddleweightReflection.MrLoadContext;

namespace Tempo
{
    /// <summary>
    /// Code to load a TypeSet. This is a base class, there's a derived for each type set
    /// </summary>
    public class TypeSetLoader
    {
        bool _useWinrtProjections = false;
        string[] _allPaths = null;
        string _version;
        string _typeSetName;
        bool _loadDependentPackages = false;
        string _packageName;
        readonly Version _badVersion = new Version(0, 0);
        List<string> _oldCachePaths = new List<string>();

        // This allows a load to preempt a preload
        TaskCompletionSource<object> _loadTaskCompletionSource = new TaskCompletionSource<object>();

        /// <summary>
        /// Constructor for type sets that are a nuget.org package, caller has to provide the package name
        /// </summary>
        public TypeSetLoader(
            bool useWinrtProjections,
            string packageName,
            string typeSetName,
            bool loadDependentPackages = false)
        {
            _packageName = packageName;
            _useWinrtProjections = useWinrtProjections;
            _typeSetName = typeSetName;
            _loadDependentPackages = loadDependentPackages;
        }

        /// <summary>
        /// Constructor for type sets that aren't a nuget.org package, caller has to provide the paths
        /// </summary>
        public TypeSetLoader(
            bool useWinrtProjections,
            string[] additionalPaths,
            string typeSetName)
        {
            _allPaths = additionalPaths;
            _typeSetName = typeSetName;
            _useWinrtProjections = useWinrtProjections;
        }

        public void ResetProjections(
            bool useWinrtProjections)
        {
            _useWinrtProjections = useWinrtProjections;
        }

        /// <summary>
        /// Optional callback for metadata reader load context
        /// </summary>
        protected AssemblyPathFromNameCallback AssemblyPathFromNameCallback = null;

        /// <summary>
        /// Optional prerelease tag for nuget.org packages
        /// </summary>
        protected string PrereleaseTag = "";

        /// <summary>
        /// Location of this type set's cach folder.
        /// This is calculated during download if the package comes from nuget.org,
        /// or specified by the subclass otherwise
        /// </summary>
        protected string CacheDirectoryPath;


        /// <summary>
        /// Ensure that the latest package is downloaded (Task can be null)
        /// </summary>
        void EnsureDownload(Task task)
        {
            // Check for already downloaded
            if (_allPaths != null)
            {
                return;
            }

            _version = null;
            _allPaths = null;

            if (task != null && task.IsCanceled)
            {
                // We're in a preload and a real load is pending
                DebugLog.Append($"Aborting preload for {_typeSetName}");
                return;
            }

            Tempo.DesktopManager2.PackageLocationAndVersion packageLocationAndVersion = null;
            string[] dependencyFilenames = null;

            try
            {
                // Figure out the latest version and download it, optionally with dependencies
                packageLocationAndVersion = DesktopManager2.DownloadNugetPackageAndDependencies(
                    _packageName,
                    PrereleaseTag,
                    _loadDependentPackages,
                    task,
                    out dependencyFilenames);
            }
            catch(Exception ex)
            {
                DebugLog.Append($"Couldn't download {_packageName}");
                DebugLog.Append(ex);

                // bugbug: show an error UI somehow
            }

            if (packageLocationAndVersion != null)
            {
                var primaryPath = packageLocationAndVersion.PrimaryPath;

                // The path will be \path\to\foopkg\1.2.3\foopkg.1.2.3.nupkg
                // Set the cache location to \path\to\foopkg\1.2.3
                CacheDirectoryPath = Path.GetDirectoryName(primaryPath);

                _version = packageLocationAndVersion.Version;

                _allPaths = new string[] { primaryPath };

                if (_loadDependentPackages && dependencyFilenames != null && dependencyFilenames.Length > 0)
                {
                    // If we have dependencies, add them to the list of additional paths
                    _allPaths = _allPaths.Concat(dependencyFilenames).ToArray();
                }
            }
        }

        /// <summary>
        /// Load a TypeSet, blocking until the download is complete, but possibly from cache
        /// </summary>
        public TypeSet Load()
        {
            _loadCalled = true;

            // No task; this can't be interrupted
            return Load(task: null);
        }

        bool _loadCalled = false;

        /// <summary>
        /// Load a TypeSet, blocking until the download is complete, but possibly from cache
        /// The task != null indicates it's called from Preload
        /// </summary>
        TypeSet Load(Task task)
        {
            // This is called either by Load or Preload.
            // If called by Preload we'll have a task that can be canceled.
            // If called by Load, cancel any Preload that's running
            // This is better than waiting for Preload to complete, because that's running at low-pri thread and IO
            // (better would be to give that thread a boost instead).
            var tcs = _loadTaskCompletionSource;
            if (task == null && tcs != null)
            {
                tcs.TrySetCanceled();
            }

            // Prevent threading issues between Load and Preload
            DebugLog.Append($"Waiting to load {_typeSetName}");
            lock (this)
            {
                DebugLog.Append($"Loading {_typeSetName}");

                // Do the download
                EnsureDownload(task);
                if (_allPaths == null)
                {
                    DebugLog.Append($"Couldn't download {_typeSetName}");
                    return null;
                }

                // Initialize the TypeSet
                var typeSet = new MRTypeSet(
                    _typeSetName,
                    _useWinrtProjections,
                    CacheDirectoryPath);

                typeSet.Version = $"{_packageName},{_version}";

                if (task != null && task.IsCanceled)
                {
                    // We're in a preload and a real load is pending
                    DebugLog.Append($"Aborting load for {_typeSetName}");
                    return null;
                }

                // Catch exceptions so we can fail gracefully if there's any kind of expected exception cases that I don't know about
                try
                {
                    // Load all the freshly downloaded packages into the TypeSet
                    DesktopManager2.LoadTypeSetMiddleweightReflection(typeSet, _allPaths, AssemblyPathFromNameCallback, task);

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
                    // Maybe a corrupted download

                    foreach (var filename in _allPaths)
                    {
                        try
                        {
                            DesktopManager2.SafeDelete(filename);
                        }
                        catch (Exception e)
                        {
                            DebugLog.Append(e, $"Couldn't delete {filename}");
                        }
                    }

                    // bugbug: throw or return null?
                    throw;
                }
            }
        }

        /// <summary>
        /// Preload phase 1. Goal is to return the AllNames from the AllNames of the latest cached version, if any
        /// </summary>
        public KeyValuePair<string, string>[] Preload1()
        {
            DebugLog.Append($"Preload1 on {_typeSetName}");
            if (_loadCalled)
            {
                DebugLog.Append($"Already loaded (1) {_typeSetName}");
                return null;
            }

            // Figure out the path of the parent to the cache
            // E.g. if the cache folder for this package is \path\to\foo\1.2.3,
            // then the parent is \path\to\foo.
            // Note that we might not know the version yet
            string cacheParentPath = "";
            if (string.IsNullOrEmpty(CacheDirectoryPath))
            {
                // Don't know the cache folder yet, figure it out
                var cachLeaf = _packageName;
                if (string.IsNullOrEmpty(cachLeaf))
                {
                    cachLeaf = _typeSetName;
                }
                cacheParentPath = Path.Combine(DesktopManager2.PackagesCachePath, cachLeaf);
            }
            else
            {
                cacheParentPath = Path.GetDirectoryName(CacheDirectoryPath);
            }


            // Find all directories for a type set (all versions)
            string[] allCachePathsForPackage = null;
            try // Play it safe with Directory API
            {
                if (Directory.Exists(cacheParentPath))
                {
                    allCachePathsForPackage = Directory.GetDirectories(cacheParentPath);
                }

                if (allCachePathsForPackage == null || allCachePathsForPackage.Length == 0)
                {
                    DebugLog.Append($"No cache yet for {_typeSetName}");
                    return null;
                }
            }
            catch(Exception e)
            {
                DebugLog.Append(e, $"No cache yet for {_typeSetName} (exception)");
                return null;
            }

            var maxVersion = _badVersion;
            var maxVersionLeaf = "";
            var maxVersionDirectory = "";

            // Loop through the cache paths and find the latest version
            foreach (var cachePath in allCachePathsForPackage)
            {
                // Get the version out of the directory name
                // E.g. get "1.2.3" out of \path\to\foo\1.2.3
                // First find the "1.2.3"
                var leaf = Path.GetFileName(cachePath);
                string versionToParse = leaf;
                if (!string.IsNullOrEmpty(PrereleaseTag))
                {
                    // The path might be more like \path\to\foo\1.2.3-prerelease
                    if (!leaf.ToLower().Contains($"-{PrereleaseTag}"))
                    {
                        continue;
                    }

                    versionToParse = leaf.Split('-')[0];
                }

                // Try to parse the version
                if (!Version.TryParse(versionToParse, out var version))
                {
                    DebugLog.Append($"Couldn't parse version from {leaf} for {_typeSetName}");
                    continue;
                }

                // Check for a new winner
                if (version > maxVersion)
                {
                    // If there's an older version, save it so that we can delete it later
                    if (maxVersion != _badVersion)
                    {
                        _oldCachePaths.Add(maxVersionDirectory);
                    }

                    maxVersion = version;
                    maxVersionLeaf = leaf;
                    maxVersionDirectory = cachePath;
                }
                else
                {
                    // If this is an older version, add it to the list of old directories
                    _oldCachePaths.Add(cachePath);
                }

            }

            if (maxVersion == _badVersion)
            {
                DebugLog.Append($"No cache found for {_typeSetName}");
                return null;
            }

            // If the subclass didn't set the cache folder name, we can figure it out now
            if (CacheDirectoryPath == null)
            {
                CacheDirectoryPath = Path.Combine(cacheParentPath, maxVersionLeaf);
            }

            Debug.Assert(Directory.Exists(CacheDirectoryPath), $"Cache path {CacheDirectoryPath} does not exist");
            DebugLog.Append($"Using cache folder {CacheDirectoryPath} for {_typeSetName}");

            // Now that we know where the cache is, see if it's cached the AllNames list
            var allNames = TypeSet.TryReadNamesFromCache2(CacheDirectoryPath);
            if (allNames != null)
            {
                DebugLog.Append($"Preloaded (1) {_typeSetName} from cache");
                return allNames;
            }

            DebugLog.Append($"No names cache yet (1) for {_typeSetName}");
            return null;
        }

        /// <summary>
        /// Phase 2 of preload. Figure out what the latest version is and download it if necessary.
        /// Returns the type set's AllNames
        /// </summary>
        /// <returns></returns>
        public KeyValuePair<string, string>[] Preload2()
        {
            if (_loadCalled)
            {
                DebugLog.Append($"Already loaded (2) {_typeSetName}");
                return null;
            }

            DebugLog.Append($"Waiting to preload (2) {_typeSetName}");
            lock (this)
            {
                DebugLog.Append($"Preloading (2) {_typeSetName}");

                var tcs = _loadTaskCompletionSource;
                var loadTask = tcs?.Task;

                if (loadTask != null && loadTask.IsCanceled)
                {
                    // There's a higher-priority load pending
                    DebugLog.Append($"Aborting preload (2) for {_typeSetName}");
                    return null;
                }

                try
                {
                    // Maybe download package from nuget.org
                    EnsureDownload(loadTask);

                    // That should have figured out the cache
                    if (CacheDirectoryPath == null)
                    {
                        DebugLog.Append($"No cache folder for {_typeSetName}");
                        return null;
                    }

                    // If we found old caches during Preload1, delete them now
                    RemoveOldCaches();

                    // If we have a cache, try to read the AllNames from it
                    var allNames = TypeSet.TryReadNamesFromCache2(CacheDirectoryPath);
                    if (allNames != null)
                    {
                        DebugLog.Append($"Preloading (2) {_typeSetName} from cache: {CacheDirectoryPath}");
                        return allNames;
                    }

                    // We have the package but AllNames hasn't been calculated yet
                    // Load the type set so that it can be calculated
                    var typeSet = Load(loadTask);
                    if (typeSet == null || typeSet.Types == null)
                    {
                        DebugLog.Append($"No type set returned to AppLoader Preload for {_typeSetName}");
                        return null;
                    }

                    // Wait for the AllNames to be calculated
                    var semaphore = new SemaphoreSlim(0, 1);
                    typeSet.PropertyChanged += (sender, e) =>
                    {
                        if (e.PropertyName == nameof(TypeSet.AllNames))
                        {
                            semaphore.Release();
                        }
                    };

                    if (typeSet.AllNames == null)
                    {
                        var task = Task.Run(() =>
                        {
                            semaphore.Wait();
                        });

                        DebugLog.Append($"Preloading (2) {_typeSetName}, waiting for names");
                        task.Wait();
                    }

                    DebugLog.Append($"Preloaded (2) {_typeSetName}");
                    return typeSet.AllNames;
                }
                finally
                {
                    _loadTaskCompletionSource = null;
                }
            } // lock
        }

        /// <summary>
        /// Remove any directories in _oldCacheDirectories
        /// </summary>
        void RemoveOldCaches()
        {
            // If we found any old directories, delete them
            if (_oldCachePaths != null)
            {
                // No point bringing down the app if there's a bug
                try
                {
                    foreach (var directory in _oldCachePaths)
                    {
                        try
                        {
                            // Deleting a directory could be bad if there's a bug. So to be super careful
                            // * Don't just delete-recursive
                            // * Only delete the extensions that will be there
                            // * Only actually delete the directory if it's empty
                            // * Use SafeDelete to delete files

                            var extensions = new string[] { "*.txt", "*.nupkg" };
                            foreach (var extension in extensions)
                            {
                                foreach (var file in Directory.GetFiles(directory, extension))
                                {
                                    // This validates that "Tempo" is in the full filename
                                    DesktopManager2.SafeDelete(file);
                                }
                            }

                            // This only works if the directory is empty
                            try
                            {
                                Directory.Delete(directory);
                            }
                            catch (Exception e)
                            {
                                DebugLog.Append(e, $"Couldn't delete {directory}");
                            }

                            DebugLog.Append($"Deleted old cache directory {directory}");
                        }
                        catch (Exception ex)
                        {
                            DebugLog.Append(ex, $"Error deleting old directory {directory}: {ex.Message}");
                        }
                    }
                }
                catch (Exception e)
                {
                    DebugLog.Append(e, $"Failed deleting old cache directories");
                }

                _oldCachePaths = null;
            }

        }
    }
}
