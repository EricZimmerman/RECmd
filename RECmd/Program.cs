using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Security;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Exceptionless;
using FluentValidation.Results;
using ICSharpCode.SharpZipLib.Zip;
using NLog;
using NLog.Config;
using NLog.Targets;
using RawCopy;
using Registry;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Other;
using RegistryPluginBase.Interfaces;
using ServiceStack;
using ServiceStack.Text;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using CsvWriter = CsvHelper.CsvWriter;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace RECmd;

internal class Program
{
    private const string VssDir = @"C:\___vssMount";
    private static Stopwatch _sw;
    private static Logger _logger;
    private static List<BatchCsvOut> _batchCsvOutList;
    private static readonly List<IRegistryPluginBase> Plugins = new();

    private static readonly string BaseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

    private static readonly string RunTimestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");

    private static string _pluginsDir = string.Empty;

    public static string dtf = "yyyy-MM-dd HH:mm:ss.fffffff";

    private static RootCommand _rootCommand;

    private static readonly string Header =
        $"RECmd version {Assembly.GetExecutingAssembly().GetName().Version}" +
        "\r\n\r\nAuthor: Eric Zimmerman (saericzimmerman@gmail.com)" +
        "\r\nhttps://github.com/EricZimmerman/RECmd\r\n\r\nNote: Enclose all strings containing spaces (and all RegEx) with double quotes";

    private static readonly string Footer = @"Example: RECmd.exe --f ""C:\Temp\UsrClass 1.dat"" --sk URL --recover false --nl" +
                                            "\r\n\t " +
                                            @"    RECmd.exe --f ""D:\temp\UsrClass 1.dat"" --StartDate ""11/13/2014 15:35:01"" " +
                                            "\r\n\t " +
                                            @"    RECmd.exe --f ""D:\temp\UsrClass 1.dat"" --RegEx --sv ""(App|Display)Name""";

    private static readonly HashSet<string> _seenHashes = new();

    private static void SetupNLog()
    {
        if (File.Exists("Nlog.config"))
        {
            return;
        }

        var config = new LoggingConfiguration();
        var loglevel = LogLevel.Info;

        var layout = @"${message}";

        var consoleTarget = new ColoredConsoleTarget();

        var whr = new ConsoleWordHighlightingRule("this will be replaced with search term", ConsoleOutputColor.Red,
            ConsoleOutputColor.Green);
        consoleTarget.WordHighlightingRules.Add(whr);

        config.AddTarget("console", consoleTarget);

        consoleTarget.Layout = layout;

        var rule1 = new LoggingRule("*", loglevel, consoleTarget);
        config.LoggingRules.Add(rule1);

        LogManager.Configuration = config;
    }

