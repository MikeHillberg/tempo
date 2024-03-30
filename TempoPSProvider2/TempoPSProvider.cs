
// Command line arguments to debug the project:
// C:\WINDOWS\syswow64\WindowsPowerShell\v1.0\powershell.exe
// -noexit -command "[reflection.assembly]::loadFrom('TempoPSProvider.dll') | import-module"
// -noexit -command "copy ..\..\..\SliceSysWin\bin\debug\*.ps*; .\MapTempoDrive.ps1; cd tempo:"

using System;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Provider;

namespace TempoPSProvider
{
    using CommonLibrary;
    using MiddleweightReflection;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Diagnostics;
    using System.Linq;
    using System.Management.Automation;
    using System.Management.Automation.Provider;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Tempo;




    // A PowerShell custom provider that lets you navigate metadata    
    [CmdletProvider("Tempo", ProviderCapabilities.ExpandWildcards)]
    public class TempoPSProvider : NavigationCmdletProvider
    {
        public static string PipeNameKey = "TempoPipeName";
        public static string FilenamesKey = "TempoFilenames";

        protected override string[] ExpandPath(string path)
        {
            // Results are cached because the paths we return here are going to get asked for right away in GetItem
            _cachedExpansionsIndex = 0;
            _cachedExpansions.Clear();

            path = this.NormalizePath(path);

            var regexParts = new List<Regex>();
            var matches = new List<string>();

            // Split the path into its component parts
            var pathParts = path.Split(_whackCharArray);
            foreach (var part in pathParts)
            {
                // Convert from file system wildcards to regex
                var converted = part.Replace("*", ".*").Replace("?", ".");
                regexParts.Add(new Regex($"^{converted}$"));
            }

            if (pathParts.Length == 1)
            {
                // Only one part, so either Namespaces or Types root directory
                foreach (var root in _rootContainers)
                {
                    if (regexParts[0].IsMatch(root))
                    {
                        matches.Add($@"{root}");
                        _cachedExpansions.Add(new ExpandedPathResult() { Path = root, Item = root });
                    }
                }

                return matches.ToArray();
            }

            if (regexParts[0].IsMatch(_namespacesRootName))
            {
                // Somewhere under \Namespaces root
                foreach (var ns in TempoDrive(this.PSDriveInfo).Namespaces)
                {
                    // Split the namespace into parts just like we did with the input path
                    var nsParts = $@"{_namespacesRootName}.{ns}".Split('.');

                    // The path we're looking for (in regexParts) could be to a namespace or a type.
                    // If it's to a namespace it will have the same number of parts as nsParts. If it's to a type,
                    // If it's to a type, it will have one more part than the namespace does.
                    var regexPartCount = regexParts.Count;
                    var nsPartCount = nsParts.Length;
                    if (regexPartCount < nsPartCount || regexPartCount > nsPartCount + 1)
                    {
                        continue;
                    }

                    // See if each part of the path matches each part of the namespace
                    // There might be one more regex part than ns parts (see above).
                    var match = true;
                    for (int i = 0; i < nsPartCount; i++)
                    {
                        if (!regexParts[i].IsMatch(nsParts[i]))
                        {
                            match = false;
                        }
                    }

                    if (match)
                    {
                        // If this path is a namespace, then we can get the namespace now and we're done.
                        var nsVM = GetNamespaceVM(ns);
                        if (nsVM != null && regexPartCount == nsParts.Length)
                        {
                            var nsPath = ns.Replace('.', '\\');
                            nsPath = $@"{_namespacesRootName}\{nsPath}";
                            matches.Add($@"{nsPath}");
                            _cachedExpansions.Add(new ExpandedPathResult() { Path = nsPath, Item = nsVM });
                        }

                        // Otherwise it might be a type. Compare the last path part (regex part)
                        // with all the type names
                        else
                        {
                            foreach (var type in nsVM.Types)
                            {
                                if (regexParts.Last().IsMatch(type.PrettyName))
                                {
                                    // This type matches
                                    var nsPath = ns.Replace('.', '\\');
                                    nsPath = $@"{_namespacesRootName}\{nsPath}\{type.PrettyName}";
                                    matches.Add($@"{nsPath}");
                                    _cachedExpansions.Add(new ExpandedPathResult() { Path = nsPath, Item = type });
                                }
                            }
                        }
                    }

                }
            }

            if (regexParts[0].IsMatch(_typesRootName))
            {
                // Somewhere under the \Types root

                // Convert the path wildcards to equivalent Regex
                var converted = path.Replace("*", ".*").Replace("?", ".");

                // Also preserve the whack from looking like a Regex escape
                converted = converted.Replace("\\", "\\\\");
                var regex = new Regex($"^{converted}$");

                foreach (var type in GetPublicTypes())
                {
                    var typePath = $@"{_typesRootName}\{type.PrettyName}";

                    if (regex.IsMatch(typePath))
                    {
                        matches.Add($@"{typePath}");
                        _cachedExpansions.Add(new ExpandedPathResult() { Path = typePath, Item = type });
                    }
                }
            }

            return matches.ToArray();
        }


