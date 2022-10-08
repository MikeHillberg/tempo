using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommonLibrary;
using Microsoft.Win32;

namespace Tempo
{
    internal class TypeIndices
    {
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public int Indent { get; set; }
    }


    static public class GeneratedIdl
    {
        public static string StatusMessage = "Initializing ...";
        public static bool InitializationError = false;

        static string _winMDIdlName = null;
        static GeneratedIdl()
        {
            IsAvailable = false;

            // bugbug: defer
            _winMDIdlName = FindWinMDIdl();
            if (_winMDIdlName != null)
                IsAvailable = true;
        }

        static public EventHandler InitializationComplete;

        public static bool IsAvailable { get; private set; }

        static public void EnsureInitialized()
        {
            // "C:\Program Files (x86)\Windows Kits\8.2\bin\x86\winmdidl.exe" /outdir:c:\temp\tempo Windows.UI.Xaml.winmd

            if (Initialized)
                return;

            lock (StatusMessage)
            {
                if (Initialized)
                    return;


                try
                {
                    //_winMDIdlName = FindWinMDIdl();
                    if (_winMDIdlName != null)
                    {
                        _tempPath = System.IO.Path.GetTempPath() + @"Tempo";
                        _idlPathBase = _tempPath + @"\Idl";
                        _idlDirectoryInfo = Directory.CreateDirectory(_idlPathBase);

                        CreateIdls();
                        //IndexIdls();

                        IsAvailable = true;
                    }

                }
                catch (Exception e)
                {
                    DebugLog.Append("Failed initializing IDL");
                    DebugLog.Append(e.Message);

                    StatusMessage = "IDL display disabled:\n" + e.Message;
                }

                Initialized = true;

                if (InitializationComplete != null)
                    InitializationComplete(null, null);
            }

        }

        static string _tempPath;
        static string _idlPathBase;
        static DirectoryInfo _idlDirectoryInfo;
        static Dictionary<string, Dictionary<string, TypeIndices>> _indices = new Dictionary<string, Dictionary<string, TypeIndices>>();

        static public bool Initialized = false;


        static private string FindWinMDIdl()
        {

            // If something goes wrong, not worth crashing over it (and that would break the possibility of updating
            // to a fixed version).

            //try
            //{
            //    DebugLog.Start("Looking for WinMDIdl");

            //    // C:\Program Files (x86)\Windows Kits\10\bin\10.0.15063.0\x86\winmdidl.exe
            //    // cd HKLM:\software\Microsoft\Windows Kits\Installed Roots
            //    // get-itempropertyvalue  .  KitsRoot10
            //    string kitsBaseDirectoryName = null;
            //    var key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows Kits\Installed Roots");
            //    if (key != null)
            //    {
            //        kitsBaseDirectoryName = key.GetValue("KitsRoot10") as string + @"\bin";
            //    }


            //    if (!Directory.Exists(kitsBaseDirectoryName))
            //    {
            //        DebugLog.Append("Couldn't find Windows Kits at " + kitsBaseDirectoryName);
            //        return null;
            //    }

            //    var kitsBaseDirectoryInfo = Directory.CreateDirectory(kitsBaseDirectoryName);

            //    //var dirs = FindNumberedDirectories(kitsBaseDirectoryInfo);
            //    string highestName = FindBuildNumberedDirectory(kitsBaseDirectoryInfo);
            //    if (highestName == null)
            //    {
            //        DebugLog.Append("Couldn't find a directory under " + kitsBaseDirectoryInfo.FullName);
            //        return null;
            //    }

            //    var name = kitsBaseDirectoryName + @"\\" + highestName + @"\x86\" + @"\winmdidl.exe";
            //    if (File.Exists(name))
            //    {
            //        DebugLog.Append("Found " + name);
            //        return name;
            //    }
            //    else
            //        DebugLog.Append("Couldn't find " + name);


            //    //foreach (var dir in dirs)
            //    //{
            //    //    var arch = Environment.ExpandEnvironmentVariables("%PROCESSOR_ARCHITECTURE%");
            //    //    if (string.IsNullOrEmpty(arch))
            //    //    {
            //    //        DebugLog.Append("Couldn't determine architecture");
            //    //        return null;
            //    //    }

            //    //    var name = kitsBaseDirectoryName + dir + @"\bin\" + arch + @"\winmdidl.exe";
            //    //    if (File.Exists(name))
            //    //    {
            //    //        DebugLog.Append("Found " + name);
            //    //        return name;
            //    //    }
            //    //    else
            //    //        DebugLog.Append("Couldn't find " + name);
            //    //}

            //    DebugLog.Append("Couldn't find WinMDIdl");

            //}
            //catch(Exception e)
            //{
            //    DebugLog.Start("Couldn't search for WinMDIdl");
            //    DebugLog.Append(e.Message);
            //}

            return null;

        }