    private static void LoadPlugins()
    {
        var dlls = Directory.GetFiles(_pluginsDir, "RegistryPlugin.*.dll", SearchOption.AllDirectories);

        var loadedGuiDs = new HashSet<string>();

        foreach (var dll in dlls)
        {
            try
            {
                foreach (var exportedType in Assembly.LoadFrom(dll).GetExportedTypes())
                {
                    if (exportedType.GetInterface("RegistryPluginBase.Interfaces.IRegistryPluginBase") == null)
                    {
                        continue;
                    }

                    _logger.Debug($"Loading plugin '{dll}'");

                    var plugin = (IRegistryPluginBase)Activator.CreateInstance(exportedType);

                    if (loadedGuiDs.Contains(plugin.InternalGuid))
                    {
                        //its already loaded, so warn
                        _logger.Warn(
                            "Plugin '{plugin.PluginName}' has already been loaded. Internal GUID: {plugin.InternalGuid}");
                    }
                    else
                    {
                        loadedGuiDs.Add(plugin.InternalGuid);
                        //this is a good plugin

                        Plugins.Add(plugin);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error loading plugin: {dll}");
            }
        }
    }

    private static async Task Main(string[] args)
    {
        ExceptionlessClient.Default.Startup("fTcEOUkt1CxljTyOZfsr8AcSGQwWE4aYaYqk7cE1");
        SetupNLog();

        _pluginsDir = Path.Combine(BaseDirectory, "Plugins");

        if (Directory.Exists(_pluginsDir) == false)
        {
            Directory.CreateDirectory(_pluginsDir);
        }

        _logger = LogManager.GetCurrentClassLogger();

        _rootCommand = new RootCommand
        {
            new Option<string>(
                "-d",
                "Directory to look for hives (recursively). -f or -d is required"),

            new Option<string>(
                "-f",
                "Hive to search. -f or -d is required"),

            new Option<bool>(
                "-q",
                () => false,
                "Quiet mode. When true, hide processing details"),

            new Option<string>(
                "--kn",
                "Display details for key name. Includes subkeys and values"),

            new Option<string>(
                "--vn",
                "alue name. Only this value will be dumped"),

            new Option<string>(
                "--bn",
                "Use settings from supplied file to find keys/values. See included sample file for examples"),

            new Option<string>(
                "--csv",
                "Directory to save CSV formatted results to. Required when -bn is used"),
            
            new Option<string>(
                "--csvf",
                "File name to save CSV formatted results to. When present, overrides default name"),

            new Option<string>(
                "--saveTo",
                "Saves --vn value data in binary form to file. Expects path to a FILE"),

            new Option<string>(
                "--json",
                "Export --kn to directory specified by --json. Ignored when --vn is specified"),

            new Option<string>(
                "--jsonf",
                "When true, compress names for profile based hives."),
            
            new Option<bool>(
                "--details",
                () => false,
                "Show more details when displaying results"),

            new Option<int>(
                "--base64",
                "Find Base64 encoded values with size >= Base64 (specified in bytes)"),
            new Option<int>(
                "--minSize",
                "Find values with data size >= MinSize (specified in bytes)"),

            new Option<string>(
                "--sa",
                "Search for <string> in keys, values, data, and slack"),
            new Option<string>(
                "--sk",
                "Search for <string> in value record's key names"),
            new Option<string>(
                "--sv",
                "Search for <string> in value record's value names"),
            new Option<string>(
                "--sd",
                "Search for <string> in value record's value data"),
            new Option<string>(
                "--ss",
                "Search for <string> in value record's value slack"),
            
            new Option<bool>(
                "--literal",
                () => false,
                "If true, --sd and --ss search value will not be interpreted as ASCII or Unicode byte strings"),

            new Option<bool>(
                "--nd",
                () => false,
                "If true, do not show data when using --sd or --ss"),

            new Option<bool>(
                "--regex",
                () => false,
                "If present, treat <string> in --sk, --sv, --sd, and --ss as a regular expression"),

            new Option<string>(
                "--dt",
                getDefaultValue:()=>"yyyy-MM-dd HH:mm:ss.fffffff",
                "The custom date/time format to use when displaying time stamps"),
            
            new Option<bool>(
                "--nl",
                () => false,
                "When true, ignore transaction log files for dirty hives"),

            new Option<bool>(
                "--recover",
                () => false,
                "If true, recover deleted keys/values"),

            new Option<bool>(
                "--vss",
                () => false,
                "Process all Volume Shadow Copies that exist on drive specified by -f or -d"),

            new Option<bool>(
                "--dedupe",
                () => false,
                "Deduplicate -f or -d & VSCs based on SHA-1. First file found wins"),

            new Option<bool>(
                "--sync",
                () => false,
                "If true, the latest batch files from https://github.com/EricZimmerman/RECmd/tree/master/BatchExamples are downloaded and local files updated"),

            new Option<bool>(
                "--debug",
                () => false,
                "Show debug information during processing"),

            new Option<bool>(
                "--trace",
                () => false,
                "Show trace information during processing")
        };

        _rootCommand.Description = Header + "\r\n\r\n" + Footer;

        _rootCommand.Handler = CommandHandler.Create(DoWork);

        await _rootCommand.InvokeAsync(args);
    }

    private static void DoWork(string d, string f, string q, string kn, string vn, string bn, string csv, string csvf, string saveTo, string json, string jsonf, bool details, int base64, int minSize, string sa, string sk, string sv, string sd, string ss, bool literal, bool nd, bool regex, string dt, bool nl, bool recover, bool vss, bool dedupe, bool sync, bool debug, bool trace)
    {
        if (debug)
        {
            foreach (var r in LogManager.Configuration.LoggingRules)
            {
                r.EnableLoggingForLevel(LogLevel.Debug);
            }

            LogManager.ReconfigExistingLoggers();
            _logger.Debug("Enabled debug messages...");
        }

        if (trace)
        {
            foreach (var r in LogManager.Configuration.LoggingRules)
            {
                r.EnableLoggingForLevel(LogLevel.Trace);
            }

            LogManager.ReconfigExistingLoggers();
            _logger.Trace("Enabled trace messages...");
        }

        if (vss & (Helper.IsAdministrator() == false))
        {
            _logger.Error("--vss is present, but administrator rights not found. Exiting\r\n");
            return;
        }

        if (vss & !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.Error("--vss not supported on non-Windows platforms. Disabling");
            vss = false;
        }


        if (sync)
        {
            try
            {
                _logger.Info(Header);
                UpdateFromRepo();
            }
            catch (Exception e)
            {
                _logger.Error(e, $"There was an error checking for updates: {e.Message}");
            }

            Environment.Exit(0);
        }

        var hivesToProcess = new List<string>();

        ReBatch reBatch = null;

        _logger.Info(Header);
        _logger.Info("");
        _logger.Info($"Command line: {string.Join(" ", Environment.GetCommandLineArgs().Skip(1))}\r\n");

        if (vss)
        {
            string driveLetter;
            if (f.IsEmpty() == false)
            {
                driveLetter = Path.GetPathRoot(Path.GetFullPath(f))
                    .Substring(0, 1);
            }
            else
            {
                driveLetter = Path.GetPathRoot(Path.GetFullPath(d))
                    .Substring(0, 1);
            }

            Helper.MountVss(driveLetter, VssDir);
            Console.WriteLine();
        }

        if (bn?.Length > 0) //batch mode
        {
            if (File.Exists(bn) == false)
            {
                _logger.Error($"Batch file '{bn}' does not exist.");
                return;
            }

            if (csv.IsNullOrEmpty())
            {
                _logger.Error("--csv is required when using --bn. Exiting.");
                return;
            }

            reBatch = ValidateBatchFile(bn);
        }


        if (f?.Length > 0)
        {
            if (File.Exists(f) == false)
            {
                _logger.Error($"File '{f}' does not exist.");
                return;
            }

            if (CheckMinSwitches(sk, sv, sd, ss, sa, kn, minSize, base64, bn) == false)
            {
                _logger.Error(
                    "\r\nOne of the following switches is required: --sa | --sk | --sv | --sd | --ss | --kn | --Base64 | --MinSize | --bn\r\n\r\n");
                _logger.Info("Verify the command line and try again");
                return;
            }

            hivesToProcess.Add(f);

            if (vss)
            {
                var vssDirs = Directory.GetDirectories(VssDir);

                var root = Path.GetPathRoot(Path.GetFullPath(f));
                var stem = Path.GetFullPath(f).Replace(root, "");

                foreach (var vssDir in vssDirs)
                {
                    var newPath = Path.Combine(vssDir, stem);
                    if (File.Exists(newPath))
                    {
                        hivesToProcess.Add(newPath);
                    }
                }
            }
        }
        else if (d?.Length > 0)
        {
            if (Directory.Exists(d) == false)
            {
                _logger.Error($"Directory '{d}' does not exist.");
                return;
            }

            if (CheckMinSwitches(sk, sv, sd, ss, sa, kn, minSize, base64, bn) == false)
            {
                _logger.Error(
                    "\r\nOne of the following switches is required: --sk | --sv | --sd | --ss | --kn | --Base64 | --MinSize | --bn\r\n\r\n");
                _logger.Info("Verify the command line and try again");
                return;
            }

            var okFileParts = new HashSet<string>();
            okFileParts.Add("USRCLASS");
            okFileParts.Add("NTUSER");
            okFileParts.Add("SYSTEM");
            okFileParts.Add("SAM");
            okFileParts.Add("SOFTWARE");
            okFileParts.Add("AMCACHE");
            okFileParts.Add("SYSCACHE");
            okFileParts.Add("SECURITY");
            okFileParts.Add("DRIVERS");
            okFileParts.Add("COMPONENTS");

            var enumerationFilters = new DirectoryEnumerationFilters();
            enumerationFilters.InclusionFilter = fsei =>
            {
                if (fsei.Extension.ToUpperInvariant() == ".LOG1" || fsei.Extension.ToUpperInvariant() == ".LOG2" ||
                    fsei.Extension.ToUpperInvariant() == ".DLL" ||
                    fsei.Extension.ToUpperInvariant() == ".LOG" ||
                    fsei.Extension.ToUpperInvariant() == ".CSV" ||
                    fsei.Extension.ToUpperInvariant() == ".BLF" ||
                    fsei.Extension.ToUpperInvariant() == ".REGTRANS-MS" ||
                    fsei.Extension.ToUpperInvariant() == ".EXE" ||
                    fsei.Extension.ToUpperInvariant() == ".TXT" || fsei.Extension.ToUpperInvariant() == ".INI")
                {
                    return false;
                }


                var foundOkFilePart = false;

                foreach (var okFilePart in okFileParts)
                {
                    if (fsei.FileName.ToUpperInvariant().Contains(okFilePart))
                    {
                        foundOkFilePart = true;
                        //     return true;
                    }
                }

                if (foundOkFilePart == false)
                {
                    return false;
                }

                var fi = new FileInfo(fsei.FullPath);

                if (fi.Length < 4)
                {
                    return false;
                }

                try
                {
                    using (var fs = new FileStream(fsei.FullPath, FileMode.Open, FileAccess.Read))
                    {
                        using (var br = new BinaryReader(fs, new ASCIIEncoding()))
                        {
                            try
                            {
                                var chunk = br.ReadBytes(4);

                                var sig = BitConverter.ToInt32(chunk, 0);

                                if (sig == 0x66676572)
                                {
                                    return true;
                                }
                            }
                            catch (Exception)
                            {
                                // ignored
                            }

                            return false;
                        }
                    }
                }
                catch (IOException)
                {
                    if (Helper.IsAdministrator() == false)
                    {
                        throw new UnauthorizedAccessException("Administrator privileges not found!");
                    }

                    var files = new List<string>();
                    files.Add(fsei.FullPath);

                    var rawf = Helper.GetRawFiles(files);

                    if (rawf.First().FileStream.Length == 0)
                    {
                        return false;
                    }

                    try
                    {
                        var b = new byte[4];
                        rawf.First().FileStream.ReadExactly(b, 4);

                        var sig = BitConverter.ToInt32(b, 0);

                        if (sig == 0x66676572)
                        {
                            return true;
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }

                    return false;
                }
            };

            enumerationFilters.RecursionFilter = entryInfo => !entryInfo.IsMountPoint && !entryInfo.IsSymbolicLink;

            enumerationFilters.ErrorFilter = (errorCode, errorMessage, pathProcessed) => true;

            var dirEnumOptions =
                DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive |
                DirectoryEnumerationOptions.SkipReparsePoints | DirectoryEnumerationOptions.ContinueOnException |
                DirectoryEnumerationOptions.BasicSearch;

            _logger.Fatal($"Searching '{d}' for hives...");

            var files2 =
                Directory.EnumerateFileSystemEntries(d, dirEnumOptions, enumerationFilters);

            var count = 0;

            try
            {
                hivesToProcess.AddRange(files2);
                count = hivesToProcess.Count;

                _logger.Info($"\tHives found: {count:N0}");
            }
            catch (Exception)
            {
                _logger.Fatal($"Could not access all files in '{d}'");
                _logger.Error("");
                _logger.Fatal("Rerun the program with Administrator privileges to try again\r\n");
                //Environment.Exit(-1);
            }

            if (vss)
            {
                var vssDirs = Directory.GetDirectories(VssDir);


                foreach (var vssDir in vssDirs)
                {
                    var root = Path.GetPathRoot(Path.GetFullPath(d));
                    var stem = Path.GetFullPath(d).Replace(root, "");

                    var target = Path.Combine(vssDir, stem);

                    _logger.Fatal($"Searching 'VSS{target.Replace($"{VssDir}\\", "")}' for hives...");

                    files2 =
                        Directory.EnumerateFileSystemEntries(target, dirEnumOptions, enumerationFilters);

                    try
                    {
                        hivesToProcess.AddRange(files2);

                        count = hivesToProcess.Count - count;

                        _logger.Info($"\tHives found: {count:N0}");
                    }
                    catch (Exception)
                    {
                        _logger.Fatal($"Could not access all files in '{d}'");
                        _logger.Error("");
                        _logger.Fatal("Rerun the program with Administrator privileges to try again\r\n");
                        //Environment.Exit(-1);
                    }
                }
            }

            _logger.Fatal($"\r\nTotal hives found: {hivesToProcess.Count:N0}");
        }
        else
        {
            var helpBld = new HelpBuilder(LocalizationResources.Instance, Console.WindowWidth);
            var hc = new HelpContext(helpBld, _rootCommand, Console.Out);

            helpBld.Write(hc);

            return;
        }


        if (hivesToProcess.Count == 0)
        {
            _logger.Warn("No hives were found. Exiting...");

            return;
        }

        var totalHits = 0;
        var hivesWithHits = 0;
        double totalSeconds = 0;
        var searchUsed = false;

        _batchCsvOutList = new List<BatchCsvOut>();

        LoadPlugins();

        var hiveInfoWithHits = new List<string>();

        foreach (var hiveToProcess in hivesToProcess)
        {
            _logger.Info("");


            if (hiveToProcess.StartsWith(VssDir))
            {
                _logger.Warn($"Processing 'VSS{hiveToProcess.Replace($"{VssDir}\\", "")}'");
            }
            else
            {
                _logger.Info($"Processing hive '{hiveToProcess}'");
            }

            //_logger.Info("");

            if (File.Exists(hiveToProcess) == false)
            {
                _logger.Warn($"'{hiveToProcess}' does not exist. Skipping");
                continue;
            }

            try
            {
                RegistryHive reg;

                var dirname = Path.GetDirectoryName(hiveToProcess);
                var hiveBase = Path.GetFileName(hiveToProcess);

                List<RawCopyReturn> rawFiles = null;

                try
                {
                    _sw = new Stopwatch();
                    _sw.Start();

                    using (var fs = new FileStream(hiveToProcess, FileMode.Open, FileAccess.Read))
                    {
                        if (dedupe)
                        {
                            var sha = Helper.GetSha1FromStream(fs, 0);
                            if (_seenHashes.Contains(sha))
                            {
                                _logger.Debug($"Skipping '{hiveToProcess}' as a file with SHA-1 '{sha}' has already been processed");
                                continue;
                            }

                            _seenHashes.Add(sha);
                        }

                        fs.Seek(0, SeekOrigin.Begin);

                        reg = new RegistryHive(fs.ReadFully(), hiveToProcess)
                        {
                            RecoverDeleted = true
                        };
                    }
                }
                catch (IOException ex)
                {
                    _logger.Debug($"IO exception! Error message: {ex.Message}");

                    //file is in use

                    if (Helper.IsAdministrator() == false)
                    {
                        throw new UnauthorizedAccessException("Administrator privileges not found!");
                    }

                    _logger.Warn($"'{hiveToProcess}' is in use. Rerouting...\r\n");

                    var files = new List<string>();
                    files.Add(hiveToProcess);

                    var logFiles = Directory.GetFiles(dirname, $"{hiveBase}.LOG?");

                    foreach (var logFile in logFiles)
                    {
                        files.Add(logFile);
                    }

                    rawFiles = Helper.GetRawFiles(files);

                    if (rawFiles.First().FileStream.Length == 0)
                    {
                        continue;
                    }

                    if (dedupe)
                    {
                        var sha = Helper.GetSha1FromStream(rawFiles.First().FileStream, 0);
                        if (_seenHashes.Contains(sha))
                        {
                            _logger.Debug($"Skipping '{hiveToProcess}' as a file with SHA-1 '{sha}' has already been processed");
                            continue;
                        }

                        _seenHashes.Add(sha);
                    }

                    var bb = rawFiles.First().FileStream.ReadFully();

                    reg = new RegistryHive(bb, rawFiles.First().InputFilename);
                }

                if (reg.Header.PrimarySequenceNumber != reg.Header.SecondarySequenceNumber)
                {
                    if (string.IsNullOrEmpty(dirname))
                    {
                        dirname = ".";
                    }

                    var logFiles = Directory.GetFiles(dirname, $"{hiveBase}.LOG?");


                    if (logFiles.Length == 0)
                    {
                        if (nl == false)
                        {
                            _logger.Warn(
                                "Registry hive is dirty and no transaction logs were found in the same directory! LOGs should have same base name as the hive. Aborting!!");
                            throw new Exception(
                                "Sequence numbers do not match and transaction logs were not found in the same directory as the hive. Aborting");
                        }

                        _logger.Warn(
                            "Registry hive is dirty and no transaction logs were found in the same directory. Data may be missing! Continuing anyways...");
                    }
                    else
                    {
                        if (nl == false)
                        {
                            if (rawFiles != null)
                            {
                                var lt = new List<TransactionLogFileInfo>();
                                foreach (var rawCopyReturn in rawFiles.Skip(1).ToList())
                                {
                                    var bb1 = rawCopyReturn.FileStream.ReadFully();

                                    var tt = new TransactionLogFileInfo(rawCopyReturn.InputFilename, bb1);
                                    lt.Add(tt);
                                }

                                reg.ProcessTransactionLogs(lt, true);
                            }
                            else
                            {
                                reg.ProcessTransactionLogs(logFiles.ToList(), true);
                            }
                        }
                        else
                        {
                            _logger.Warn(
                                "Registry hive is dirty and transaction logs were found in the same directory, but --nl was provided. Data may be missing! Continuing anyways...");
                        }
                    }
                }


                reg.ParseHive();


                // _logger.Info("");

                //hive is ready for searching/interaction

                if (sa?.Length > 0 ||
                    sk?.Length > 0 ||
                    sv?.Length > 0 ||
                    sd?.Length > 0 ||
                    ss?.Length > 0)
                {
                    searchUsed = true;

                    if (sa.IsNullOrEmpty() == false)
                    {
                        sk = sa;
                        sv = sa;
                        sd = sa;
                        ss = sa;
                    }

                    var hits = new List<SearchHit>();

                    if (sk.Length > 0)
                    {
                        var results = DoKeySearch(reg, sk,
                            regex);
                        if (results != null)
                        {
                            hits.AddRange(results);
                        }
                    }

                    if (sv.Length > 0)
                    {
                        var results = DoValueSearch(reg, sv,
                            regex);
                        if (results != null)
                        {
                            hits.AddRange(results);
                        }
                    }

                    if (sd.Length > 0)
                    {
                        var results = DoValueDataSearch(reg, sd,
                            regex, literal);
                        if (results != null)
                        {
                            hits.AddRange(results);
                        }
                    }

                    if (ss.Length > 0)
                    {
                        var results = DoValueSlackSearch(reg,
                            ss,
                            regex, literal);
                        if (results != null)
                        {
                            hits.AddRange(results);
                        }
                    }

                    if (hits.Count > 0)
                    {
                        var suffix2 = hits.Count == 1 ? "" : "s";

                        if (hiveToProcess.StartsWith(VssDir))
                        {
                            _logger.Fatal($"\tFound {hits.Count:N0} search hit{suffix2} in 'VSS{hiveToProcess.Replace($"{VssDir}\\", "")}'");
                            hiveInfoWithHits.Add($"\tFound {hits.Count:N0} search hit{suffix2} in 'VSS{hiveToProcess.Replace($"{VssDir}\\", "")}'");
                        }
                        else
                        {
                            _logger.Fatal($"\tFound {hits.Count:N0} search hit{suffix2} in '{hiveToProcess}'");
                            hiveInfoWithHits.Add($"\tFound {hits.Count:N0} search hit{suffix2} in '{hiveToProcess}'");
                        }

                        hivesWithHits += 1;
                        totalHits += hits.Count;
                    }
                    else
                    {
                        _logger.Info("\tNothing found");
                    }

                    var words = new HashSet<string>();
                    foreach (var searchHit in hits)
                    {
                        if (sa?.Length > 0)
                        {
                            words.Add(sa);
                        }

                        if (sk?.Length > 0)
                        {
                            words.Add(sk);
                        }

                        if (sv?.Length > 0)
                        {
                            words.Add(sv);
                        }

                        if (sd?.Length > 0)
                        {
                            if (regex)
                            {
                                words.Add(sd);
                            }
                            else
                            {
                                if (searchHit.Value?.VkRecord.DataType == VkCellRecord.DataTypeEnum.RegBinary)
                                {
                                    words.Add(searchHit.HitString);
                                }
                                else
                                {
                                    words.Add(sd);
                                }
                            }
                        }

                        if (ss.Length > 0)
                        {
                            if (regex)
                            {
                                words.Add(ss);
                            }
                            else
                            {
                                words.Add(searchHit.HitString);
                            }
                        }
                    }

                    AddHighlightingRules(words.ToList(), regex);

                    foreach (var searchHit in hits)
                    {
                        searchHit.StripRootKeyName = true;

                        string display;

                        var keyIsDeleted = (searchHit.Key.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) ==
                                           RegistryKey.KeyFlagsEnum.Deleted;

                        switch (searchHit.HitLocation)
                        {
                            case SearchHit.HitType.KeyName:
                                display =
                                    $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}'";

                                if (keyIsDeleted)
                                {
                                    _logger.Fatal(display);
                                }
                                else
                                {
                                    _logger.Info(display);
                                }

                                break;
                            case SearchHit.HitType.ValueName:
                                display =
                                    $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}'";

                                if (keyIsDeleted)
                                {
                                    _logger.Fatal(display);
                                }
                                else
                                {
                                    _logger.Info(display);
                                }

                                break;
                            case SearchHit.HitType.ValueData:
                                if (nd)
                                {
                                    display =
                                        $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}'";
                                    if (keyIsDeleted)
                                    {
                                        _logger.Fatal(display);
                                    }
                                    else
                                    {
                                        _logger.Info(display);
                                    }
                                }
                                else
                                {
                                    if (searchHit.Value != null && sd?.Length > 0)
                                    {
                                        display =
                                            $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}', Data: '{searchHit.Value.ValueData}'";
                                        if (keyIsDeleted)
                                        {
                                            _logger.Fatal(display);
                                        }
                                        else
                                        {
                                            _logger.Info(display);
                                        }
                                    }
                                }

                                break;
                            case SearchHit.HitType.ValueSlack:
                                if (nd)
                                {
                                    display =
                                        $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}'";
                                    if (keyIsDeleted)
                                    {
                                        _logger.Fatal(display);
                                    }
                                    else
                                    {
                                        _logger.Info(display);
                                    }
                                }
                                else
                                {
                                    if (searchHit.Value != null &&
                                        ss?.Length > 0)
                                    {
                                        display =
                                            $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}', Slack: '{searchHit.Value.ValueSlack}'";

                                        if (keyIsDeleted)
                                        {
                                            _logger.Fatal(display);
                                        }
                                        else
                                        {
                                            _logger.Info(display);
                                        }
                                    }
                                }

                                break;
                        }

//                                if (_fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0 ||
//                                    _fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
//                                {
//                                    if (_fluentCommandLineParser.Object.SuppressData)
//                                    {
//                                        var display =
//                                            $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}'";
//                                        if (keyIsDeleted)
//                                        {
//                                            _logger.Fatal(display);
//                                        }
//                                        else
//                                        {
//                                            _logger.Info(display);
//                                        }
//                                    }
//                                    else
//                                    {
//                                        if (searchHit.Value != null && _fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
//                                        {
//                                            var display =
//                                                $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}', Slack: '{searchHit.Value.ValueSlack}'";
//
//                                            if (keyIsDeleted)
//                                            {
//                                                _logger.Fatal(display);
//                                            }
//                                            else
//                                            {
//                                                _logger.Info(display);
//                                            }
//                                        }
//
//                                        if (searchHit.Value != null &&  _fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0)
//                                        {
//                                            var display =
//                                                $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}', Data: '{searchHit.Value.ValueData}'";
//                                            if (keyIsDeleted)
//                                            {
//                                                _logger.Fatal(display);
//                                            }
//                                            else
//                                            {
//                                                _logger.Info(display);
//                                            }
//                                        }
//                                    }
//                                }
//                                
//                                if (_fluentCommandLineParser.Object.SimpleSearchKey.Length > 0)
//                                {
//                                    var display =
//                                        $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}'";
//
//                                    if (keyIsDeleted)
//                                    {
//                                        _logger.Fatal(display);
//                                    }
//                                    else
//                                    {
//                                        _logger.Info(display);
//                                    }
//                                } 
//                                if (searchHit.Value != null && _fluentCommandLineParser.Object.SimpleSearchValue.Length > 0)
//                                {
//                                    var display =
//                                        $"\tKey: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}'";
//
//                                    if (keyIsDeleted)
//                                    {
//                                        _logger.Fatal(display);
//                                    }
//                                    else
//                                    {
//                                        _logger.Info(display);
//                                    }
//                                }
                    }

                    var target = (ColoredConsoleTarget)LogManager.Configuration.FindTargetByName("console");
                    target.WordHighlightingRules.Clear();

//                    //TODO search deleted?? should only need to look in reg.UnassociatedRegistryValues
                } //End s* options