        // Members returned by ExpandPaths so that we don't have to look them up again in GetItem
        List<ExpandedPathResult> _cachedExpansions = new List<ExpandedPathResult>();
        int _cachedExpansionsIndex;
        struct ExpandedPathResult
        {
            public string Path;
            public object Item; // NamespaceVM, TypeVM, or string
        }

        protected override Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            var drive = new PSDriveInfo("Tempo", this.ProviderInfo, "\\", "Tempo types", null);
            return new Collection<PSDriveInfo>() { drive };
        }


        /// <summary>
        /// The Windows PowerShell engine calls this method when the New-Drive
        /// cmdlet is run. This provider creates a connection to the database
        /// file and sets the Connection property in the PSDriveInfo.
        /// </summary>
        /// <param name="drive">
        /// Information describing the drive to create.
        /// </param>
        /// <returns>An object that describes the new drive.</returns>
        protected override PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            try
            {
                UnhandledExceptionManager.UnhandledException += (s, e) =>
                {
                    PrintException(e.Exception);
                };

                // Check to see if the supplied drive object is null.
                if (drive == null)
                {
                    WriteError(new ErrorRecord(
                                               new ArgumentNullException("drive"),
                                               "NullDrive",
                                               ErrorCategory.InvalidArgument,
                                               null));

                    return null;
                }

                InstallRedirectHack();

                AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
                {
                    return null;
                };

                // Initialize the type metadata

                DesktopManager2.Initialize(wpfApp: false);

                // Check if Tempo passed a filename list
                // @"C:\Users\Mike\Downloads\Microsoft.WindowsAppSDK.1.3.230110100-experimental.nightly.nupkg;"
                var filenameList = Environment.GetEnvironmentVariable(FilenamesKey);
                if (string.IsNullOrEmpty(filenameList))
                {
                    // No list, just load the System32 WinMDs
                    DesktopManager2.LoadWindowsTypesWithMR(useWinRTProjections: false);

                    // Bugbug: cleaner out value, side effects
                    Manager.CurrentTypeSet = Manager.WindowsTypeSet;
                }
                else
                {
                    // Load the specified files
                    var filenames = filenameList.Split(';');
                    var typeSet = new MRTypeSet(MRTypeSet.CustomMRName, usesWinRTProjections: true);
                    DesktopManager2.LoadTypeSetMiddleweightReflection(
                        typeSet,
                        (from filename in filenames where !string.IsNullOrEmpty(filename) select filename).ToArray(),
                        useWinRTProjections: true);
                    Manager.CurrentTypeSet = typeSet;
                }

                var types = GetPublicTypes();


                // Create the new drive and create an ODBC connection
                // to the new drive.
                TempoPSDriveInfo tempoDrive = new TempoPSDriveInfo(drive);

                // Cache namespace info on the drive. This class gets re-created all the time, so we can't store state
                // on it, but the drive doesn't go away.
                tempoDrive.Namespaces = new HashSet<string>();
                foreach (var type in types)
                {
                    var ns = type.Namespace;

                    // Store the actual namespace in the cache
                    tempoDrive.Namespaces.Add(ns);

                    // Store all the ancestor namespaces in the path too. This makes it easier to 
                    // navigate the namespace like a hierarchy.
                    while (ns.Contains('.'))
                    {
                        ns = ns.Substring(0, ns.LastIndexOf('.'));
                        tempoDrive.Namespaces.Add(ns);
                    }

                }

                return tempoDrive;
            }
            catch(Exception e)
            {
                PrintException(e);
                return null;
            }
        }

        void PrintException(Exception e)
        {
            Console.WriteLine($"{e.GetType().Name}\n{e.Message}");
        }

        private void InstallRedirectHack()
        {
            var appDomain = AppDomain.CurrentDomain;

            appDomain.AssemblyResolve += (s, e) =>
            {
                // This is a hack to get the right assembly versions loaded
                //
                // System.Reflection.Metadata.dll 8.0.0.0 depends on
                // System.Runtime.CompilerServices.dll 6.0.0.0
                // 
                // But SRM.dll also depends on
                // System.Memory.dll, which depends on
                // CompilerServices.dll 4.0.4.1
                //
                // In an app, the app.config would resolve that to the higher version (6.0.0.0)
                // But this Tempo PS Provider is a DLL, which don't have app.config files
                //
                // As a workaround, hook AppDomain.AssemblyResolve, and when the CompilerServices.Unsafe
                // DLL is requested, load and return the DLL file that's packaged with
                // the Provider DLL.

                var compilerServicesName = "System.Runtime.CompilerServices.Unsafe";
                if (e.Name.StartsWith($"{compilerServicesName},"))
                {
                    try
                    {
                        var thisAssemblyPath = this.GetType().Assembly.Location;
                        var thisAssemblyLocation = Path.GetDirectoryName(thisAssemblyPath);
                        var compilerServicesPath = $@"{thisAssemblyLocation}\{compilerServicesName}.dll";
                        var assembly = Assembly.LoadFile(compilerServicesPath);
                        return assembly;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Couldn't load {compilerServicesName}");
                        Console.WriteLine(ex.Message);
                    }
                }

                return null;
            };
        }

        /// <summary>
        /// The Windows PowerShell engine calls this method when the Get-Item
        /// cmdlet is run.
        /// </summary>
        /// <param name="path">The path to the item to return.</param>
        protected override void GetItem(string path)
        {
            // See if this is in the cache
            if (_cachedExpansions.Count >= _cachedExpansionsIndex + 1)
            {
                var savedMatch = _cachedExpansions[_cachedExpansionsIndex++];
                if (savedMatch.Path == path)
                {
                    WriteItemObject(savedMatch.Item, savedMatch.Path, false);
                    return;
                }
            }

            var parsedPaths = this.TryParseRawPath(path, containerOnly: false);
            if (parsedPaths == null)
            {
                return;
            }


            // If this is a path to a type there can be more than one (when under \Types). But there are no duplicate namespaces (yet)
            var nsVM = parsedPaths.FirstOrDefault().NamespaceVM;
            if (nsVM != null)
            {
                WriteItemObject(nsVM, path, true);
                return;
            }


            // There could be multiple type names returned
            foreach (var parsedPath in parsedPaths)
            {
                var type = parsedPath.TypeVM;
                if (type != null)
                {
                    WriteItemObject(type, path, false);
                }
            }

            foreach (var root in _rootContainers)
            {
                if (path == root)
                {
                    WriteItemObject(root, path, true);
                    return;
                }
            }

            return;
        } // End GetItem method.


        /// <summary>
        /// Test to see if the specified item exists.
        /// </summary>
        /// <param name="path">The path to the item to verify.</param>
        /// <returns>True if the item is found.</returns>
        protected override bool ItemExists(string path)
        {
            return IsValidPath(path);
        } // End ItemExists method.

        /// <summary>
        /// Test to see if the specified path is syntactically valid.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if the specified path is valid.</returns>
        protected override bool IsValidPath(string path)
        {
            var parsedPaths = this.TryParseRawPath(path, containerOnly: true).ToList();
            if (parsedPaths == null || parsedPaths.Count == 0)
            {
                return false;
            }

            var parsed = parsedPaths[0];
            return parsed.ContainerName != null
                || parsed.NamespaceVM != null
                || parsed.TypeVM != null;

        } // End IsValidPath method.

        TypeViewModel GetTypeVM(string fullName)
        {
            foreach (var type in GetPublicTypes())
            {
                if (fullName == type.PrettyFullName)
                {
                    return type;
                }
            }

            return null;
        }


        // There are two containers at the root: Namespaces and Types
        const string _namespacesRootName = "Namespaces";
        const string _namespacesRootPath = @"\Namespaces";
        const string _typesRootName = "Types";
        const string _typesRootPath = @"\Types";
        static string[] _rootContainers = new string[] { _namespacesRootName, _typesRootName };

        static TempoPSDriveInfo TempoDrive(PSDriveInfo driveInfo)
        {
            return driveInfo as TempoPSDriveInfo;
        }

        public static IEnumerable<TypeViewModel> GetPublicTypes()
        {
            return from t in Manager.CurrentTypeSet.Types
                   where t.IsPublic
                   select t;
        }

        // This is called as part of a 'dir' (gci)
        protected override void GetChildItems(string path, bool recurse)
        {
            var parsedPaths = TryParseRawPath(path, containerOnly: true).ToList();
            if (parsedPaths == null || parsedPaths.Count == 0)
            {
                // Happens if you do a 'dir foo' and 'foo' doesn't exist
                return;
            }

            foreach (var parsedPath in parsedPaths)
            {
                if (parsedPath.ContainerName == string.Empty)
                {
                    // The children of the root are Namespaces and Types
                    WriteItemObject(_typesRootName, path, true);
                    WriteItemObject(_namespacesRootName, path, true);
                    return;
                }

                if (parsedPath.ContainerName == _typesRootName)
                {
                    // The children of Types is all the types
                    foreach (var type in GetPublicTypes())
                    {
                        WriteItemObject(type, $@"{_typesRootPath}\{type.PrettyFullName}", false);
                    }
                    return;
                }

                if (parsedPath.ContainerName == _namespacesRootName)
                {
                    // The children of "Namespaces" is the root namespaces
                    var roots = (from ns in TempoDrive(this.PSDriveInfo).Namespaces
                                 select ns.Split('.')[0])
                                 .Distinct().OrderBy(s => s).ToList();

                    foreach (var root in roots)
                    {
                        var vm = new NamespaceViewModel(Manager.CurrentTypeSet, root);
                        WriteItemObject(vm, $@"{_namespacesRootPath}\{root}", false);
                    }
                    return;
                }

                if (parsedPath.NamespaceVM != null)
                {
                    // Get the children namespaces (e.g. from \Namespaces\Microsoft return \Namespaces\Microsoft\UI)
                    foreach (var ns in parsedPath.NamespaceVM.Namespaces)
                    {
                        var children = parsedPath.NamespaceVM.Namespaces;
                        var nsPath = ns.FullName.Replace('.', '\\');
                        WriteItemObject(ns, $@"{_namespacesRootPath}\{nsPath}", true);
                    }

                    // Get the types directly in this namespace
                    foreach (var type in parsedPath.NamespaceVM.Types)
                    {
                        if (!type.IsPublic)
                        {
                            continue;
                        }

                        WriteItemObject(type, $@"{path}\{type.PrettyName}", false);
                    }
                }

                if (parsedPath.TypeVM != null)
                {
                    WriteItemObject(parsedPath.TypeVM, $@"{path}\{parsedPath.TypeVM.PrettyName}", false);
                }
            }

        } // End GetChildItems method.

        // A single backslash as a char array
        static char[] _whackCharArray = new char[] { '\\' };

        NamespaceViewModel GetNamespaceVM(string fullName)
        {
            // The TempoDrive.Namespaces contains not only all the actual namespaces, but intermediates.
            // Ex not just A.B.C, also A.B, even if there are no types directly in A.B

            if (string.IsNullOrEmpty(fullName)
                || !TempoDrive(this.PSDriveInfo).Namespaces.Contains(fullName))
            {
                return null;
            }

            return new NamespaceViewModel(Manager.CurrentTypeSet, fullName);

        }

        class ParsedRawPath
        {
            public string ContainerName;
            public NamespaceViewModel NamespaceVM;
            public TypeViewModel TypeVM;
        }

        IEnumerable<ParsedRawPath> TryParseRawPath(string path, bool containerOnly)
        {
            path = this.NormalizePath(path);

            // Root case
            if (string.IsNullOrEmpty(path))
            {
                yield return new ParsedRawPath() { ContainerName = "" };
                yield break;
            }

            // "\Types"
            if (path == _typesRootName)
            {
                yield return new ParsedRawPath() { ContainerName = _typesRootName };
                yield break;
            }

            // "\Namespaces"
            if (path == _namespacesRootName)
            {
                yield return new ParsedRawPath() { ContainerName = _namespacesRootName };
                yield break;
            }

            if (path.StartsWith(_namespacesRootName))
            {
                // Path is in \Namespaces directory

                // Get the rest of the path beyond \Namespaces, which is the actual namespace or string.empty
                path = path.Substring(_namespacesRootName.Length);

                // If it's not string.empty, trim the leading whack
                path = path.TrimStart('\\');

                // The *path* shouldn't contain any dots, just whacks
                if (path.Contains('.'))
                {
                    yield break;
                }

                // Now convert the whacks to dots to get a namespace name
                var name = path.Replace('/', '.').Replace('\\', '.');

                var nsVM = GetNamespaceVM(name);
                if (nsVM != null)
                {
                    yield return new ParsedRawPath() { NamespaceVM = nsVM };
                }
                else
                {
                    var typeVM = GetTypeVM(name);
                    if (typeVM != null)
                    {
                        yield return new ParsedRawPath() { TypeVM = typeVM };
                    }
                }
            }

            else if (path != _typesRootName && path.StartsWith($@"{_typesRootName}\"))
            {
                // This path is in \Types, and there could be multiple with the same name

                path = path.Substring(_typesRootName.Length + 1);
                foreach (var type in GetPublicTypes())
                {
                    if (type.PrettyName == path)
                    {
                        yield return new ParsedRawPath() { TypeVM = type };
                    }
                }
            }


            yield break;
        }

        bool IsRawPathInTypesRoot(string rawPath)
        {
            return this.NormalizePath(rawPath).StartsWith($@"{_typesRootName}\");
        }

        bool IsRawPathInNamespacesRoot(string rawPath)
        {
            return this.NormalizePath(rawPath).StartsWith($@"{_namespacesRootName}\");
        }


        /// <summary>
        /// Return the names of all child items.
        /// </summary>
        /// <param name="path">The root path.</param>
        /// <param name="returnContainers">This parameter is not used.</param>
        protected override void GetChildNames(
                                              string path,
                                              ReturnContainers returnContainers)
        {
            Debug.Assert(false);
            throw new NotImplementedException("GetChildNames");
        }

        /// <summary>
        /// Determines if the specified path has child items.
        /// </summary>
        /// <param name="path">The path to examine.</param>
        /// <returns>
        /// True if the specified path has child items.
        /// </returns>
        protected override bool HasChildItems(string path)
        {
            // bugbug: This should check not only that it's a container but that it has children

            // Only namespaces and the root containers can have children, and the names are unique
            var parsedPath = this.TryParseRawPath(path, false).FirstOrDefault();
            return parsedPath != null &&
                   (parsedPath.ContainerName != null || parsedPath.NamespaceVM != null);
        }


        /// <summary>
        /// Determine if the path specified is that of a container.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>True if the path specifies a container.</returns>
        protected override bool IsItemContainer(string path)
        {
            // Only namespaces and the root containers can have children, and the names are unique
            var parsedPath = this.TryParseRawPath(path, false).FirstOrDefault();
            return parsedPath != null &&
                   (parsedPath.ContainerName != null || parsedPath.NamespaceVM != null);
        }


        /// <summary>
        /// Gets the name of the leaf element in the specified path.
        /// </summary>
        /// <param name="path">
        /// The full or partial provider specific path.
        /// </param>
        /// <returns>
        /// The leaf element in the path.
        /// </returns>
        protected override string GetChildName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var index = path.LastIndexOf('\\');
            if (index == -1)
            {
                return path;
            }
            else
            {
                var result = path.Substring(index + 1);
                return result;
            }
        }



        /// <summary>
        /// Returns the parent portion of the path, removing the child
        /// segment of the path.
        /// </summary>
        /// <param name="path">
        /// A full or partial provider specific path. The path may be to an
        /// item that may or may not exist.
        /// </param>
        /// <param name="root">
        /// The fully qualified path to the root of a drive. This parameter
        /// may be null or empty if a mounted drive is not in use for this
        /// operation.  If this parameter is not null or empty the result
        /// of the method should not be a path to a container that is a
        /// parent or in a different tree than the root.
        /// </param>
        /// <returns>The parent portion of the path.</returns>
        protected override string GetParentPath(string path, string root)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var parentPath = base.GetParentPath(path, root);
            return parentPath;
        }

        /// <summary>
        /// Joins two strings with a provider specific path separator.
        /// </summary>
        /// <param name="parent">
        /// The parent segment of a path to be joined with the child.
        /// </param>
        /// <param name="child">
        /// The child segment of a path to be joined with the parent.
        /// </param>
        /// <returns>
        /// A string that contains the parent and child segments of the path
        /// joined by a path separator.
        /// </returns>
        protected override string MakePath(string parent, string child)

        {
            string result;

            string normalParent = this.NormalizePath(parent);
            //normalParent = this.RemoveDriveFromPath(normalParent);
            string normalChild = this.NormalizePath(child);
            //normalChild = this.RemoveDriveFromPath(normalChild);

            if (String.IsNullOrEmpty(normalParent) && String.IsNullOrEmpty(normalChild))
            {
                result = String.Empty;
            }
            else if (String.IsNullOrEmpty(normalParent) && !String.IsNullOrEmpty(normalChild))
            {
                result = normalChild;
            }
            else if (!String.IsNullOrEmpty(normalParent) && String.IsNullOrEmpty(normalChild))
            {
                if (normalParent.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
                {
                    result = normalParent;
                }
                else
                {
                    result = normalParent + @"\";
                }
            }
            else
            {
                if (!normalParent.Equals(String.Empty) &&
                    !normalParent.EndsWith(@"\", StringComparison.OrdinalIgnoreCase))
                {
                    result = normalParent + "\\";
                }
                else
                {
                    result = normalParent;
                }

                if (normalChild.StartsWith("\\", StringComparison.OrdinalIgnoreCase))
                {
                    result += normalChild.Substring(1);
                }
                else
                {
                    result += normalChild;
                }
            } // End else block.

            return result;
        } // End MakePath method.

        /// <summary>
        /// Normalizes the path so that it is a relative path to the
        /// basePath that was passed.
        /// </summary>
        /// <param name="path">
        /// A fully qualified provider specific path to an item.  The item
        /// should exist or the provider should write out an error.
        /// </param>
        /// <param name="basepath">
        /// The path that the return value should be relative to.
        /// </param>
        /// <returns>
        /// A normalized path that is relative to the basePath that was
        /// passed. The provider should parse the path parameter, normalize
        /// the path, and then return the normalized path relative to the
        /// basePath.
        /// </returns>
        protected override string NormalizeRelativePath(
                                                        string path,
                                                        string basepath)
        {
            // Normalize the paths first.
            string normalPath = this.NormalizePath(path);
            //normalPath = this.RemoveDriveFromPath(normalPath);
            string normalBasePath = this.NormalizePath(basepath);
            //normalBasePath = this.RemoveDriveFromPath(normalBasePath);

            if (String.IsNullOrEmpty(normalBasePath))
            {
                return normalPath;
            }
            else
            {
                if (!normalPath.Contains(normalBasePath))
                {
                    return null;
                }

                var index = normalBasePath.Length;
                if (!normalBasePath.EndsWith(@"\"))
                {
                    index += "\\".Length;
                }
                return normalPath.Substring(index);
            }
        }


        /// <summary>
        // Convert slashes to whacks, remove leading/trailing whacks
        private string NormalizePath(string path)
        {
            string result = path;

            if (!String.IsNullOrEmpty(path))
            {
                result = path.Replace("/", "\\");
                result = result.TrimStart(_whackCharArray).TrimEnd(_whackCharArray);
            }

            return result;
        } // End NormalizePath method.


        /// <summary>
        /// Throws an argument exception stating that the specified path does
        /// not represent either a table or a row
        /// </summary>
        /// <param name="path">path which is invalid</param>
        private void ThrowTerminatingInvalidPathException(string path)
        {
            StringBuilder message = new StringBuilder("Path must represent either a table or a row :");
            message.Append(path);

            throw new ArgumentException(message.ToString());
        }



    } // End AccessDBProvider class.
}