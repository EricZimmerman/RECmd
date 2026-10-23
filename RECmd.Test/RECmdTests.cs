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

        //reported as a hive that could not be processed, but a dirty hive is not a failure of the tool: exit code 0
        var (code, output) = Run("RECmd", "-f", hive, "--kn", @"ControlSet001\Control\TimeZoneInformation");
        code.Should().Be(0, output);
        output.Should().Contain("could not be processed");
        Run("RECmd", "-f", hive, "--kn", @"ControlSet001\Control\TimeZoneInformation", "--nl").ExitCode.Should().Be(0);
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
}