                if (kn.IsNullOrEmpty() == false)
                {
                    //dumping key and/or values
                    searchUsed = true;

                    var key = reg.GetKey(kn);

                    if (key == null && kn.ToUpperInvariant() == "ROOT")
                    {
                        _logger.Info($"\tUsing 'ROOT' alias. Actual ROOT key name: '{reg.Root.KeyName}'");
                        key = reg.Root;
                    }

                    KeyValue val = null;

                    if (key == null)
                    {
                        _logger.Warn($"Key '{kn}' not found.");

                        continue;
                    }

                    if (vn?.Length > 0)
                    {
                        val = key.Values.SingleOrDefault(c =>
                            c.ValueName == vn);

                        if (val == null)
                        {
                            _logger.Warn(
                                $"Value '{vn}' not found for key '{kn}'.");

                            continue;
                        }

                        if (saveTo?.Length > 0)
                        {
                            var baseDir = Path.GetDirectoryName(saveTo);
                            if (Directory.Exists(baseDir) == false)
                            {
                                Directory.CreateDirectory(baseDir);
                            }

                            _logger.Warn(
                                $"Saving contents of '{val.ValueName}' to '{saveTo}\r\n'");
                            try
                            {
                                File.WriteAllBytes(saveTo, val.ValueDataRaw);
                            }
                            catch (Exception ex)
                            {
                                _logger.Error(
                                    $"Save failed to '{saveTo}'. Error: {ex.Message}");
                            }
                        }
                    }

                    var keyIsDeleted = (key.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) ==
                                       RegistryKey.KeyFlagsEnum.Deleted;

                    //dump key here
                    if (vn.IsNullOrEmpty())
                    {
                        if (json.IsNullOrEmpty() == false)
                        {
                            //export to json
                            if (Directory.Exists(json) == false)
                            {
                                Directory.CreateDirectory(json);
                            }

                            var jso = BuildJson(key);

                            try
                            {
                                var outFile = Path.Combine(json,
                                    $"{StripInvalidCharsFromFileName(key.KeyName, "_")}.json");

                                if (jsonf.IsNullOrEmpty() == false)
                                {
                                    outFile = Path.Combine(json, Path.GetFileName(jsonf));
                                }

                                _logger.Warn($"Saving key to json file '{outFile}'\r\n");
                                File.WriteAllText(outFile, jso.ToJson());
                            }
                            catch (Exception e)
                            {
                                _logger.Error(
                                    $"Error saving key '{key.KeyPath}' to directory '{json}': {e.Message}");
                            }
                        }

                        if (details)
                        {
                            _logger.Info(key);
                        }
                        else
                        {
                            //key info only
                            _logger.Warn($"Key path: '{Helpers.StripRootKeyNameFromKeyPath(key.KeyPath)}'");
                            _logger.Info($"Last write time: {key.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}");
                            if (keyIsDeleted)
                            {
                                _logger.Fatal("Deleted: TRUE");
                            }

                            _logger.Info("");

                            _logger.Info($"Subkey count: {key.SubKeys.Count:N0}");
                            _logger.Info($"Values count: {key.Values.Count:N0}");
                            _logger.Info("");

                            var i = 0;

                            foreach (var registryKey in key.SubKeys)
                            {
                                var skeyIsDeleted = (registryKey.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) ==
                                                    RegistryKey.KeyFlagsEnum.Deleted;
                                if (skeyIsDeleted)
                                {
                                    _logger.Fatal($"------------ Subkey #{i:N0} (DELETED) ------------");
                                    _logger.Fatal(
                                        $"Name: {registryKey.KeyName} (Last write: {registryKey.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}) Value count: {registryKey.Values.Count:N0}");
                                }
                                else
                                {
                                    _logger.Info($"------------ Subkey #{i:N0} ------------");
                                    _logger.Info(
                                        $"Name: {registryKey.KeyName} (Last write: {registryKey.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}) Value count: {registryKey.Values.Count:N0}");
                                }

                                i += 1;
                            }

                            i = 0;
                            _logger.Info("");

                            foreach (var keyValue in key.Values)
                            {
                                if (keyIsDeleted)
                                {
                                    _logger.Fatal($"------------ Value #{i:N0} (DELETED) ------------");
                                    _logger.Fatal($"Name: {keyValue.ValueName} ({keyValue.ValueType})");

                                    var slack = "";

                                    if (keyValue.ValueSlack.Length > 0)
                                    {
                                        slack = $"(Slack: {keyValue.ValueSlack})";
                                    }

                                    _logger.Fatal($"Data: {keyValue.ValueData} {slack}");
                                }
                                else
                                {
                                    _logger.Info($"------------ Value #{i:N0} ------------");
                                    _logger.Info($"Name: {keyValue.ValueName} ({keyValue.ValueType})");

                                    var slack = "";

                                    if (keyValue.ValueSlack.Length > 0)
                                    {
                                        slack = $"(Slack: {keyValue.ValueSlack})";
                                    }

                                    _logger.Info($"Data: {keyValue.ValueData} {slack}");
                                }

                                i += 1;
                            }
                        }
                    }
                    else
                    {
                        //value only

                        if (keyIsDeleted)
                        {
                            _logger.Warn($"Key path: '{Helpers.StripRootKeyNameFromKeyPath(key.KeyPath)}'");
                            _logger.Info($"Last write time: {key.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}");

                            _logger.Fatal("Deleted: TRUE");

                            _logger.Info("");

                            _logger.Fatal($"Value name: '{val.ValueName}' ({val.ValueType})");
                            var slack = "";
                            if (val.ValueSlack.Length > 0)
                            {
                                slack = $"(Slack: {val.ValueSlack})";
                            }

                            _logger.Fatal($"Value data: {val.ValueData} {slack}");
                        }
                        else
                        {
                            _logger.Warn($"Key path: '{Helpers.StripRootKeyNameFromKeyPath(key.KeyPath)}'");
                            _logger.Info($"Last write time: {key.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}");

                            _logger.Info("");

                            _logger.Info($"Value name: '{val.ValueName}' ({val.ValueType})");
                            var slack = "";
                            if (val.ValueSlack.Length > 0)
                            {
                                slack = $"(Slack: {val.ValueSlack})";
                            }

                            _logger.Info($"Value data: {val.ValueData} {slack}");
                        }
                    }

                    _logger.Info("");
                } //end kn options
                else if (minSize > 0)
                {
                    searchUsed = true;
                    var hits = reg.FindByValueSize(minSize).ToList();

                    if (hits.Count > 0)
                    {
                        var suffix2 = hits.Count == 1 ? "" : "s";

                        if (hiveToProcess.StartsWith(VssDir))
                        {
                            _logger.Warn(
                                $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {minSize:N0} bytes in 'VSS{hiveToProcess.Replace($"{VssDir}\\", "")}'");
                        }
                        else
                        {
                            _logger.Warn(
                                $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {minSize:N0} bytes in '{hiveToProcess}'");
                        }

                        hivesWithHits += 1;
                        totalHits += hits.Count;
                    }
                    else
                    {
                        _logger.Info("Nothing found");
                    }

                    foreach (var valueBySizeInfo in hits)
                    {
                        searchUsed = true;

                        var keyIsDeleted = (valueBySizeInfo.Key.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) ==
                                           RegistryKey.KeyFlagsEnum.Deleted;

                        var display =
                            $"Key: {Helpers.StripRootKeyNameFromKeyPath(valueBySizeInfo.Key.KeyPath)}, Value: {valueBySizeInfo.Value.ValueName}, Size: {valueBySizeInfo.Value.ValueDataRaw.Length:N0}";

                        if (keyIsDeleted)
                        {
                            _logger.Fatal(display);
                        }
                        else
                        {
                            _logger.Info(display);
                        }
                    }

                    _logger.Info("");
                } //end min size option
                else if (base64 > 0)
                {
                    searchUsed = true;
                    var hits = reg.FindBase64(base64).ToList();

                    if (hits.Count > 0)
                    {
                        var suffix2 = hits.Count == 1 ? "" : "s";

                        if (hiveToProcess.StartsWith(VssDir))
                        {
                            _logger.Warn(
                                $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {base64:N0} bytes in 'VSS{hiveToProcess.Replace($"{VssDir}\\", "")}'");
                        }
                        else
                        {
                            _logger.Warn(
                                $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {base64:N0} bytes in '{hiveToProcess}'");
                        }

                        hivesWithHits += 1;
                        totalHits += hits.Count;
                    }
                    else
                    {
                        _logger.Info("Nothing found");
                    }

                    foreach (var base64Hit in hits)
                    {
                        var keyIsDeleted = (base64Hit.Key.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) ==
                                           RegistryKey.KeyFlagsEnum.Deleted;

                        var display =
                            $"Key: {Helpers.StripRootKeyNameFromKeyPath(base64Hit.Key.KeyPath)}, Value: {base64Hit.Value.ValueName}, Size: {base64Hit.Value.ValueDataRaw.Length:N0}";

                        if (keyIsDeleted)
                        {
                            _logger.Fatal(display);
                        }
                        else
                        {
                            _logger.Info(display);
                        }
                    }

                    _logger.Info("");
                } //end min size option
                else if (bn?.Length > 0) //batch mode
                {
                    ProcessBatch(reBatch, reg, d, f, csv, csvf, dt);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Sequence numbers do not match and transaction") == false)
                {
                    if (ex.Message.Contains("Administrator privileges not found"))
                    {
                        _logger.Fatal($"Could not access '{hiveToProcess}' because it is in use");
                        _logger.Error("");
                        _logger.Fatal("Rerun the program with Administrator privileges to try again\r\n");
                    }
                    else
                    {
                        _logger.Error($"There was an error: {ex.Message}");
                    }
                }
            }

            _sw.Stop();
            totalSeconds += _sw.Elapsed.TotalSeconds;
        }

        if (bn.IsNullOrEmpty() == false)
        {
            _logger.Info("");

            var suffix2 = _batchCsvOutList.Count == 1 ? "" : "s";
            var suffix4 = hivesToProcess.Count == 1 ? "" : "s";

            _logger.Info(
                $"Found {_batchCsvOutList.Count:N0} key/value pair{suffix2} across {hivesToProcess.Count:N0} file{suffix4}");
            _logger.Info($"Total search time: {totalSeconds:N3} seconds");
            if (_batchCsvOutList.Count > 0)
            {
                if (Directory.Exists(csv) == false)
                {
                    _logger.Warn(
                        $"Path to '{csv}' doesn't exist. Creating...");
                    Directory.CreateDirectory(csv);
                }

                var outName =
                    $"{RunTimestamp}_RECmd_Batch_{Path.GetFileNameWithoutExtension(bn)}_Output.csv";

                if (csvf.IsNullOrEmpty() == false)
                {
                    outName = Path.GetFileName(csvf);
                }

                var outFile = Path.Combine(csv, outName);

                _logger.Info($"\r\nSaving batch mode CSV file to '{outFile}'");

                var swCsv = new StreamWriter(outFile, false, Encoding.UTF8);
                var csvWriter = new CsvWriter(swCsv, CultureInfo.InvariantCulture);

                var foo = csvWriter.Context.AutoMap<BatchCsvOut>();

                foo.Map(t => t.LastWriteTimestamp).Convert(t =>
                    $"{t.Value.LastWriteTimestamp?.ToString(dt)}");

                csvWriter.Context.RegisterClassMap(foo);

                csvWriter.WriteHeader<BatchCsvOut>();
                csvWriter.NextRecord();

                csvWriter.WriteRecords(_batchCsvOutList);

                swCsv.Flush();
                swCsv.Close();
            }
        }


        if (searchUsed && d?.Length > 0)
        {
            _logger.Info("");

            var suffix2 = totalHits == 1 ? "" : "s";
            var suffix3 = hivesWithHits == 1 ? "" : "s";
            var suffix4 = hivesToProcess.Count == 1 ? "" : "s";
            var suffix5 = vss ? " (and VSCs)" : "";

            _logger.Info("---------------------------------------------");
            _logger.Info($"Directory: {d}{suffix5}");
            _logger.Info("");
            _logger.Info(
                $"Found {totalHits:N0} hit{suffix2} in {hivesWithHits:N0} hive{suffix3} out of {hivesToProcess.Count:N0} file{suffix4}:");

            foreach (var hiveInfoWithHit in hiveInfoWithHits)
            {
                _logger.Fatal(hiveInfoWithHit);
            }

            _logger.Info("");

            _logger.Info($"Total search time: {totalSeconds:N3} seconds");
        }

        _logger.Info("");

        if (vss)
        {
            if (Directory.Exists(VssDir))
            {
                foreach (var directory in Directory.GetDirectories(VssDir))
                {
                    Directory.Delete(directory);
                }

                Directory.Delete(VssDir, true, true);
            }
        }
    }

    private static void UpdateFromRepo()
    {
        Console.WriteLine();

        _logger.Info(
            "Checking for updated batch files at https://github.com/EricZimmerman/RECmd/tree/master/BatchExamples...");
        Console.WriteLine();
        var archivePath = Path.Combine(BaseDirectory, "____master.zip");

        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

#pragma warning disable CS0618
        using (var client = new WebClient())
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            client.DownloadFile("https://github.com/EricZimmerman/RECmd/archive/master.zip", archivePath);
        }
#pragma warning restore CS0618

        var fff = new FastZip();

        if (Directory.Exists(Path.Combine(BaseDirectory, "BatchExamples")) == false)
        {
            Directory.CreateDirectory(Path.Combine(BaseDirectory, "BatchExamples"));
        }

        var directoryFilter = "BatchExamples";

        // Will prompt to overwrite if target file names already exist
        fff.ExtractZip(archivePath, BaseDirectory, FastZip.Overwrite.Always, null,
            null, directoryFilter, true);

        if (File.Exists(archivePath))
        {
            File.Delete(archivePath);
        }

        var newMapPath = Path.Combine(BaseDirectory, "RECmd-master", "BatchExamples");

        var orgMapPath = Path.Combine(BaseDirectory, "BatchExamples");

        var newMaps = Directory.GetFiles(newMapPath);

        var newlocalMaps = new List<string>();

        var updatedlocalMaps = new List<string>();


        foreach (var newMap in newMaps)
        {
            var mName = Path.GetFileName(newMap);
            var dest = Path.Combine(orgMapPath, mName);

            if (File.Exists(dest) == false)
            {
                //new target
                newlocalMaps.Add(mName);
            }
            else
            {
                //current destination file exists, so compare to new
                var fiNew = new FileInfo(newMap);
                var fi = new FileInfo(dest);

                if (fiNew.GetHash(HashType.SHA1) != fi.GetHash(HashType.SHA1))
                {
                    //updated file
                    updatedlocalMaps.Add(mName);
                }
            }

            File.Copy(newMap, dest, CopyOptions.None);
        }


        if (newlocalMaps.Count > 0 || updatedlocalMaps.Count > 0)
        {
            _logger.Fatal("Updates found!");
            Console.WriteLine();

            if (newlocalMaps.Count > 0)
            {
                _logger.Error("New batch files");
                foreach (var newLocalMap in newlocalMaps)
                {
                    _logger.Info(Path.GetFileNameWithoutExtension(newLocalMap));
                }

                Console.WriteLine();
            }

            if (updatedlocalMaps.Count > 0)
            {
                _logger.Error("Updated batch files");
                foreach (var um in updatedlocalMaps)
                {
                    _logger.Info(Path.GetFileNameWithoutExtension(um));
                }

                Console.WriteLine();
            }

            Console.WriteLine();
        }
        else
        {
            Console.WriteLine();
            _logger.Info("No new batch files available");
            Console.WriteLine();
        }

        Directory.Delete(Path.Combine(BaseDirectory, "RECmd-master"), true);
    }

    private static void ProcessBatchKey(RegistryKey key, Key batchKey, string hivePath, string d, string f, string csv, string csvf, string dt)
    {
        var regKey = key;

        KeyValue regVal = null;

        //1. dump all key values (value name is null)
        //2. dump only value in a key (value name is specified)

        if (batchKey.ValueName.IsNullOrEmpty() == false)
        {
            //we need to check for a value
            regVal = regKey.Values.SingleOrDefault(t =>
                string.Equals(t.ValueName, batchKey.ValueName, StringComparison.InvariantCultureIgnoreCase));

            if (regVal != null)
            {
                _logger.Trace($"Found value '{batchKey.ValueName}' in key {regKey.KeyPath}!");
                BatchDumpKey(key, batchKey, hivePath, d, f, csv, csvf, dt);
            }
        }

        if (regVal == null && batchKey.ValueName.IsNullOrEmpty())
        {
            //do not need to find a value, 
            _logger.Trace($"Found key '{key.KeyPath}'!");
            BatchDumpKey(key, batchKey, hivePath, d, f, csv, csvf, dt);
        }


        if (!batchKey.Recursive)
        {
            return;
        }

        foreach (var regKeySubKey in regKey.SubKeys)
        {
            ProcessBatchKey(regKeySubKey, batchKey, hivePath, d, f, csv, csvf, dt);
        }
    }

    private static void ProcessBatch(ReBatch reBatch, RegistryHive regHive, string d, string f, string csv, string csvf, string dt)
    {
        foreach (var key in reBatch.Keys)
        {
            if ((int)regHive.HiveType != (int)key.HiveType)
            {
                _logger.Debug(
                    $"Skipping key '{key.KeyPath}' because the current hive ({regHive.HiveType}) is not of the right type ({key.HiveType})");
                continue;
            }

            _logger.Debug($"Processing '{key.KeyPath}' (HiveType match)");
            _logger.Trace(key.Dump);

            if (key.KeyPath.Equals("*"))
            {
                var regKey = regHive.Root;

                if (regKey == null)
                {
                    _logger.Debug($"Key '{key.KeyPath}' not found in '{regHive.HivePath}'");
                    continue;
                }

                ProcessBatchKey(regKey, key, regHive.HivePath, d, f, csv, csvf, dt);
            }
            else if (key.KeyPath.Contains("*"))
            {
                var keysToProcess = regHive.ExpandKeyPath(key.KeyPath);
                _logger.Trace($"Expanded '{key.KeyPath}' to '{string.Join(" | ", keysToProcess)}'");
                foreach (var keyToProcess in keysToProcess)
                {
                    var regKey = regHive.GetKey(keyToProcess);

                    KeyValue regVal = null;

                    if (regKey == null)
                    {
                        _logger.Warn($"Key '{keyToProcess}' not found in '{regHive.HivePath}'");
                        continue;
                    }

                    if (key.ValueName.IsNullOrEmpty() == false)
                    {
                        //we need to check for a value
                        regVal = regKey.Values.SingleOrDefault(t =>
                            t.ValueName.ToUpperInvariant() == key.ValueName.ToUpperInvariant());

                        if (regVal == null)
                        {
                            _logger.Debug(
                                $"Value '{key.ValueName}' not found in key '{Helpers.StripRootKeyNameFromKeyPath(regKey.KeyPath)}'");
                            continue;
                        }
                    }

                    if (regVal != null)
                    {
                        _logger.Info(
                            $"Found key '{Helpers.StripRootKeyNameFromKeyPath(regKey.KeyPath)}' and value '{key.ValueName}'!");
                    }
                    else
                    {
                        _logger.Info($"Found key '{Helpers.StripRootKeyNameFromKeyPath(regKey.KeyPath)}'!");
                    }

                    //TODO test this with all conditions
                    //BatchDumpKey(regKey, key, regHive.HivePath);

                    //switch to this for better recursive support vs BatchDumpKey
                    ProcessBatchKey(regKey, key, regHive.HivePath, d, f, csv, csvf, dt);
                }
            }
            else
            {
                var regKey = regHive.GetKey(key.KeyPath);

                if (regKey == null)
                {
                    _logger.Debug($"Key '{key.KeyPath}' not found in '{regHive.HivePath}'");
                    continue;
                }

                ProcessBatchKey(regKey, key, regHive.HivePath, d, f, csv, csvf, dt);
            }
        }
    }

    private static bool CheckMinSwitches(string sk, string sv, string sd, string ss, string sa, string kn, int minSize, int base64, string bn)
    {
        if (sk.IsNullOrEmpty() &&
            sd.IsNullOrEmpty() &&
            sv.IsNullOrEmpty() &&
            ss.IsNullOrEmpty() &&
            sa.IsNullOrEmpty() &&
            kn.IsNullOrEmpty() &&
            minSize == 0 &&
            base64 == 0 &&
            bn.IsNullOrEmpty())
        {
            return false;
        }

        return true;
    }

    private static List<IRegistryPluginBase> GetPluginsToActivate(RegistryKey regKey, Key key)
    {
        var pluginsToActivate = new List<IRegistryPluginBase>();

        var keyPath = Helpers.StripRootKeyNameFromKeyPath(regKey.KeyPath);

        foreach (var registryPluginBase in Plugins)
        foreach (var path in registryPluginBase.KeyPaths)
        {
            if (path.Contains("*"))
            {
                var rPath = path.Replace("*", ".+?").Replace("\\", "\\\\");

                if (!Regex.IsMatch(keyPath, $@"{rPath}\z", RegexOptions.IgnoreCase))
                {
                    continue;
                }


                if (registryPluginBase.ValueName == null)
                {
                    pluginsToActivate.Add(registryPluginBase);
                }
                else
                {
                    if (key.ValueName != null && registryPluginBase.ValueName.ToLowerInvariant() ==
                        key.ValueName?.ToLowerInvariant())
                    {
                        pluginsToActivate.Add(registryPluginBase);
                    }
                }
            }
            else
            {
                if (path.ToLowerInvariant().Contains(keyPath.ToLowerInvariant()))
                {
                    if (registryPluginBase.ValueName == null &&
                        path.ToLowerInvariant().Equals(keyPath.ToLowerInvariant()))
                    {
                        pluginsToActivate.Add(registryPluginBase);
                    }
                    else
                    {
                        if (key.ValueName != null && registryPluginBase.ValueName?.ToLowerInvariant() ==
                            key.ValueName?.ToLowerInvariant())
                        {
                            pluginsToActivate.Add(registryPluginBase);
                        }
                    }
                }
            }
        }

        return pluginsToActivate;
    }

    private static void BatchDumpKey(RegistryKey regKey, Key key, string hivePath, string d, string f, string csv, string csvf, string dt)
    {
        _logger.Trace($"Batch dumping '{regKey.KeyPath}' in '{hivePath}'");

        if (regKey == null)
        {
            throw new NullReferenceException("regKey is null!");
        }

        //1. dump all key values (valueName is null)
        //2. dump only value in a key (value name is specified)

        var pluginsToActivate = GetPluginsToActivate(regKey, key);

        if (key.DisablePlugin == false && pluginsToActivate.Count > 0)
        {
            foreach (var registryPluginBase in pluginsToActivate)
            {
                var pig = (IRegistryPluginGrid)registryPluginBase;

                pig.ProcessValues(regKey);

                var pluginDetailsFile = DumpPluginValues(pig, hivePath, d, f, csv, csvf);

                var path = hivePath;

                if (hivePath.StartsWith(VssDir))
                {
                    path = $"VSS{hivePath.Replace($"{VssDir}\\", "")}'";
                }

                foreach (var pigValue in pig.Values)
                {
                    var conv = (IValueOut)pigValue;

                    var rebOut = new BatchCsvOut
                    {
                        ValueName = conv.BatchValueName,
                        Deleted = regKey.NkRecord.IsDeleted,
                        Description = key.Description,
                        Category = key.Category,
                        Comment = key.Comment,
                        HivePath = path,
                        HiveType = key.HiveType.ToString(),
                        KeyPath = regKey.KeyPath,
                        LastWriteTimestamp = regKey.LastWriteTime.Value,
                        Recursive = key.Recursive,
                        ValueType = "(plugin)",
                        ValueData = conv.BatchValueData1,
                        ValueData2 = conv.BatchValueData2,
                        ValueData3 = conv.BatchValueData3,
                        PluginDetailFile = pluginDetailsFile
                    };

                    _batchCsvOutList.Add(rebOut);
                }

                if (pig.Errors.Count > 0)
                {
                    _logger.Warn($"Plugin {pig.PluginName} error. Errors: {string.Join(", ", pig.Errors)}");
                }
            }
        }
        else
        {
            if (key.ValueName.IsNullOrEmpty() == false)
            {
                //one value only
                var regVal = regKey.Values.SingleOrDefault(t =>
                    t.ValueName.ToLowerInvariant() == key.ValueName.ToLowerInvariant());

                if (regVal != null)
                {
                    var rebOut = BuildBatchCsvOut(regKey, key, hivePath, regVal, dt);

                    _batchCsvOutList.Add(rebOut);
                }
            }
            else
            {
                if (regKey.Values.Any() == false)
                {
                    //dump the key itself since there are no values to pull in the times, etc.
                    var keyOut = BuildBatchCsvOut(regKey, key, hivePath, null, dt);

                    _batchCsvOutList.Add(keyOut);
                }


                //dump all values from current key
                foreach (var regKeyValue in regKey.Values)
                {
                    var rebOut = BuildBatchCsvOut(regKey, key, hivePath, regKeyValue, dt);

                    _batchCsvOutList.Add(rebOut);
                }
            }
        }
    }

    private static string DumpPluginValues(IRegistryPluginGrid plugin, string hivePath, string d, string f, string csv, string csvf)
    {
        var pluginType = plugin.GetType();

        if (plugin.Values.Count == 0)
        {
            return null;
        }

        var dirName = d;

        if (f.IsNullOrEmpty() == false)
        {
            dirName = Path.GetDirectoryName(f);
        }

        var hiveName1 = hivePath.Replace(dirName, "").Replace(":", "").Replace("\\", "_");

        if (hivePath.StartsWith(VssDir))
        {
            hiveName1 = $"VSS{hivePath.Replace($"{VssDir}\\", "").Replace(":", "").Replace("\\", "_")}";
        }

        var outbase = $"{RunTimestamp}_{pluginType.Name}_{hiveName1}.csv";

        if (Directory.Exists(csv) == false)
        {
            _logger.Warn(
                $"Path to '{csv}' doesn't exist. Creating...");
            Directory.CreateDirectory(csv);
        }

        if (csvf.IsNullOrEmpty() == false)
        {
            outbase =
                $"{Path.GetFileNameWithoutExtension(csvf)}_{pluginType.Name}{Path.GetExtension(csvf)}";
        }

        var outFile = Path.Combine(csv, RunTimestamp, outbase);

        if (Directory.Exists(Path.GetDirectoryName(outFile)) == false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outFile));
        }

