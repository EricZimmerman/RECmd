using System.IO;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using rla;

namespace RECmd.Test;

/// <summary>Where rla saves a hive under --out (issue #112)</summary>
[TestFixture]
public class OutputNamesTests
{
    private static readonly string Out = Path.Combine(Path.GetTempPath(), "out");

    [Test]
    public void PathIsRecreated()
    {
        var hive = Path.Combine(Path.GetTempPath(), "triage", "C", "Windows", "System32", "config", "SAM");
        var expected = Path.Combine(new[] { Out }.Concat(Path.GetFullPath(hive).Split('\\', '/').Where(t => t.Length > 0).Select(t => t.TrimEnd(':'))).ToArray());

        new OutputNames().Get(hive, Out, false, true).Should().Be(expected);
        new OutputNames().Get(hive, Out, false, false).Should().Be(expected, "--cn only names hives that go straight into --out");
    }

    [Test]
    public void SameNameGetsANumber()
    {
        var names = new OutputNames();

        //with --nop, hives from different folders would overwrite each other
        names.Get(Path.Combine("a", "SAM"), Out, true, false).Should().Be(Path.Combine(Out, "SAM"));
        names.Get(Path.Combine("b", "SAM"), Out, true, false).Should().Be(Path.Combine(Out, "SAM_2"));
        names.Get(Path.Combine("c", "SAM"), Out, true, false).Should().Be(Path.Combine(Out, "SAM_3"));
        names.Get(Path.Combine("d", "NTUSER.DAT"), Out, true, false).Should().Be(Path.Combine(Out, "NTUSER.DAT"));
        names.Get(Path.Combine("e", "ntuser.dat"), Out, true, false).Should().Be(Path.Combine(Out, "ntuser_2.dat"));
    }

    [TestCase(@"C:\Users\eric\NTUSER.DAT", "C", "eric")]
    [TestCase(@"C:\Users\eric\AppData\Local\Microsoft\Windows\UsrClass.dat", "C", "eric")]
    [TestCase(@"E:\triage\C\Users\eric\NTUSER.DAT", "C", "eric")]
    [TestCase(@"E:\Documents and Settings\bob\NTUSER.DAT", "E", "bob")]
    [TestCase(@"C:\Windows\ServiceProfiles\LocalService\NTUSER.DAT", "C", "LocalService")]
    [TestCase("/mnt/evidence/C/Users/eric/NTUSER.DAT", "C", "eric")]
    [TestCase("/mnt/evidence/NTUSER.DAT", "Undetermined", "Undetermined")]
    public void ProfileHives(string hive, string drive, string profile)
    {
        OutputNames.Drive(hive).Should().Be(drive);
        OutputNames.Profile(hive).Should().Be(profile);
    }

    [Test]
    public void ProfileHivesWithNop()
    {
        var names = new OutputNames();

        names.Get(@"C:\Users\eric\NTUSER.DAT", Out, true, true).Should().Be(Path.Combine(Out, "C_eric_NTUSER.DAT"));
        names.Get(@"C:\Users\bob\NTUSER.DAT", Out, true, true).Should().Be(Path.Combine(Out, "C_bob_NTUSER.DAT"));
        names.Get(@"C:\Windows\System32\config\SYSTEM", Out, true, true).Should().Be(Path.Combine(Out, "SYSTEM"));
        names.Get(@"C:\Users\eric\NTUSER.DAT", Out, true, false).Should().Be(Path.Combine(Out, "NTUSER.DAT"), "the first one was saved as C_eric_NTUSER.DAT");
        names.Get(@"C:\Users\bob\NTUSER.DAT", Out, true, false).Should().Be(Path.Combine(Out, "NTUSER_2.DAT"));
    }
}
