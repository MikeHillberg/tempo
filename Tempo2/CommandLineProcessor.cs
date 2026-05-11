using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Tempo
{
    /// <summary>
    /// Parses and normalizes command-line arguments.
    /// Accepts raw args from Environment.GetCommandLineArgs() (where args[0] is the exe name).
    /// </summary>
    class CommandLineProcessor
    {
        /// <summary>
        /// Command line arguments (skip exe name) with normalized prefixes:
        /// --foo, -foo, /foo all become /foo. Lowercased for flags, original case for filenames.
        /// </summary>
        public string[] NormalizedCommandArgs { get; }

        /// <summary>
        /// True if /? or /help was specified on the command line
        /// </summary>
        public bool NeedHelp => HasCommandLineFlag("?") || HasCommandLineFlag("help");

        /// <summary>
        /// True if /ps was specified on the command line.
        /// </summary>
        public bool HasPS => HasCommandLineFlag("ps");

        /// <summary>
        /// Filenames following the /ps flag, or null if /ps was not specified.
        /// </summary>
        public List<string>? PSFilenames => GetCommandLineFilenames("ps");

        /// <summary>
        /// True if /waitfordebugger was specified on the command line
        /// </summary>
        public bool NeedWaitForDebugger => HasCommandLineFlag("waitfordebugger");

        /// <summary>
        /// True if /singleinstance was specified on the command line
        /// </summary>
        public bool ShouldAllowSingleInstance => HasCommandLineFlag("singleinstance");


        /// <summary>
        /// The API scope specified on the command line, or null if none was specified.
        /// </summary>
        public string? ApiScope { get; private set; }

        /// <summary>
        /// True if a valid /diff was parsed (both baseline and custom file were provided)
        /// </summary>
        public bool IsDiffMode { get; private set; }

        /// <summary>
        /// The baseline filename from /diff (first file after /diff). Null if no /diff.
        /// </summary>
        public string? DiffBaselineFilename { get; private set; }

        /// <summary>
        /// Custom filenames parsed from the command line (bare filenames and the second /diff file).
        /// Null if none were provided.
        /// </summary>
        public List<string>? CustomFilenames { get; private set; }

        /// <summary>
        /// True if /diff was specified but didn't have enough arguments.
        /// </summary>
        public bool HasIncompleteDiff { get; private set; }

        /// <summary>
        /// If a filename argument couldn't be resolved to a full path, this is the bad argument.
        /// </summary>
        public string? InvalidPathArgument { get; private set; }

        public CommandLineProcessor(string[] commandLineArgs)
        {
            if (commandLineArgs == null || commandLineArgs.Length <= 1)
            {
                NormalizedCommandArgs = Array.Empty<string>();
                return;
            }

            // Normalize args from --foo or -foo to /foo, and make lower case
            var result = new string[commandLineArgs.Length - 1];
            for (int i = 1; i < commandLineArgs.Length; i++)
            {
                var arg = commandLineArgs[i];
                var lower = arg.ToLower();
                if (lower.StartsWith("--"))
                {
                    result[i - 1] = "/" + lower[2..];
                }
                else if (lower.StartsWith("-") || lower.StartsWith("/"))
                {
                    result[i - 1] = "/" + lower[1..];
                }
                else
                {
                    result[i - 1] = arg; // preserve original case for filenames
                }
            }
            NormalizedCommandArgs = result;

            // Parse API scope flag
            ParseApiScope();

            // Parse /diff and custom filenames
            ParseDiffAndCustomFilenames();
        }

        private static readonly string[] ApiScopeFlags = new[]
        {
            $"/{ApiScopeNames.WinAppSdk}", $"/{ApiScopeNames.Windows}", $"/{ApiScopeNames.Win32}",
            $"/{ApiScopeNames.WebView2}", $"/{ApiScopeNames.DotNet}", $"/{ApiScopeNames.DotNetWindows}"
        };

        /// <summary>
        /// Parse the normalized args for an API scope flag.
        /// </summary>
        private void ParseApiScope()
        {
            foreach (var arg in NormalizedCommandArgs)
            {
                if (ApiScopeFlags.Contains(arg))
                {
                    ApiScope = arg[1..]; // strip leading '/'
                    return;
                }
            }
        }

        /// <summary>
        /// Parse the normalized args for /diff and bare filename arguments.
        /// </summary>
        private void ParseDiffAndCustomFilenames()
        {
            var args = NormalizedCommandArgs;
            if (args.Length == 0)
            {
                return;
            }

            List<string>? customFilenames = null;
            string? baselineFilename = null;
            bool waitingForFirstDiff = false;
            bool waitingForSecondDiff = false;
            bool isDiffMode = false;

            for (int i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Length == 0)
                {
                    continue;
                }

                // Skip known flags that are handled elsewhere
                if (arg == "/waitfordebugger" || arg == "/singleinstance" || arg == "/ps"
                    || ApiScopeFlags.Contains(arg))
                {
                    continue;
                }

                // Skip args that follow /ps (they belong to PSFilenames)
                if (arg == "/ps")
                {
                    continue;
                }

                if (arg == "/diff")
                {
                    waitingForFirstDiff = true;
                    waitingForSecondDiff = false;
                    continue;
                }
                else if (waitingForFirstDiff)
                {
                    try
                    {
                        baselineFilename = Path.GetFullPath(arg);
                    }
                    catch
                    {
                        InvalidPathArgument = arg;
                        return;
                    }
                    waitingForSecondDiff = true;
                    waitingForFirstDiff = false;
                    continue;
                }
                else if (waitingForSecondDiff)
                {
                    isDiffMode = true;
                }
                waitingForFirstDiff = waitingForSecondDiff = false;

                // Skip other flags (but not filenames)
                if (arg.StartsWith("/"))
                {
                    continue;
                }

                customFilenames ??= new List<string>();

                try
                {
                    customFilenames.Add(Path.GetFullPath(arg));
                }
                catch
                {
                    InvalidPathArgument = arg;
                    return;
                }
            }

            IsDiffMode = isDiffMode;
            DiffBaselineFilename = baselineFilename;
            CustomFilenames = customFilenames;
            HasIncompleteDiff = waitingForFirstDiff || waitingForSecondDiff;
        }

        /// <summary>
        /// Check if a flag (e.g. "ps") is present on the command line
        /// </summary>
        public bool HasCommandLineFlag(string flag)
        {
            var normalized = "/" + flag.ToLower();
            return NormalizedCommandArgs.Any(a => a == normalized);
        }

        /// <summary>
        /// Get filenames that follow the specified flag on the command line.
        /// E.g. for "ps", returns all args after "/ps" until end or another "/" flag.
        /// </summary>
        public List<string>? GetCommandLineFilenames(string forFlag)
        {
            var target = "/" + forFlag.ToLower();
            var filenames = new List<string>();
            var args = NormalizedCommandArgs;
            bool collecting = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == target)
                {
                    collecting = true;
                    continue;
                }

                if (collecting)
                {
                    if (args[i].StartsWith("/"))
                    {
                        break;
                    }

                    try
                    {
                        filenames.Add(Path.GetFullPath(args[i]));
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Append(ex, $"Couldn't get argument {args[i]}");
                    }
                }
            }
            if (!collecting)
            {
                return null;
            }
            return filenames;
        }
    }
}