        var exists = File.Exists(outFile);

        using (var sw = new StreamWriter(outFile, true, Encoding.UTF8))
        {
            var csvWriter = new CsvWriter(sw, CultureInfo.InvariantCulture);

            var foo = csvWriter.Context.AutoMap(plugin.Values[0].GetType());

            foreach (var fooMemberMap in foo.MemberMaps)
            {
                if (fooMemberMap.Data.Member.ToString().Contains("DateTime") ||
                    fooMemberMap.Data.Member.ToString().Contains("DateTimeOffset"))
                {
                    fooMemberMap.Data.TypeConverter = new NullConverter();
                }

                if (fooMemberMap.Data.Member.Name.StartsWith("BatchValueData"))
                {
                    fooMemberMap.Ignore();
                }

                if (fooMemberMap.Data.Member.Name.StartsWith("BatchKeyPath"))
                {
                    fooMemberMap.Index(0);
                }

                if (fooMemberMap.Data.Member.Name.StartsWith("BatchValueName"))
                {
                    fooMemberMap.Index(1);
                }
            }

            csvWriter.Context.RegisterClassMap(foo);

            if (exists == false)
            {
                csvWriter.WriteHeader(plugin.Values[0].GetType());

                csvWriter.NextRecord();
            }

            foreach (var pluginValue in plugin.Values)
            {
                csvWriter.WriteRecord(pluginValue);
                csvWriter.NextRecord();
            }

            sw.Flush();
        }


