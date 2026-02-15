using System;
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
        internal static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: tempo-diff <baselinePath> <newPath> [/csv] [/fqn] [/showexp\n");

                Console.Error.WriteLine("Paths can be a managed assembly, a Windows winmd, a Nuget nupkg, or a directory\n");

                Console.Error.WriteLine("Optional flags:\n" +
                                        "  /csv       Output results in CSV format\n" +
                                        "  /fqn       Output type/member names in fully-qualified format\n" +
                                        "  /showexp   Highlight experimental APIs in the output\n");

                Console.Error.WriteLine("E.g.:  tempo-diff ver1.dll ver2.dll\n" +
                                        "       tempo-diff ver1.winmd ver2.winmd /fqn\n" +
                                        "       tempo-diff ver1.nupkg ver2.nupkg /csv\n" +
                                        "       tempo-diff dir1 dir2 /showexp /csv\n");

                Console.Error.WriteLine("To see the diff in the UX app, use `tempo /diff`, e.g.:\n" +
                                        "  tempo /diff dir1 dir2\n");

                return 1;
            }

            var baselineArg = args[0];
            var newArg = args[1];
            var asCsv = IsSwitchSet(args, "csv");
            var flat = IsSwitchSet(args, "fqn");
            var showExperimental = IsSwitchSet(args, "showexp");
            try
            {

                // Initialize shared library across solution
                // We won't be downloading any packages from nuget.org, so don't need a packagedCachePath
                DesktopManager2.Initialize(wpfApp: false, packagesCachePath: null);
                DesktopManager2.CommandLineMode = true; // forces sync

                // Put the search into diff mode
                Manager.ResetSettings();
                Manager.Settings.CompareToBaseline = true;

                // Expand folders to file lists
                var baselineFiles = Helpers.ExpandDirectories(baselineArg);
                var newFiles = Helpers.ExpandDirectories(newArg);

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
                var experimentalCount = 0;
                foreach (var result in resultsList)
                {
                    if (result is MemberOrTypeViewModelBase member && member.IsExperimental)
                    {
                        experimentalCount++;
                    }
                }

                // Convert to string using existing export helpers
                var output = CopyExport.ConvertItemsToABigString(
                    resultsList,
                    asCsv: asCsv,
                    flat: flat,
                    groupByNamespace: true,
                    compressTypes: true,
                    markExperimental: showExperimental);

                // Print and copy
                Console.OutputEncoding = Encoding.UTF8;
                Console.WriteLine(output);
                var summaryBuilder = new StringBuilder();
                summaryBuilder.Append($"Found {resultsList.Count} result items. Baseline types: {baselineTypeSet.TypeCount}, New types: {customTypeSet.TypeCount}.");
                var nonExperimentalCount = resultsList.Count - experimentalCount;
                summaryBuilder.Append($" Experimental: {experimentalCount}, Non-experimental: {nonExperimentalCount}.");
                Console.Error.WriteLine(summaryBuilder.ToString());

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.ToString());
                return 2;
            }
        }

        static bool IsSwitchSet(string[] args, string switchName)
        {
            return args.Contains($"/{switchName}", StringComparer.OrdinalIgnoreCase)
                   || args.Contains($"--{switchName}", StringComparer.OrdinalIgnoreCase);
        }
    }
}