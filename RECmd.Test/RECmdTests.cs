using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace RECmd.Test;

[TestFixture]
public class RECmdTests : ToolTests
{
    private const string Batch = @"Description: test
Author: test
Version: 1
Id: 6b7f2d7a-2f8e-4c41-9a4a-6b1b0d0d1a11
Keys:
    -
        Description: Time zone
        HiveType: SYSTEM
        Category: System Info
        KeyPath: ControlSet001\Control\TimeZoneInformation
        ValueName: TimeZoneKeyName
        Recursive: false
        Comment: The time zone
";

    [Test]
    public void BatchCsv()
    {
        var hive = Hive();
        var bn = Path.Combine(Dir, "test.reb");
        File.WriteAllText(bn, Batch);

        var (code, output) = Run("RECmd", "-f", hive, "--bn", bn, "--csv", Path.Combine(Dir, "out"), "--csvf", "a.csv");

        code.Should().Be(0, output);
        var lines = File.ReadAllLines(Path.Combine(Dir, "out", "a.csv"));
        lines[0].Should().Be("HivePath,HiveType,Description,Category,KeyPath,ValueName,ValueType,ValueData,ValueData2,ValueData3,Comment,Recursive,Deleted,LastWriteTimestamp,PluginDetailFile");
        lines.Should().HaveCount(2);
        lines[1].Should().Contain(@"ControlSet001\Control\TimeZoneInformation,TimeZoneKeyName,RegSz,");
    }

    [Test]
    public void BadBatchFile()
    {
        var bn = Path.Combine(Dir, "bad.reb");
        File.WriteAllText(bn, "Description: test\nKeys:\n\t- KeyPath: x\n");

        //it used to exit with 0
        Run("RECmd", "-f", Hive(), "--bn", bn, "--csv", Path.Combine(Dir, "out")).ExitCode.Should().Be(1);
    }

    [Test]
    public void KeyDetails()
    {
        var (code, output) = Run("RECmd", "-f", Hive(), "--kn", @"ControlSet001\Control\TimeZoneInformation");

        code.Should().Be(0, output);
        output.Should().Contain("TimeZoneKeyName");
        //malformed templates printed the template itself
        output.Should().NotContain("{Del");
    }

    [Test]
    public void DirtyHiveWithoutLogs()
    {
        var hive = Hive(false);

        //reported as a hive that could not be processed; the hive is not bad, its logs are missing (give them, or use --nl): exit code 1
        var (code, output) = Run("RECmd", "-f", hive, "--kn", @"ControlSet001\Control\TimeZoneInformation");
        code.Should().Be(1, output);
        output.Should().Contain("could not be processed");
        Run("RECmd", "-f", hive, "--kn", @"ControlSet001\Control\TimeZoneInformation", "--nl").ExitCode.Should().Be(0);
    }

    [Test]
    public void UnreadableSubfolder()
    {
        if (!System.OperatingSystem.IsWindows())
        {
            Assert.Ignore("Folder permissions are set with Windows ACLs");
            return;
        }

        Hive();
        var locked = Path.Combine(Dir, "hives", "locked");
        var bn = Path.Combine(Dir, "test.reb");
        File.WriteAllText(bn, Batch);

        int code;
        string output;
        using (Unreadable(locked))
        {
            (code, output) = Run("RECmd", "-d", Path.Combine(Dir, "hives"), "--bn", bn, "--csv", Path.Combine(Dir, "out"), "--csvf", "a.csv");
        }

        //the folder is reported and the run goes on: the hive next to it is processed, exit code 1
        code.Should().Be(1, output);
        output.Should().Contain($"Unable to read folder {locked}");
        File.ReadAllLines(Path.Combine(Dir, "out", "a.csv")).Should().HaveCount(2);
    }