        return outFile;
    }

    private static BatchCsvOut BuildBatchCsvOut(RegistryKey regKey, Key key, string hivePath, KeyValue regVal, string dt)
    {
        var path = hivePath;

        if (hivePath.StartsWith(VssDir))
        {
            path = $"VSS{hivePath.Replace($"{VssDir}\\", "")}'";
        }

        var rebOut = new BatchCsvOut
        {
            ValueName = regVal == null ? "" : regVal.ValueName,
            Deleted = regKey.NkRecord.IsDeleted,
            Description = key.Description,
            Category = key.Category,
            Comment = key.Comment,
            HivePath = path,
            HiveType = key.HiveType.ToString(),
            KeyPath = regKey.KeyPath,
            LastWriteTimestamp = regKey.LastWriteTime.Value,
            Recursive = key.Recursive,
            ValueType = regVal == null ? "" : regVal.ValueType
        };

        if (regVal?.ValueType == "RegBinary")
        {
            rebOut.ValueData = "(Binary data)";

            if (key.IncludeBinary)
            {
                switch (key.BinaryConvert)
                {
                    case Key.BinConvert.Filetime:
                        try
                        {
                            var ft = DateTimeOffset.FromFileTime((long)BitConverter.ToUInt64(regVal.ValueDataRaw, 0));
                            rebOut.ValueData = ft.ToUniversalTime()
                                .ToString(dt);
                        }
                        catch (Exception)
                        {
                            _logger.Warn("Error converting to FILETIME. Using bytes instead!");
                            rebOut.ValueData = regVal.ValueData;
                        }

                        break;
                    case Key.BinConvert.Ip:
                        try
                        {
                            var ipNumeric = BitConverter.ToUInt32(regVal.ValueDataRaw, 0);
                            var ipString = new IPAddress(ipNumeric).ToString();
                            rebOut.ValueData = ipString;
                        }
                        catch (Exception e)
                        {
                            _logger.Warn($"Error converting to IP address. Using bytes instead! Error: {e.Message}");
                            rebOut.ValueData = regVal.ValueData;
                        }

                        break;

                    case Key.BinConvert.Epoch:
                        try
                        {
                            var ft = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(regVal.ValueDataRaw, 0));
                            rebOut.ValueData = ft.ToUniversalTime()
                                .ToString(dt);
                        }
                        catch (Exception)
                        {
                            _logger.Warn("Error converting to Epoch. Using bytes instead!");
                            rebOut.ValueData = regVal.ValueData;
                        }

                        break;
                    case Key.BinConvert.Sid:
                        try
                        {
                            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                            {
                                var sid = new SecurityIdentifier(regVal.ValueDataRaw, 0);

                                rebOut.ValueData = sid.ToString();
                            }
                            else
                            {
                                rebOut.ValueData = $"<SID conversion only available on Windows. Using bytes instead>: {regVal.ValueData}";
                            }
                        }
                        catch (Exception)
                        {
                            _logger.Warn("Error converting to SID. Using bytes instead!");
                            rebOut.ValueData = regVal.ValueData;
                        }

                        break;
                    default:
                        rebOut.ValueData = regVal.ValueData;
                        break;
                }
            }
        }
        else
        {
            rebOut.ValueData = regVal == null ? "" : regVal.ValueData;

            switch (key.BinaryConvert)
            {
                case Key.BinConvert.Epoch:
                    try
                    {
                        var ft = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(regVal.ValueDataRaw, 0));
                        rebOut.ValueData = ft.ToUniversalTime()
                            .ToString(dt);
                    }
                    catch (Exception)
                    {
                        _logger.Warn("Error converting to Epoch. Using bValueDataytes instead!");
                        rebOut.ValueData = regVal.ValueData;
                    }

                    break;

                case Key.BinConvert.Filetime:
                    try
                    {
                        var ft = DateTimeOffset.FromFileTime((long)BitConverter.ToUInt64(regVal.ValueDataRaw, 0));
                        rebOut.ValueData = ft.ToUniversalTime()
                            .ToString(dt);
                    }
                    catch (Exception)
                    {
                        _logger.Warn("Error converting to FILETIME. Using bytes instead!");
                        rebOut.ValueData = regVal.ValueData;
                    }

                    break;
            }
        }

        return rebOut;
    }

    private static ReBatch ValidateBatchFile(string bn)
    {
        var deserializer = new DeserializerBuilder()
            .Build();

        var hasError = false;

        ReBatch re = null;

        try
        {
            re = deserializer.Deserialize<ReBatch>(File.ReadAllText(bn));
            var validator = new ReBatchValidator();

            var validate = validator.Validate(re);
            DisplayValidationResults(validate, bn);
        }
        catch (SyntaxErrorException se)
        {
            _logger.Warn($"\r\nSyntax error in '{bn}':");
            _logger.Fatal(se.Message);

            var lines = File.ReadLines(bn).ToList();
            var fileContents = bn.ReadAllText();

            var badLine = lines[se.Start.Line - 1];

            _logger.Fatal(
                $"\r\nBad line (or close to it) '{badLine}' has invalid data at column '{se.Start.Column}'");

            if (fileContents.Contains('\t'))
            {
                _logger.Error(
                    "\r\nBad line contains one or more tab characters. Replace them with spaces\r\n");
                _logger.Info(fileContents.Replace("\t", "<TAB>"));
            }

            hasError = true;
        }
        catch (YamlException ye)
        {
            _logger.Warn($"\r\nSyntax error in '{bn}':");
            _logger.Fatal(ye.Message);

            _logger.Fatal(ye.InnerException?.Message);

            hasError = true;
        }

        catch (Exception e)
        {
            _logger.Warn($"\r\nError when validating '{bn}'");
            _logger.Fatal(e);
            hasError = true;
        }

        if (hasError)
        {
            _logger.Warn(
                "\r\n\r\nThe batch file failed validation. Fix the issues and try again\r\n");
            Environment.Exit(0);
        }

        return re;
    }

    private static void DisplayValidationResults(ValidationResult result, string source)
    {
        _logger.Trace($"Performing validation on '{source}': {result.Dump()}");
        if (result.Errors.Count == 0)
        {
            return;
        }

        _logger.Error($"\r\n{source} had validation errors:");

        foreach (var validationFailure in result.Errors)
        {
            _logger.Error(validationFailure);
        }

        _logger.Error("\r\nCorrect the errors and try again. Exiting");

        Environment.Exit(0);
    }

    private static SimpleKey BuildJson(RegistryKey key)
    {
        var sk = new SimpleKey
        {
            KeyName = key.KeyName,
            KeyPath = key.KeyPath,
            LastWriteTimestamp = key.LastWriteTime.Value
        };
        foreach (var keyValue in key.Values)
        {
            var sv = new SimpleValue
            {
                ValueType = keyValue.ValueType,
                ValueData = keyValue.ValueData,
                ValueName = keyValue.ValueName,
                DataRaw = keyValue.ValueDataRaw,
                Slack = keyValue.ValueSlackRaw
            };
            sk.Values.Add(sv);
        }

        foreach (var registryKey in key.SubKeys)
        {
            var skk = BuildJson(registryKey);
            sk.SubKeys.Add(skk);
        }

        return sk;
    }

    private static string StripInvalidCharsFromFileName(string initialFileName, string substituteWith)
    {
        var regex = $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]";
        var removeInvalidChars = new Regex(regex,
            RegexOptions.Singleline | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        var newpath = removeInvalidChars.Replace(initialFileName, substituteWith);

        newpath = newpath.Trim('\0');

        return newpath.Replace("%", "");
    }

    private static IEnumerable<SearchHit> DoValueSlackSearch(RegistryHive reg, string simpleSearchValueSlack,
        bool isRegEx, bool isLiteral)
    {
        var hits = reg.FindInValueDataSlack(simpleSearchValueSlack, isRegEx, isLiteral).ToList();
        return hits;
    }

    private static IEnumerable<SearchHit> DoValueDataSearch(RegistryHive reg, string simpleSearchValueData,
        bool isRegEx, bool isLiteral)
    {
        var hits = reg.FindInValueData(simpleSearchValueData, isRegEx, isLiteral).ToList();
        return hits;
    }

    private static IEnumerable<SearchHit> DoValueSearch(RegistryHive reg, string simpleSearchValue, bool isRegEx)
    {
        var hits = reg.FindInValueName(simpleSearchValue, isRegEx).ToList();
        return hits;
    }

    private static IEnumerable<SearchHit> DoKeySearch(RegistryHive reg, string simpleSearchKey, bool isRegEx)
    {
        var hits = reg.FindInKeyName(simpleSearchKey, isRegEx).ToList();
        return hits;
    }

    private static void AddHighlightingRules(List<string> words, bool isRegEx = false)
    {
        var target = (ColoredConsoleTarget)LogManager.Configuration.FindTargetByName("console");
        var rule = target.WordHighlightingRules.FirstOrDefault();

        var bgColor = ConsoleOutputColor.Green;
        var fgColor = ConsoleOutputColor.Red;

        if (rule != null)
        {
            bgColor = rule.BackgroundColor;
            fgColor = rule.ForegroundColor;
        }

        target.WordHighlightingRules.Clear();

        foreach (var word in words)
        {
            var r = new ConsoleWordHighlightingRule
            {
                IgnoreCase = true
            };
            if (isRegEx)
            {
                r.Regex = word;
            }
            else
            {
                r.Text = word;
            }

            r.ForegroundColor = fgColor;
            r.BackgroundColor = bgColor;

            r.WholeWords = false;
            target.WordHighlightingRules.Add(r);
        }
    }

    public class NullConverter : DefaultTypeConverter
    {
        public override string ConvertToString(object value, IWriterRow row, MemberMapData memberMapData)
        {
            switch (value)
            {
                case DateTimeOffset dto:
                    return dto.ToString(dtf);
                case DateTime dt:
                    return dt.ToString(dtf);
                default:
                    return base.ConvertToString(value, row, memberMapData);
            }
        }
    }
}