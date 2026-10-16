using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using FluentAssertions;
using NUnit.Framework;

namespace RECmd.Test;

/// <summary>Runs RECmd.dll and rla.dll (built next to the tests) as separate processes, in a folder of their own</summary>
public abstract class ToolTests
{
    protected string Dir;

    [SetUp]
    public void SetUp()
    {
        Dir = Path.Combine(Path.GetTempPath(), "recmd-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Dir);
    }

    [TearDown]
    public void TearDown()
    {
        Directory.Delete(Dir, true);
    }

    /// <summary>
    ///     The DFIR Artifact Museum's dirty Windows 11 SYSTEM hive (and, with <paramref name="logs" />, its logs),
    ///     decompressed into <paramref name="folder" /> under the test's folder
    /// </summary>
    protected string Hive(bool logs = true, string folder = "hives")
    {
        var source = Path.Combine(TestContext.CurrentContext.TestDirectory, "TestFiles", "DFIRArtifactMuseum", "Win11", "RathbunVM");
        var target = Path.Combine(Dir, folder);
        Directory.CreateDirectory(target);

        foreach (var name in logs ? new[] { "SYSTEM", "SYSTEM.LOG1", "SYSTEM.LOG2" } : new[] { "SYSTEM" })
        {
            using var gz = new GZipStream(File.OpenRead(Path.Combine(source, name + ".gz")), CompressionMode.Decompress);
            using var output = File.Create(Path.Combine(target, name));
            gz.CopyTo(output);
        }

        return Path.Combine(target, "SYSTEM");
    }

    protected (int ExitCode, string Output) Run(string tool, params string[] args)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Dir
        };
        start.ArgumentList.Add(Path.Combine(TestContext.CurrentContext.TestDirectory, tool + ".dll"));
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        process.StandardInput.Close();

        process.WaitForExit(300_000).Should().BeTrue($"{tool} should finish");
        return (process.ExitCode, stdout.Result + stderr.Result);
    }

    /// <summary>A hive's primary and secondary sequence numbers are equal and its header checksum is right</summary>
    protected static bool IsClean(string hive)
    {
        var header = new byte[512];
        using (var fs = File.OpenRead(hive))
        {
            fs.ReadExactly(header);
        }

        var checksum = 0u;
        for (var i = 0; i < 508; i += 4)
        {
            checksum ^= BitConverter.ToUInt32(header, i);
        }

        if (checksum == 0) checksum = 1;
        if (checksum == 0xFFFFFFFF) checksum = 0xFFFFFFFE;

        return BitConverter.ToUInt32(header, 4) == BitConverter.ToUInt32(header, 8) && BitConverter.ToUInt32(header, 508) == checksum;
    }
}
