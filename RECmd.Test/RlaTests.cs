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

        var out2 = Path.Combine(Dir, "out2");
        Run("rla", "-f", hive, "--out", out2, "--ca", "false").ExitCode.Should().Be(0);
        Directory.Exists(out2).Should().BeFalse();
    }

    [Test]
    public void ExitCodes()
    {
        Run("rla", "-f", Path.Combine(Dir, "missing"), "--out", Dir).ExitCode.Should().Be(1);
        Run("rla", "-f", Hive()).ExitCode.Should().Be(1, "--out is required");
        Run("rla").ExitCode.Should().Be(1);

        File.WriteAllText(Path.Combine(Dir, "notahive"), "not a hive at all");
        Run("rla", "-f", Path.Combine(Dir, "notahive"), "--out", Path.Combine(Dir, "out")).ExitCode.Should().Be(1);
    }
}