    [Test]
    public void ExitCodes()
    {
        Run("RECmd", "-f", Path.Combine(Dir, "missing"), "--kn", "x").ExitCode.Should().Be(1);
        Run("RECmd").ExitCode.Should().Be(1);
        Run("RECmd", "-f", Hive(), "--bn", Path.Combine(Dir, "missing.reb"), "--csv", Dir).ExitCode.Should().Be(1);

        //a damaged hive is reported and exits 0
        File.WriteAllText(Path.Combine(Dir, "notahive"), "not a hive at all");
        var (code, output) = Run("RECmd", "-f", Path.Combine(Dir, "notahive"), "--kn", "x");
        code.Should().Be(0, output);
        output.Should().Contain("could not be processed");

        //output that can't be written: --csv is a file
        var bn = Path.Combine(Dir, "test.reb");
        File.WriteAllText(bn, Batch);
        var csvFile = Path.Combine(Dir, "afile");
        File.WriteAllText(csvFile, "");
        Run("RECmd", "-f", Hive(), "--bn", bn, "--csv", csvFile).ExitCode.Should().Be(1);
    }

    [Test]
    public void Recover()
    {
        var hive = Hive();

        //--recover false used to be ignored
        var (code, withDeleted) = Run("RECmd", "-f", hive, "--sk", "USBSTOR");
        code.Should().Be(0, withDeleted);
        var (code2, without) = Run("RECmd", "-f", hive, "--sk", "USBSTOR", "--recover", "false");
        code2.Should().Be(0, without);

        without.Should().NotContain("(Deleted: True)");
        if (withDeleted.Contains("(Deleted: True)") == false)
        {
            Assert.Inconclusive("The test hive has no deleted keys matching the search");
        }
    }
    [Test]
    public void ConsoleLog()
    {
        //Issues #259: --co (on by default) saves the console messages to <yyyyMMddHHmmss>_RECmd_<version>_ConsoleLog.txt in the first output directory given
        var hive = Hive();
        var bn = Path.Combine(Dir, "test.reb");
        File.WriteAllText(bn, Batch);
        var csv = Path.Combine(Dir, "out");
        var json = Path.Combine(Dir, "json");

        var (code, output) = Run("RECmd", "-f", hive, "--bn", bn, "--csv", csv);
        code.Should().Be(0, output);
        var log = Directory.GetFiles(csv, "*_ConsoleLog.txt").Should().ContainSingle().Subject;
        Path.GetFileName(log).Should().MatchRegex(@"^\d{14}_RECmd_\d+\.\d+\.\d+_ConsoleLog\.txt$");
        output.Should().Contain("Console log: " + log);
        //the same time stamp as the batch output
        Directory.GetFiles(csv, "*_Output.csv").Should().ContainSingle().Which.Should().Contain(Path.GetFileName(log)[..14]);
        var text = File.ReadAllText(log);
        text.Should().MatchRegex(@"\[\d\d:\d\d:\d\d\.\d{3} INF\] RECmd version ");
        text.Should().Contain("Command line: -f ");

        //--json (a key's JSON)
        (code, output) = Run("RECmd", "-f", hive, "--kn", @"ControlSet001\Control\TimeZoneInformation", "--json", json);
        code.Should().Be(0, output);
        Directory.GetFiles(json, "*_RECmd_*_ConsoleLog.txt").Should().ContainSingle();

        //--saveTo: its folder
        var saved = Path.Combine(Dir, "saved");
        (code, output) = Run("RECmd", "-f", hive, "--kn", @"ControlSet001\Control\TimeZoneInformation", "--vn", "TimeZoneKeyName", "--saveTo", Path.Combine(saved, "value.bin"));
        code.Should().Be(0, output);
        Directory.GetFiles(saved, "*_RECmd_*_ConsoleLog.txt").Should().ContainSingle();

        //--co false: none
        var other = Path.Combine(Dir, "other");
        (code, output) = Run("RECmd", "-f", hive, "--bn", bn, "--csv", other, "--co", "false");
        code.Should().Be(0, output);
        Directory.GetFiles(other, "*_ConsoleLog.txt").Should().BeEmpty();
        output.Should().NotContain("Console log:");

        //no output directory: no log, and no warning (--co is on by default)
        (code, output) = Run("RECmd", "-f", hive, "--kn", @"ControlSet001\Control\TimeZoneInformation");
        code.Should().Be(0, output);
        output.Should().NotContain("--co:").And.NotContain("Console log:");
    }
}