        static public string FindHighestDirectory(DirectoryInfo directory, string prefix)
        {
            var directories = directory.EnumerateDirectories(prefix + "*");
            var best = prefix;
            foreach (var vdir in directories)
            {
                if (vdir.Name.CompareTo(best) > 0)
                {
                    best = vdir.Name;
                }
            }

            return best;
        }



        public static string FindBuildNumberedDirectory(DirectoryInfo directory)
        {
            var directories = directory.EnumerateDirectories("*");

            var numbers = new List<float>();

            DirectoryInfo maxDir = null;
            BuildNumber maxBuildNumber = null;

            foreach (var dir in directories)
            {
                var parts = dir.Name.Split('.');
                if (parts.Length != 4)
                    continue;

                try
                {
                    var buildNumber = new BuildNumber()
                    {
                        //Major = int.Parse(parts[0]),
                        //Minor = int.Parse(parts[1]),
                        //Build = int.Parse(parts[2]),
                        //Patch = int.Parse(parts[3])
                    };

                    int result;
                    if (int.TryParse(parts[0], out result))
                        buildNumber.Major = result;
                    if (int.TryParse(parts[1], out result))
                        buildNumber.Minor = result;
                    if (int.TryParse(parts[2], out result))
                        buildNumber.Build = result;
                    if (int.TryParse(parts[3], out result))
                        buildNumber.Patch = result;

                    if (maxBuildNumber == null || buildNumber >= maxBuildNumber)
                    {
                        maxDir = dir;
                        maxBuildNumber = buildNumber;
                    }
                }
                catch(Exception)
                {
                    // Too lazy to do int.TryParse
                }
            }

            return maxDir?.Name;
        }

        class BuildNumber
        {
            public int Major { get; set; }
            public int Minor { get; set; }
            public int Build { get; set; }
            public int Patch { get; set; }

            public override bool Equals(object obj)
            {
                var other = obj as BuildNumber;
                if (other == null)
                    return false;

                return Major == other.Major
                    && Minor == other.Minor
                    && Build == other.Build
                    && Patch == other.Patch;
            }

            public override int GetHashCode()
            {
                return Major.GetHashCode() + Minor.GetHashCode() + Build.GetHashCode() + Patch.GetHashCode();
            }
            public static bool operator >= (BuildNumber num1, BuildNumber num2)
            {
                if (num2.Major > num1.Major
                    || num2.Minor > num1.Minor
                    || num2.Build > num1.Build
                    || num2.Patch > num1.Patch)
                {
                    return false;
                }

                return true;
            }
            public static bool operator <=(BuildNumber num1, BuildNumber num2)
            {
                if (num1 == num2)
                    return true;
                else
                    return num2 >= num1;
            }
        }



        static public IEnumerable<string> FindNumberedDirectories(DirectoryInfo directory)
        {
            var directories = directory.EnumerateDirectories("*");

            var numbers = new List<float>();

            foreach (var dir in directories)
            {
                float num;
                if (!float.TryParse(dir.Name, out num))
                    continue;

                numbers.Add(num);
            }

            return from n in numbers orderby n descending select n.ToString();

        }

