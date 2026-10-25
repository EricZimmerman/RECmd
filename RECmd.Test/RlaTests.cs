using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace RECmd.Test;

[TestFixture]
public class RlaTests : ToolTests
{
    [Test]
    public void DirtyHiveIsReplayedUnderItsPath()
    {
        var hive = Hive();
        var @out = Path.Combine(Dir, "out");

        var (code, output) = Run("rla", "-f", hive, "--out", @out);

        code.Should().Be(0, output);
        IsClean(hive).Should().BeFalse();

        //--nop false (the default): the hive's path under --out (#112)
        var saved = Directory.GetFiles(@out, "*", SearchOption.AllDirectories).Single();
        Path.GetFileName(saved).Should().Be("SYSTEM");
        saved.Should().EndWith(Path.Combine("hives", "SYSTEM"));
        IsClean(saved).Should().BeTrue();
    }

    [Test]
    public void NopDoesNotOverwrite()
    {
        Hive(folder: "a");
        Hive(folder: "b");
        var @out = Path.Combine(Dir, "out");

        var (code, output) = Run("rla", "-d", Dir, "--out", @out, "--nop", "true");

        code.Should().Be(0, output);
        //two SYSTEM hives: the second one used to overwrite the first
        Directory.GetFiles(@out).Select(Path.GetFileName).Should().BeEquivalentTo("SYSTEM", "SYSTEM_2");
        Directory.GetFiles(@out).Should().OnlyContain(t => IsClean(t));
    }

    [Test]
    public void DirtyHiveWithoutLogs()
    {
        var hive = Hive(false);
        var @out = Path.Combine(Dir, "out");

        //--ca (the default) copies it as it is
        Run("rla", "-f", hive, "--out", @out).ExitCode.Should().Be(0);
        Directory.GetFiles(@out, "*", SearchOption.AllDirectories).Should().ContainSingle();

        //--ca false skips it; the hive is not bad, its logs are missing: exit code 1
        var out2 = Path.Combine(Dir, "out2");
        var (code, output) = Run("rla", "-f", hive, "--out", out2, "--ca", "false");
        code.Should().Be(1, output);
        output.Should().Contain("could not be processed");
        Directory.Exists(out2).Should().BeFalse();
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
        var @out = Path.Combine(Dir, "out");

        int code;
        string output;
        using (Unreadable(locked))
        {
            (code, output) = Run("rla", "-d", Path.Combine(Dir, "hives"), "--out", @out, "--nop", "true");
        }

        //the folder is reported and the run goes on: the hive next to it is saved, exit code 1
        code.Should().Be(1, output);
        output.Should().Contain($"Unable to read folder {locked}");
        Directory.GetFiles(@out).Select(Path.GetFileName).Should().BeEquivalentTo("SYSTEM");
    }

    [Test]
    public void ExitCodes()
    {
        Run("rla", "-f", Path.Combine(Dir, "missing"), "--out", Dir).ExitCode.Should().Be(1);
        Run("rla", "-f", Hive()).ExitCode.Should().Be(1, "--out is required");
        Run("rla").ExitCode.Should().Be(1);

        File.WriteAllText(Path.Combine(Dir, "notahive"), "not a hive at all");
        //a damaged hive is reported and exits 0
        var (code, output) = Run("rla", "-f", Path.Combine(Dir, "notahive"), "--out", Path.Combine(Dir, "out"));
        code.Should().Be(0, output);
        output.Should().Contain("could not be processed");

        //output that can't be written: --out is a file
        var outFile = Path.Combine(Dir, "afile");
        File.WriteAllText(outFile, "");
        Run("rla", "-f", Hive(), "--out", outFile).ExitCode.Should().Be(1);
    }
    [Test]
    public void ConsoleLog()
    {
        //Issues #259: --co saves the console messages to <yyyyMMddHHmmss>_rla_<version>_ConsoleLog.txt in --out
        var hive = Hive();
        var @out = Path.Combine(Dir, "out");

        var (code, output) = Run("rla", "-f", hive, "--out", @out, "--co");
        code.Should().Be(0, output);
        var log = Directory.GetFiles(@out, "*_ConsoleLog.txt").Should().ContainSingle().Subject;
        Path.GetFileName(log).Should().MatchRegex(@"^\d{14}_rla_\d+\.\d+\.\d+_ConsoleLog\.txt$");
        output.Should().Contain("Console log: " + log);
        var text = File.ReadAllText(log);
        text.Should().MatchRegex(@"\[\d\d:\d\d:\d\d\.\d{3} INF\] rla version ");
        text.Should().Contain("Command line: -f ");

        var other = Path.Combine(Dir, "other");
        (code, output) = Run("rla", "-f", hive, "--out", other);
        code.Should().Be(0, output);
        Directory.GetFiles(other, "*_ConsoleLog.txt", SearchOption.AllDirectories).Should().BeEmpty();
        output.Should().NotContain("Console log:");
    }
}
