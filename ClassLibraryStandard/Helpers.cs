using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Tempo
{
    public class Helpers
    {
        /// <summary>
        /// For each provided path, if it's a file add it to the return list,
        /// if it's a directory then add its children files to the return list
        /// </summary>
        static public List<string> ExpandDirectories(IEnumerable<string> paths)
        {
            var newList = new List<string>();

            foreach (var path in paths)
            {
                if (File.Exists(path))
                {
                    newList.Add(path);
                }

                else if (Directory.Exists(path))
                {
                    var files = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                        .Where(f => IsSupportedExtension(f))
                        .ToList();
                    newList.AddRange(files);
                }
            }

            return newList;
        }

        /// <summary>
        /// For the provided path, if it's a file add it to the return list,
        /// if it's a directory then add its children files to the return list
        /// </summary>
        static public List<string> ExpandDirectories(string path)
        {
            return ExpandDirectories(new string[] { path });
        }

        private static bool IsSupportedExtension(string filename)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            return ext == ".dll" || ext == ".winmd" || ext == ".nupkg";
        }

        /// <summary>
        /// Find the subdirectory with the highest version number (e.g. "8.0.5") under the given path.
        /// Returns the full path, or null if not found.
        /// </summary>
        static public string FindHighestVersionDirectory(string basePath)
        {
            if (!Directory.Exists(basePath))
            {
                return null;
            }

            string bestPath = null;
            Version bestVersion = null;

            foreach (var dir in Directory.GetDirectories(basePath))
            {
                var leaf = Path.GetFileName(dir);
                if (Version.TryParse(leaf, out var v))
                {
                    if (bestVersion == null || v > bestVersion)
                    {
                        bestVersion = v;
                        bestPath = dir;
                    }
                }
            }

            return bestPath;
        }
    }
}