        private static void CreateIdls()
        {
            //var windir = Environment.ExpandEnvironmentVariables("%windir%");
            //var metadataDirectoryName = windir + @"\System32\WinMetadata";            // C:\Windows\System32\WinMetadata
            //var winmdFileNames = Directory.EnumerateFiles(metadataDirectoryName, @"*.winmd");

            DebugLog.Start("Generating IDLs");

            var process = new Process();
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = _idlPathBase;
            process.StartInfo.FileName = _winMDIdlName;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
            process.StartInfo.UseShellExecute = false;

            foreach (var assembly in Manager.WinmdTypeSet.Assemblies)
            {
                var winmdFileName = assembly.Location;
                DebugLog.Append("Generating IDL for " + winmdFileName);

                string justTheFileName;
                string winmdFileIdlDir;
                FigureOutFileNames(winmdFileName, out justTheFileName, out winmdFileIdlDir);


                if (Directory.Exists(winmdFileIdlDir))
                {
                    continue;
                }


                foreach (var dir in Directory.EnumerateDirectories(_idlPathBase, justTheFileName + ".*"))
                {
                    // Sanity check; don't accidentally delete c:\
                    if (!dir.Contains("Tempo"))
                    {
                        throw new Exception("Error trying to delete old cache (" + dir + ")");
                    }

                    foreach (var file in Directory.EnumerateFiles(dir))
                    {
                        var fileInfo = new FileInfo(file);
                        fileInfo.Delete();
                    }

                    Directory.Delete(dir);
                }

                // Delete the old cache location
                if (Directory.Exists(_idlPathBase))
                {
                    // Sanity check; don't accidentally delete c:\
                    if (!_idlPathBase.Contains("Tempo"))
                    {
                        throw new Exception("Error trying to delete old cache (" + _idlPathBase + ")");
                    }

                    foreach (var file in Directory.EnumerateFiles(_idlPathBase, "*.idl").Union(Directory.EnumerateFiles(_idlPathBase, "*.index")))
                    {
                        var fileInfo = new FileInfo(file);
                        fileInfo.Delete();
                    }
                }

                process.StartInfo.Arguments = @"/outdir:" + winmdFileIdlDir + " " + winmdFileName;
                process.Start();
                process.WaitForExit();

                IndexIdls(winmdFileIdlDir);
            }

            process.Dispose();

        }

        private static void FigureOutFileNames(string winmdFileName, out string justTheFileName, out string winmdFileIdlDir)
        {
            justTheFileName = winmdFileName.Substring(winmdFileName.LastIndexOf('\\') + 1);
            var winmdFileInfo = new FileInfo(winmdFileName);

            var date = winmdFileInfo.LastWriteTime.ToString();
            date = date.Replace('/', '-');
            date = date.Replace(':', '-');
            date = date.Replace(' ', '-');

            var winmdFileIdlDirBase = _idlPathBase + @"\" + justTheFileName;
            winmdFileIdlDir = winmdFileIdlDirBase + "." + date;
        }

        private static void IndexIdls(string winmdFileIdlDir)
        {
            var idlFiles = Directory.EnumerateFiles(winmdFileIdlDir, @"*.idl");

            foreach (var idlFile in idlFiles)
            {
                IndexIdlFile(idlFile);
            }
        }

        enum ScopeTypes
        {
            Namespace,
            Type
        }

