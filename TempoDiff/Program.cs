using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Tempo;

namespace TempoDiff
{
    internal static class Program
    {
        // Contract for CLI
        // Inputs: two paths (files or directories) containing metadata sources (dll/winmd/nupkg)
        // Output: prints API diff (new vs baseline) to stdout
        // Exit codes: 0 success, 1 usage error, 2 load error.
        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: TempoDiff.exe <baselinePath> <newPath> [--csv] [--flat]");
                return 1;
            }

            var baselineArg = args[0];
            var newArg = args[1];
            var asCsv = args.Contains("--csv", StringComparer.OrdinalIgnoreCase);
            var flat = args.Contains("--flat", StringComparer.OrdinalIgnoreCase);

            try
            {
                // Initialize backend library without UI
                var localCache = Path.Combine(Path.GetTempPath(), "TempoDiffCache");
                Directory.CreateDirectory(localCache);
                DesktopManager2.Initialize(wpfApp: false, packagesCachePath: localCache);
                DesktopManager2.CommandLineMode = true; // forces sync
                // No UI dispatcher in CLI; run posted actions inline
                Manager.PostToUIThread = (action) => action();

                // Reset settings for a clean diff pass
                Manager.ResetSettings();
                Manager.Settings.CompareToBaseline = true;

                // Expand folders to file lists
                var baselineFiles = ExpandToFiles(baselineArg);
                var newFiles = ExpandToFiles(newArg);

                if (baselineFiles.Count == 0)
                {
                    Console.Error.WriteLine($"No files found in baseline path: {baselineArg}");
                    return 2;
                }
                if (newFiles.Count == 0)
                {
                    Console.Error.WriteLine($"No files found in new path: {newArg}");
                    return 2;
                }

                // Load baseline typeset
                var baselineTypeSet = new MRTypeSet("Baseline", usesWinRTProjections: true);
                DesktopManager2.LoadTypeSetMiddleweightReflection(baselineTypeSet, baselineFiles.ToArray());
                if (baselineTypeSet.Types == null || baselineTypeSet.Types.Count == 0)
                {
                    Console.Error.WriteLine($"No APIs found in baseline. Files: {string.Join(", ", baselineFiles)}");
                    return 2;
                }
                Manager.BaselineTypeSet = baselineTypeSet;

                // Load new/custom typeset
                var customTypeSet = new MRTypeSet(MRTypeSet.CustomMRName, usesWinRTProjections: true);
                DesktopManager2.LoadTypeSetMiddleweightReflection(customTypeSet, newFiles.ToArray());
                if (customTypeSet.Types == null || customTypeSet.Types.Count == 0)
                {
                    Console.Error.WriteLine($"No APIs found in new set. Files: {string.Join(", ", newFiles)}");
                    return 2;
                }
                Manager.CustomMRTypeSet = customTypeSet;
                Manager.CurrentTypeSet = customTypeSet;

                // Build a trivial search that returns everything new vs baseline
                var search = new SearchExpression();
                search.RawValue = string.Empty; // no name filter

                var iteration = ++Manager.RecalculateIteration;
                var results = Manager.GetMembers(search, iteration);
                var resultsList = results.ToList();

                // Convert to string using existing export helpers
                var output = CopyExport.ConvertItemsToABigString(
                    resultsList,
                    asCsv: asCsv,
                    flat: flat,
                    groupByNamespace: true,
                    compressTypes: true);

                // Print and copy
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine(output);
                Console.Error.WriteLine($"Found {resultsList.Count} result items. Baseline types: {baselineTypeSet.TypeCount}, New types: {customTypeSet.TypeCount}.");

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 2;
            }
        }

        private static List<string> ExpandToFiles(string path)
        {
            var paths = new List<string>();
            if (File.Exists(path))
            {
                paths.Add(path);
                return paths;
            }

            if (Directory.Exists(path))
            {
                var files = Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => HasSupportedExtension(f))
                    .ToList();
                paths.AddRange(files);
                return paths;
            }

            // Not found; return empty to be handled by caller
            return paths;
        }

        private static bool HasSupportedExtension(string filename)
        {
            var ext = Path.GetExtension(filename).ToLowerInvariant();
            return ext == ".dll" || ext == ".winmd" || ext == ".nupkg";
        }
    }
}