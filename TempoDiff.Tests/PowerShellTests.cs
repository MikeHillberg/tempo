using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace Tempo.Tests;

[TestClass]
public class PowerShellTests
{
    /// <summary>
    /// Launch PowerShell with the Tempo drive mapped to d1.dll,
    /// list the types, and verify against expected output.
    /// </summary>
    [TestMethod]
    public void TempoCD_ListsTypesFromTestDll()
    {
        var output = RunTempoPS("Get-ChildItem Tempo:\\Types | Select-Object -ExpandProperty Name | Sort-Object");

        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.IsTrue(lines.Length > 0, $"Expected type names in output. Got: {output}");

        // d1.dll contains types in the ClassLibrary namespace
        CollectionAssert.Contains(lines, "ExampleClass", $"Expected ExampleClass in output. Got: {output}");
    }

    /// <summary>
    /// Launch PowerShell with the Tempo drive mapped to d1.dll,
    /// get members of a specific type, and verify output.
    /// </summary>
    [TestMethod]
    public void TempoCD_GetTypeProperties()
    {
        var output = RunTempoPS(
            "$t = Get-ChildItem Tempo:\\Types | Where-Object { $_.Name -eq 'ExampleClass' }; $t.Members | Select-Object -ExpandProperty Name | Sort-Object");

        var lines = output.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.IsTrue(lines.Length > 0, $"Expected member names in output. Got: {output}");

        Assert.IsTrue(Array.Exists(lines, l => l.Contains("Method")),
            $"Expected a Method member in output. Got: {string.Join(", ", lines)}");
    }

    /// <summary>
    /// Helper to launch pwsh with the Tempo drive mapped to d1.dll and run a command.
    /// Returns stdout.
    /// </summary>
    private string RunTempoPS(string command)
    {
        var testDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var assetsPath = Path.Combine(testDirectory, "Assets");
        var d1Path = Path.Combine(assetsPath, "d1.dll");
        Assert.IsTrue(File.Exists(d1Path), $"Test asset not found: {d1Path}");

        var tempoAppDir = FindTempoAppDir();
        Assert.IsNotNull(tempoAppDir, "Could not find Tempo app directory with MapTempoDrive.ps1");

        var mapScript = Path.Combine(tempoAppDir, "MapTempoDrive.ps1");
        var fullCommand = $". '{mapScript}'; {command}";

        var startInfo = new ProcessStartInfo("pwsh.exe")
        {
            Arguments = $"-ExecutionPolicy Bypass -NoProfile -NonInteractive -Command \"{fullCommand.Replace("\"", "\\\"")}\"",
            WorkingDirectory = tempoAppDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.Environment[TempoPSProvider.TempoPSProvider.PipeNameKey] = $"TempoPipe_{Guid.NewGuid()}";
        startInfo.Environment["Tempo_TypeSetName"] = "Custom";
        startInfo.Environment[TempoPSProvider.TempoPSProvider.UseWinRTProjectionsKey] = "1";
        startInfo.Environment[TempoPSProvider.TempoPSProvider.FilenamesKey] = d1Path + ";";

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        using var process = Process.Start(startInfo)!;
        process.OutputDataReceived += (s, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (s, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var exited = process.WaitForExit(30000);
        Assert.IsTrue(exited, "pwsh did not exit within timeout");
        Assert.AreEqual(0, process.ExitCode, $"pwsh exited with code {process.ExitCode}. stderr: {stderr}");

        var output = stdout.ToString().Trim();
        Assert.IsFalse(string.IsNullOrEmpty(output), $"No output from PowerShell. stderr: {stderr}");
        return output;
    }

    static string? FindTempoAppDir()
    {
        var buildRoot = Environment.GetEnvironmentVariable("TEMPO_BUILD_ROOT") ?? @"C:\build";
        var candidates = new[]
        {
            Path.Combine(buildRoot, "Tempo2", "x64", "Debug", "net8.0-windows10.0.19041", "win-x64", "AppX"),
            Path.Combine(buildRoot, "Tempo2", "x64", "Debug", "net8.0-windows10.0.19041", "win-x64"),
            Path.Combine(buildRoot, "Tempo2", "x64", "Debug"),
        };

        foreach (var dir in candidates)
        {
            if (File.Exists(Path.Combine(dir, "MapTempoDrive.ps1")))
                return dir;
        }
        return null;
    }
}