        private static void IndexIdlFile(string idlFile)
        {
            StreamReader file = null;
            bool searchingForStart = true;

            var justTheFileName = idlFile.Substring(idlFile.LastIndexOf('\\') + 1);
            var ns = justTheFileName.Substring(0, justTheFileName.LastIndexOf('.'));

            var typeIndicesMap = new Dictionary<string, TypeIndices>();

            var scope = new Stack<ScopeTypes>();

            int typeStart = -1;
            int typeEnd = -1;
            string typeKind = null;
            string typeName = null;
            int typeIndent = 0;
            bool searchingForName = true;
            bool isDelegate = false;
            int position = 0;

            string line = null;

            var typeIndexStrings = new Dictionary<string, string>();

            DebugLog.Append("Indexing " + justTheFileName);

            using (file = File.OpenText(idlFile))
            {
                for (; ; )
                {
                    if (line != null)
                    {
                        position += line.Length + 2; // (0d,0a)
                    }

                    line = file.ReadLine();
                    if (line == null)
                        break;

                    if (line.Contains("VideoFrame") & idlFile.EndsWith("Windows.Media.idl"))
                    {
                    }

                    var trimmed = line.Trim();

                    if (searchingForStart)
                    {
                        if (line.StartsWith(@"// Type definition"))
                            searchingForStart = false;

                        continue;
                    }

                    if (trimmed.StartsWith(@"//"))
                        continue;

                    if (trimmed.StartsWith(@"{"))
                        continue;

                    if (line == "")
                        continue;



                    if (trimmed.StartsWith("namespace"))
                    {
                        continue;
                    }

                    if (typeStart == -1)
                    {
                        if (trimmed == "}" || trimmed == "};")
                            continue;


                        typeStart = position;

                        searchingForName = true;

                        var chars = line.ToCharArray();
                        int indent = 0;
                        foreach (var c in chars)
                        {
                            if (c == ' ')
                                indent++;
                            else
                            {
                                break;
                            }
                        }

                        typeIndent = indent;
                        continue;
                    }
                    else if (trimmed.StartsWith("[") && searchingForName)
                        continue;
                    else if (
                        trimmed == "}" || trimmed == "};"
                        || isDelegate)
                    {
                        if (typeStart == -1)
                            continue; // end of a namespace

                        if (isDelegate)
                        {
                            // Example:
                            // HRESULT BackgroundTaskCanceledEventHandler([in] Windows.ApplicationModel.Background.IBackgroundTaskInstance* sender, [in] Windows.ApplicationModel.Background.BackgroundTaskCancellationReason reason);

                            var split = trimmed.Split(new char[] { ' ', '(' });
                            typeName = split[1];
                            isDelegate = false;
                        }

                        typeEnd = position + line.Length + 2; // 0d, 0a

                        var sb = new StringBuilder();
                        sb.Append(typeStart);
                        sb.Append(":");
                        sb.Append(typeIndent.ToString());
                        sb.Append(":");
                        sb.Append(typeEnd.ToString());

                        typeIndexStrings.Add(typeName, sb.ToString());

                        var typeIndices = new TypeIndices()
                        {
                            StartPosition = typeStart,
                            EndPosition = typeEnd,
                            Indent = typeIndent
                        };
                        typeIndicesMap.Add(typeName, typeIndices);

                        typeStart = -1;
                    }
                    else if (searchingForName)
                    {
                        searchingForName = false;

                        if (trimmed == "delegate")
                        {
                            isDelegate = true;
                            typeKind = "delegate";
                            continue;
                        }
                        else
                        {
                            var split = trimmed.Split(' ');
                            typeKind = split[0];
                            typeName = split[1];
                        }

                    }
                }
            } // using

            var indexFileName = IndexFileFromIdlFile(idlFile);
            var indexFile = File.CreateText(indexFileName);
            foreach (var entry in typeIndexStrings)
            {
                indexFile.WriteLine(entry.Key);
                indexFile.WriteLine(entry.Value);
            }
            indexFile.Flush();

            _indices.Add(ns, typeIndicesMap);
        }

        //idlFile.Substring(0, idlFile.LastIndexOf('.')) + ".index";
        static string IndexFileFromIdlFile(string idlFile)
        {
            return idlFile.Substring(0, idlFile.LastIndexOf('.')) + ".index";
        }

        static public string Get(TypeViewModel type)
        {
            if (type.IsAttribute)
                return "(IDL information not available for attribute types)";

            return "IDL currently disabled";

            // Catch exceptions or binding will catch it for us
            //try
            //{

            //    if (!type.IsClass)
            //        return GetHelper(type, type.Name);

            //    // Add the class 
            //    var sb = new StringBuilder();
            //    var runtimeClass = GetHelper(type, type.Name);
            //    sb.AppendLine(runtimeClass);

            //    // Add the required interfaces that aren't from an ancestor
            //    //var baseType = type.BaseType;
            //    //foreach (var iface in type.GetAllInterfaces())
            //    //{
            //    //    if (baseType != null && baseType.GetAllInterfaces().Contains(iface))
            //    //        continue;

            //    //    // Ignore IDisposable, etc
            //    //    if (!iface.Namespace.StartsWith("Windows."))
            //    //    {
            //    //        sb.AppendLine("No IDL available for " + iface.FullName + "\r\n");
            //    //        continue;
            //    //    }

            //    //    sb.AppendLine(GetHelper(iface));
            //    //}

            //    // Add the static and activation interfaces





            //    var waitingForBody = true;
            //    var reader = new StringReader(runtimeClass);
            //    string line;
            //    while ((line = reader.ReadLine()) != null)
            //    {
            //        int length = 0;
            //        line = line.Trim();

            //        if (waitingForBody)
            //        {
            //            if( line.StartsWith("{"))
            //            {
            //                waitingForBody = false;
            //            }
            //            else if (line.StartsWith("[static("))
            //                length = 8;
            //            else if (line.StartsWith("[composable("))
            //                length = 12;
            //            else if (line.StartsWith("[activatable("))
            //                length = 13;

            //            if (length != 0)
            //            {
            //                var iface = line.Substring(length).Split(',')[0];
            //                var ifaceType = ReflectionTypeViewModel.LookupByName(iface);
            //                sb.AppendLine(GetHelper(ifaceType, iface));
            //            }
            //        }
            //        else
            //        {
            //            if (line.StartsWith("}"))
            //                break;

            //            var ifaceString = "interface";
            //            var start = line.IndexOf(ifaceString) + ifaceString.Length + 1;
            //            var end = line.LastIndexOf(";");
            //            var iface = line.Substring(start, end-start);


            //            if( start == -1 || end == -1 || start > end )
            //            {
            //                DebugLog.Start("Failed to parse IDL: " + line);
            //                sb.AppendLine("Parsing error on: " + line);
            //            }
            //            else
            //            {
            //                var ifaceType = ReflectionTypeViewModel.LookupByName(iface);
            //                sb.AppendLine(GetHelper(ifaceType, iface));
            //            }
            //        }
            //    }


            //    return sb.ToString();
            //}
            //catch (Exception e)
            //{
            //    var message = "Failed to get IDL information:\r\n" + e.Message;
            //    DebugLog.Start(message);
            //    return message;
            //}

        }


