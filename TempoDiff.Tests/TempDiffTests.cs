using System;
using System.IO;
using System.Reflection;
using Tempo;
using TempoDiff;

namespace TempoDiff.Tests
{
    public class TempDiffTests
    {
        [Fact]
        public void Main_WithNoArguments_ReturnsUsageError()
        {
            var exitCode = InvokeMain(new string[] { });
            Assert.Equal(1, exitCode);
        }

        [Fact]
        public void Main_WithOneArgument_ReturnsUsageError()
        {
            var exitCode = InvokeMain(new string[] { "path1" });
            Assert.Equal(1, exitCode);
        }

        [Fact]
        public void Main_WithTestDllFiles_OutputMatchesExpectedDiff()
        {
            AssertOutputMatchesExpectedDiff("diff.txt");
        }

        [Fact]
        public void Main_WithTestDllFiles_CsvOutputMatchesExpectedDiff()
        {
            AssertOutputMatchesExpectedDiff("diff-csv.txt", "/csv");
        }

        [Fact]
        public void Main_WithTestDllFiles_FqnOutputMatchesExpectedDiff()
        {
            AssertOutputMatchesExpectedDiff("diff-fqn.txt", "/fqn");
        }

        [Fact]
        public void Main_WithTestDllFiles_CsvFqnOutputMatchesExpectedDiff()
        {
            // Also validate that both "/" and "--" switch prefixes work
            AssertOutputMatchesExpectedDiff("diff-csv-fqn.txt", "/csv", "--fqn");
        }

        [Fact]
        public void Main_WithTestDllFiles_ShowExpOutputMatchesExpectedDiff()
        {
            AssertOutputMatchesExpectedDiff("diff-showexp.txt", "/showexp");
        }

        [Fact]
        public void Main_WithTestDllFiles_ShowExpCsvOutputMatchesExpectedDiff()
        {
            // Also validate case-inensitive
            AssertOutputMatchesExpectedDiff("diff-showexp-csv.txt", "/shOWexp", "/cSv");
        }

        [Fact]
        public void Main_WithTestDllFiles_ShowExpFqnOutputMatchesExpectedDiff()
        {
            AssertOutputMatchesExpectedDiff("diff-showexp-fqn.txt", "/showexp", "/fqn");
        }

        private static void AssertOutputMatchesExpectedDiff(string expectedDiffFileName, params string[] additionalArgs)
        {
            var testAssembly = Assembly.GetExecutingAssembly();
            var testDirectory = Path.GetDirectoryName(testAssembly.Location);
            var assetsPath = Path.Combine(testDirectory!, "Assets");

            var d1Path = Path.Combine(assetsPath, "d1.dll");
            var d2Path = Path.Combine(assetsPath, "d2.dll");
            var expectedDiffPath = Path.Combine(assetsPath, expectedDiffFileName);

            Assert.True(File.Exists(d1Path), $"Test asset not found: {d1Path}");
            Assert.True(File.Exists(d2Path), $"Test asset not found: {d2Path}");
            Assert.True(File.Exists(expectedDiffPath), $"Expected diff file not found: {expectedDiffPath}");

            var originalOut = Console.Out;
            var originalError = Console.Error;
            try
            {
                using var outWriter = new StringWriter();
                using var errorWriter = new StringWriter();
                Console.SetOut(outWriter);
                Console.SetError(errorWriter);

                var args = new[] { d1Path, d2Path }.Concat(additionalArgs).ToArray();
                var exitCode = InvokeMainDirect(args);

                Assert.Equal(0, exitCode);

                var actualOutput = outWriter.ToString();
                var expectedOutput = File.ReadAllText(expectedDiffPath);

                actualOutput = NormalizeLineEndings(actualOutput.Trim());
                expectedOutput = NormalizeLineEndings(expectedOutput.Trim());

                Assert.Equal(expectedOutput, actualOutput);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }

        private static string NormalizeLineEndings(string text)
        {
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        [Fact]
        public void Main_HelpOutput_ContainsUsageInformation()
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            try
            {
                using var outWriter = new StringWriter();
                using var errorWriter = new StringWriter();
                Console.SetOut(outWriter);
                Console.SetError(errorWriter);

                InvokeMainDirect(new string[] { });

                var errorOutput = errorWriter.ToString();
                Assert.Contains("Usage:", errorOutput);
                Assert.Contains("/csv", errorOutput);
                Assert.Contains("/fqn", errorOutput);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }

        private static int InvokeMain(string[] args)
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            
            try
            {
                using var outWriter = new StringWriter();
                using var errorWriter = new StringWriter();
                Console.SetOut(outWriter);
                Console.SetError(errorWriter);

                return InvokeMainDirect(args);
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }

        private static int InvokeMainDirect(string[] args)
        {
            var programType = typeof(TempoDiff.Program);
            var mainMethod = programType.GetMethod("Main", BindingFlags.Static | BindingFlags.NonPublic);
            
            if (mainMethod == null)
            {
                throw new InvalidOperationException("Could not find Main method");
            }

            var result = mainMethod.Invoke(null, new object[] { args });
            return result != null ? (int)result : 1;
        }
    }
}
