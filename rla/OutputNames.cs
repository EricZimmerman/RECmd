using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace rla;

/// <summary>Where a hive is saved under --out</summary>
internal class OutputNames
{
    private static readonly char[] Separators = { '\\', '/' };

    //Users\eric, Documents and Settings\eric, Windows\ServiceProfiles\LocalService (with either separator)
    private static readonly Regex ProfileRegex = new(@"(?:^|[\\/])(?:Users|Documents and Settings|ServiceProfiles)[\\/]([^\\/]+)[\\/]", RegexOptions.IgnoreCase);

    //a folder named for the drive, as KAPE makes (E:\triage\C\Users\...), else the path's drive (C:\...)
    private static readonly Regex DriveFolderRegex = new(@"[\\/]([A-Za-z])[\\/](?:Users|Documents and Settings|Windows)[\\/]", RegexOptions.IgnoreCase);
    private static readonly Regex DriveRegex = new(@"^([A-Za-z]):", RegexOptions.IgnoreCase);

    private readonly HashSet<string> _used = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     nop false: the hive's path under outDir, without the drive's colon (C:\Windows\System32\config\SAM is
    ///     outDir\C\Windows\System32\config\SAM). nop true: just the name, in outDir; with cn, profile hives (NTUSER.DAT,
    ///     UsrClass.dat) are named drive_profile_name. A name already used in this run gets a number (SAM_2)
    /// </summary>
    public string Get(string hive, string outDir, bool nop, bool cn)
    {
        //either separator, so Windows paths work everywhere
        var fileName = hive.Split(Separators).Last();
        string path;

        if (nop == false)
        {
            path = Path.Combine(new[] { outDir }.Concat(RelativeParts(Path.GetFullPath(hive))).ToArray());
        }
        else if (cn && IsProfileHive(fileName))
        {
            path = Path.Combine(outDir, $"{Drive(hive)}_{Profile(hive)}_{fileName}");
        }
        else
        {
            path = Path.Combine(outDir, fileName);
        }

        return Unique(path);
    }

    public static bool IsProfileHive(string fileName) =>
        fileName.IndexOf("NTUSER", StringComparison.OrdinalIgnoreCase) >= 0 ||
        fileName.IndexOf("USRCLASS", StringComparison.OrdinalIgnoreCase) >= 0;

    public static string Profile(string hive)
    {
        var m = ProfileRegex.Match(hive);
        return m.Success ? m.Groups[1].Value : "Undetermined";
    }

    public static string Drive(string hive)
    {
        var m = DriveFolderRegex.Match(hive);
        if (m.Success == false)
        {
            m = DriveRegex.Match(hive);
        }

        return m.Success ? m.Groups[1].Value : "Undetermined";
    }

    /// <summary>The folders and name of a full path, without the root's separators and the drive's colon (\\server\share\x is server\share\x)</summary>
    private static IEnumerable<string> RelativeParts(string fullPath) =>
        fullPath.Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(t => t.TrimEnd(':')).Where(t => t.Length > 0 && t != "?" && t != ".");

    private string Unique(string path)
    {
        var candidate = path;
        for (var n = 2; _used.Add(candidate) == false; n++)
        {
            var dir = Path.GetDirectoryName(path) ?? string.Empty;
            candidate = Path.Combine(dir, $"{Path.GetFileNameWithoutExtension(path)}_{n}{Path.GetExtension(path)}");
        }

        return candidate;
    }
}