        static string GetHelper(TypeViewModel type, string originalTypeName)
        {
            string idlFileName;
            string justTheFileName;
            string winmdFileIdlDir;
            string failureString = "IDL unavailable for " + originalTypeName;
            Dictionary<string, TypeIndices> typeIndicesMap = null;

            if (type == null)
            {
                if (originalTypeName.Contains('<'))
                    return "IDL not available for parameterized types (" + originalTypeName + ")";
                else
                    return failureString;
            }

            if( originalTypeName.Contains('`'))
            {
                return "IDL not supported for parameterized types";
            }


            var adjustedTypeNamespace = type.Namespace;
            if (type.Namespace == "Windows.Foundation.Collections")
            {
                // Don't know why WinMDIdl does this
                adjustedTypeNamespace = "Windows.Foundation";
            }

            if (!_indices.TryGetValue(adjustedTypeNamespace, out typeIndicesMap))
            {
                typeIndicesMap = new Dictionary<string, TypeIndices>();

                FigureOutFileNames(type.AssemblyLocation, out justTheFileName, out winmdFileIdlDir);

                var indexFileName = IndexFileFromIdlFile(winmdFileIdlDir + @"\" + adjustedTypeNamespace + ".idl");

                using (var idlFile = File.Open(indexFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var reader = new StreamReader(idlFile);
                    while (true)
                    {
                        var line = reader.ReadLine();
                        if (line == null)
                            break;

                        var typeName = line;
                        var indicesLine = reader.ReadLine();
                        var split = indicesLine.Split(':');
                        var indices = new TypeIndices();
                        indices.StartPosition = int.Parse(split[0]);
                        indices.Indent = int.Parse(split[1]);
                        indices.EndPosition = int.Parse(split[2]);

                        typeIndicesMap[typeName] = indices;
                    }
                }

                _indices.Add(type.Namespace, typeIndicesMap);
            }


            TypeIndices typeIndices;
            if (!typeIndicesMap.TryGetValue(type.Name, out typeIndices))
                return failureString;

            var sb = new StringBuilder();

            FigureOutFileNames(type.AssemblyLocation, out justTheFileName, out winmdFileIdlDir);

            idlFileName = winmdFileIdlDir + @"\\" + adjustedTypeNamespace + ".idl";

            try
            {
                using (var file = File.Open(idlFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    file.Seek(typeIndices.StartPosition, SeekOrigin.Begin);

                    var length = typeIndices.EndPosition - typeIndices.StartPosition + 1;
                    var buffer = new byte[length];

                    file.Read(buffer, 0, length);
                    var stream = new MemoryStream(buffer);
                    var reader = new StreamReader(stream);


                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();

                        if (line.Length >= typeIndices.Indent)
                            line = line.Substring(typeIndices.Indent);

                        sb.Append(line + "\r\n"); // AppendLine doesn't copy/paste well
                    }
                }
            }
            catch (Exception e)
            {
                DebugLog.Start("Couldn't read IDL for " + type.FullName);
                DebugLog.Append(e.Message);

                return "Couldn't open IDL file";
            }

            return sb.ToString();

        }

    }
}
