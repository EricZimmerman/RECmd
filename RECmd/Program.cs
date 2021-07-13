using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Security;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Exceptionless;
using Fclp;
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

namespace RECmd
{
    internal class Program
    {
        private static Stopwatch _sw;
        private static Logger _logger;
        private static FluentCommandLineParser<ApplicationArguments> _fluentCommandLineParser;
        private static List<BatchCsvOut> _batchCsvOutList;
        private static readonly List<IRegistryPluginBase> Plugins = new List<IRegistryPluginBase>();

        private static readonly string BaseDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private static readonly string RunTimestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");

        private static string _pluginsDir = string.Empty;
        private const string VssDir = @"C:\___vssMount";

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

                        var plugin = (IRegistryPluginBase) Activator.CreateInstance(exportedType);

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

        private static HashSet<string> _seenHashes = new HashSet<string>();

        private static void Main(string[] args)
        {
            ExceptionlessClient.Default.Startup("fTcEOUkt1CxljTyOZfsr8AcSGQwWE4aYaYqk7cE1");
            SetupNLog();

            _pluginsDir = Path.Combine(BaseDirectory, "Plugins");

            if (Directory.Exists(_pluginsDir) == false)
            {
                Directory.CreateDirectory(_pluginsDir);
            }

            _logger = LogManager.GetCurrentClassLogger();

            _fluentCommandLineParser = new FluentCommandLineParser<ApplicationArguments>
            {
                IsCaseSensitive = false
            };

            _fluentCommandLineParser.Setup(arg => arg.Directory)
                .As('d')
                .WithDescription(
                    "Directory to look for hives (recursively). -f or -d is required.");
            _fluentCommandLineParser.Setup(arg => arg.HiveFile)
                .As('f')
                .WithDescription("Hive to search. -f or -d is required.\r\n");

            _fluentCommandLineParser.Setup(arg => arg.Quiet)
                .As('q')
                .WithDescription("Quiet mode. When true, hide processing details. Default is FALSE\r\n").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.KeyName)
                .As("kn")
                .WithDescription(
                    "Display details for key name. Includes subkeys and values");
            _fluentCommandLineParser.Setup(arg => arg.ValueName)
                .As("vn")
                .WithDescription(
                    "Value name. Only this value will be dumped");
            _fluentCommandLineParser.Setup(arg => arg.BatchName)
                .As("bn")
                .WithDescription(
                    "Use settings from supplied file to find keys/values. See included sample file for examples");
            _fluentCommandLineParser.Setup(arg => arg.CsvDirectory)
                .As("csv")
                .WithDescription(
                    "Directory to save CSV formatted results to. Required when -bn is used.");

            _fluentCommandLineParser.Setup(arg => arg.CsvName)
                .As("csvf")
                .WithDescription(
                    "File name to save CSV formatted results to. When present, overrides default name");


            _fluentCommandLineParser.Setup(arg => arg.SaveToName)
                .As("saveTo")
                .WithDescription("Saves --vn value data in binary form to file. Expects path to a FILE");
            _fluentCommandLineParser.Setup(arg => arg.Json)
                .As("json")
                .WithDescription(
                    "Export --kn to directory specified by --json. Ignored when --vn is specified");
            _fluentCommandLineParser.Setup(arg => arg.JsonName)
                .As("jsonf")
                .WithDescription(
                    "File name to save JSON formatted results to. When present, overrides default name\r\n");

            _fluentCommandLineParser.Setup(arg => arg.Detailed)
                .As("details")
                .WithDescription(
                    "Show more details when displaying results. Default is FALSE\r\n").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.Base64)
                .As("Base64")
                .WithDescription("Find Base64 encoded values with size >= Base64 (specified in bytes)");
            _fluentCommandLineParser.Setup(arg => arg.MinimumSize)
                .As("MinSize")
                .WithDescription("Find values with data size >= MinSize (specified in bytes)\r\n");


            _fluentCommandLineParser.Setup(arg => arg.SimpleSearchAll)
                .As("sa")
                .WithDescription("Search for <string> in keys, values, data, and slack.");

            _fluentCommandLineParser.Setup(arg => arg.SimpleSearchKey)
                .As("sk")
                .WithDescription("Search for <string> in key names.");

            _fluentCommandLineParser.Setup(arg => arg.SimpleSearchValue)
                .As("sv")
                .WithDescription("Search for <string> in value names");

            _fluentCommandLineParser.Setup(arg => arg.SimpleSearchValueData)
                .As("sd")
                .WithDescription("Search for <string> in value record's value data");

            _fluentCommandLineParser.Setup(arg => arg.SimpleSearchValueSlack)
                .As("ss")
                .WithDescription("Search for <string> in value record's value slack");


            _fluentCommandLineParser.Setup(arg => arg.Literal)
                .As("literal")
                .WithDescription(
                    "If true, --sd and --ss search value will not be interpreted as ASCII or Unicode byte strings")
                .SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.SuppressData)
                .As("nd")
                .WithDescription(
                    "If true, do not show data when using --sd or --ss. Default is FALSE").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.RegEx)
                .As("regex")
                .WithDescription(
                    "If present, treat <string> in --sk, --sv, --sd, and --ss as a regular expression. Default is FALSE\r\n")
                .SetDefault(false);


            _fluentCommandLineParser.Setup(arg => arg.DateTimeFormat)
                .As("dt")
                .WithDescription(
                    "The custom date/time format to use when displaying time stamps. Default is: yyyy-MM-dd HH:mm:ss.fffffff")
                .SetDefault("yyyy-MM-dd HH:mm:ss.fffffff");
            _fluentCommandLineParser.Setup(arg => arg.NoTransLogs)
                .As("nl")
                .WithDescription(
                    "When true, ignore transaction log files for dirty hives. Default is FALSE").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.RecoverDeleted)
                .As("recover")
                .WithDescription("If true, recover deleted keys/values. Default is TRUE\r\n").SetDefault(true);
            
            _fluentCommandLineParser.Setup(arg => arg.Vss)
                .As("vss")
                .WithDescription(
                    "Process all Volume Shadow Copies that exist on drive specified by -f or -d . Default is FALSE")
                .SetDefault(false);
            _fluentCommandLineParser.Setup(arg => arg.Dedupe)
                .As("dedupe")
                .WithDescription(
                    "Deduplicate -f or -d & VSCs based on SHA-1. First file found wins. Default is TRUE\r\n")
                .SetDefault(true);

            _fluentCommandLineParser.Setup(arg => arg.Sync)
                .As("sync")
                .WithDescription(
                    "If true, the latest batch files from https://github.com/EricZimmerman/RECmd/tree/master/BatchExamples are downloaded and local files updated. Default is FALSE\r\n")
                .SetDefault(false);
            _fluentCommandLineParser.Setup(arg => arg.Debug)
                .As("debug")
                .WithDescription("Show debug information during processing").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.Trace)
                .As("trace")
                .WithDescription("Show trace information during processing").SetDefault(false);


            var header =
                $"RECmd version {Assembly.GetExecutingAssembly().GetName().Version}" +
                "\r\n\r\nAuthor: Eric Zimmerman (saericzimmerman@gmail.com)" +
                "\r\nhttps://github.com/EricZimmerman/RECmd\r\n\r\nNote: Enclose all strings containing spaces (and all RegEx) with double quotes";

            var footer = @"Example: RECmd.exe --f ""C:\Temp\UsrClass 1.dat"" --sk URL --recover false --nl" +
                         "\r\n\t " +
                         @"RECmd.exe --f ""D:\temp\UsrClass 1.dat"" --StartDate ""11/13/2014 15:35:01"" " +
                         "\r\n\t " +
                         @"RECmd.exe --f ""D:\temp\UsrClass 1.dat"" --RegEx --sv ""(App|Display)Name"" " + "\r\n";

            _fluentCommandLineParser.SetupHelp("?", "help").WithHeader(header)
                .Callback(text => _logger.Info(text + "\r\n" + footer));

            var result = _fluentCommandLineParser.Parse(args);

            if (_fluentCommandLineParser.Object.Debug)
            {
                foreach (var r in LogManager.Configuration.LoggingRules)
                {
                    r.EnableLoggingForLevel(LogLevel.Debug);
                }

                LogManager.ReconfigExistingLoggers();
                _logger.Debug("Enabled debug messages...");
            }

            if (_fluentCommandLineParser.Object.Trace)
            {
                foreach (var r in LogManager.Configuration.LoggingRules)
                {
                    r.EnableLoggingForLevel(LogLevel.Trace);
                }

                LogManager.ReconfigExistingLoggers();
                _logger.Trace("Enabled trace messages...");
            }

            if (_fluentCommandLineParser.Object.Vss & (Helper.IsAdministrator() == false))
            {
                _logger.Error("--vss is present, but administrator rights not found. Exiting\r\n");
                return;
            }

            if (result.HelpCalled)
            {
                return;
            }

            if (result.HasErrors)
            {
                _logger.Error("");
                _logger.Error(result.ErrorText);


                _fluentCommandLineParser.HelpOption.ShowHelp(_fluentCommandLineParser.Options);

                return;
            }

            if (_fluentCommandLineParser.Object.Sync)
            {
                try
                {
                    _logger.Info(header);
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

            _logger.Info(header);
            _logger.Info("");
            _logger.Info($"Command line: {string.Join(" ", Environment.GetCommandLineArgs().Skip(1))}\r\n");

            if (_fluentCommandLineParser.Object.Vss)
            {
                string driveLetter;
                if (_fluentCommandLineParser.Object.HiveFile.IsEmpty() == false)
                {
                    driveLetter = Path.GetPathRoot(Path.GetFullPath(_fluentCommandLineParser.Object.HiveFile))
                        .Substring(0, 1);
                }
                else
                {
                    driveLetter = Path.GetPathRoot(Path.GetFullPath(_fluentCommandLineParser.Object.Directory))
                        .Substring(0, 1);
                }
                
                Helper.MountVss(driveLetter,VssDir);
                Console.WriteLine();
            }

            if (_fluentCommandLineParser.Object.BatchName?.Length > 0) //batch mode
            {
                if (File.Exists(_fluentCommandLineParser.Object.BatchName) == false)
                {
                    _logger.Error($"Batch file '{_fluentCommandLineParser.Object.BatchName}' does not exist.");
                    return;
                }

                if (_fluentCommandLineParser.Object.CsvDirectory.IsNullOrEmpty())
                {
                    _logger.Error("--csv is required when using --bn. Exiting.");
                    return;
                }

                reBatch = ValidateBatchFile();
            }


            if (_fluentCommandLineParser.Object.HiveFile?.Length > 0)
            {
                if (File.Exists(_fluentCommandLineParser.Object.HiveFile) == false)
                {
                    _logger.Error($"File '{_fluentCommandLineParser.Object.HiveFile}' does not exist.");
                    return;
                }

                if (CheckMinSwitches() == false)
                {
                    _logger.Error(
                        "\r\nOne of the following switches is required: --sa | --sk | --sv | --sd | --ss | --kn | --Base64 | --MinSize | --bn\r\n\r\n");
                    _logger.Info("Verify the command line and try again");
                    return;
                }

                hivesToProcess.Add(_fluentCommandLineParser.Object.HiveFile);

                if (_fluentCommandLineParser.Object.Vss)
                {
                    var vssDirs = Directory.GetDirectories(VssDir);

                    var root = Path.GetPathRoot(Path.GetFullPath(_fluentCommandLineParser.Object.HiveFile));
                    var stem = Path.GetFullPath(_fluentCommandLineParser.Object.HiveFile).Replace(root, "");

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
            else if (_fluentCommandLineParser.Object.Directory?.Length > 0)
            {
                if (Directory.Exists(_fluentCommandLineParser.Object.Directory) == false)
                {
                    _logger.Error($"Directory '{_fluentCommandLineParser.Object.Directory}' does not exist.");
                    return;
                }

                if (CheckMinSwitches() == false)
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

                var f = new DirectoryEnumerationFilters();
                f.InclusionFilter = fsei =>
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

                        var rawf = Helper.GetFiles(files);

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
                        }

                        return false;
                    }
                };

                f.RecursionFilter = entryInfo => !entryInfo.IsMountPoint && !entryInfo.IsSymbolicLink;

                f.ErrorFilter = (errorCode, errorMessage, pathProcessed) => true;

                var dirEnumOptions =
                    DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive |
                    DirectoryEnumerationOptions.SkipReparsePoints | DirectoryEnumerationOptions.ContinueOnException |
                    DirectoryEnumerationOptions.BasicSearch;

                _logger.Fatal($"Searching '{_fluentCommandLineParser.Object.Directory}' for hives...");

                var files2 =
                    Directory.EnumerateFileSystemEntries(_fluentCommandLineParser.Object.Directory, dirEnumOptions, f);

                var count = 0;

                try
                {
                    hivesToProcess.AddRange(files2);
                    count = hivesToProcess.Count;

                    _logger.Info($"\tHives found: {count:N0}");
                }
                catch (Exception)
                {
                    _logger.Fatal($"Could not access all files in '{_fluentCommandLineParser.Object.Directory}'");
                    _logger.Error("");
                    _logger.Fatal("Rerun the program with Administrator privileges to try again\r\n");
                    //Environment.Exit(-1);
                }

                if (_fluentCommandLineParser.Object.Vss)
                {
                    var vssDirs = Directory.GetDirectories(VssDir);

                    

                    foreach (var vssDir in vssDirs)
                    {
                        var root = Path.GetPathRoot(Path.GetFullPath(_fluentCommandLineParser.Object.Directory));
                        var stem = Path.GetFullPath(_fluentCommandLineParser.Object.Directory).Replace(root, "");

                        var target = Path.Combine(vssDir, stem);

                        _logger.Fatal($"Searching 'VSS{target.Replace($"{VssDir}\\","")}' for hives...");

                        files2 =
                            Directory.EnumerateFileSystemEntries(target, dirEnumOptions, f);

                        try
                        {
                            hivesToProcess.AddRange(files2);

                            count = hivesToProcess.Count - count;

                            _logger.Info($"\tHives found: {count:N0}");
                        }
                        catch (Exception)
                        {
                            _logger.Fatal($"Could not access all files in '{_fluentCommandLineParser.Object.Directory}'");
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
                _fluentCommandLineParser.HelpOption.ShowHelp(_fluentCommandLineParser.Options);
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

                        using (var fs = new FileStream(hiveToProcess,FileMode.Open,FileAccess.Read))
                        {
                            if (_fluentCommandLineParser.Object.Dedupe)
                            {
                                var sha = Helper.GetSha1FromStream(fs,0);
                                if (_seenHashes.Contains(sha))
                                {
                                    _logger.Debug($"Skipping '{hiveToProcess}' as a file with SHA-1 '{sha}' has already been processed");
                                    continue;
                                }

                                _seenHashes.Add(sha);
                            }

                            fs.Seek(0, SeekOrigin.Begin);

                            reg = new RegistryHive(fs.ReadFully(),hiveToProcess)
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

                        rawFiles = Helper.GetFiles(files);

                        if (rawFiles.First().FileStream.Length == 0)
                        {
                            continue;
                        }

                        if (_fluentCommandLineParser.Object.Dedupe)
                        {
                            var sha = Helper.GetSha1FromStream(rawFiles.First().FileStream,0);
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
                            if (_fluentCommandLineParser.Object.NoTransLogs == false)
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
                            if (_fluentCommandLineParser.Object.NoTransLogs == false)
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

                    if (_fluentCommandLineParser.Object.SimpleSearchAll.Length > 0 ||
                        _fluentCommandLineParser.Object.SimpleSearchKey.Length > 0 ||
                        _fluentCommandLineParser.Object.SimpleSearchValue.Length > 0 ||
                        _fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0 ||
                        _fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
                    {
                        searchUsed = true;

                        if (_fluentCommandLineParser.Object.SimpleSearchAll.IsNullOrEmpty() == false)
                        {
                            _fluentCommandLineParser.Object.SimpleSearchKey =_fluentCommandLineParser.Object.SimpleSearchAll;
                            _fluentCommandLineParser.Object.SimpleSearchValue =_fluentCommandLineParser.Object.SimpleSearchAll;
                            _fluentCommandLineParser.Object.SimpleSearchValueData =_fluentCommandLineParser.Object.SimpleSearchAll;
                            _fluentCommandLineParser.Object.SimpleSearchValueSlack =_fluentCommandLineParser.Object.SimpleSearchAll;
                        }

                        var hits = new List<SearchHit>();

                        if (_fluentCommandLineParser.Object.SimpleSearchKey.Length > 0)
                        {
                            var results = DoKeySearch(reg, _fluentCommandLineParser.Object.SimpleSearchKey,
                                _fluentCommandLineParser.Object.RegEx);
                            if (results != null)
                            {
                                hits.AddRange(results);
                            }
                        }

                        if (_fluentCommandLineParser.Object.SimpleSearchValue.Length > 0)
                        {
                            var results = DoValueSearch(reg, _fluentCommandLineParser.Object.SimpleSearchValue,
                                _fluentCommandLineParser.Object.RegEx);
                            if (results != null)
                            {
                                hits.AddRange(results);
                            }
                        }

                        if (_fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0)
                        {
                            var results = DoValueDataSearch(reg, _fluentCommandLineParser.Object.SimpleSearchValueData,
                                _fluentCommandLineParser.Object.RegEx, _fluentCommandLineParser.Object.Literal);
                            if (results != null)
                            {
                                hits.AddRange(results);
                            }
                        }

                        if (_fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
                        {
                            var results = DoValueSlackSearch(reg,
                                _fluentCommandLineParser.Object.SimpleSearchValueSlack,
                                _fluentCommandLineParser.Object.RegEx, _fluentCommandLineParser.Object.Literal);
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
                            if (_fluentCommandLineParser.Object.SimpleSearchAll.Length > 0)
                            {
                                words.Add(_fluentCommandLineParser.Object.SimpleSearchAll);
                            }
                            if (_fluentCommandLineParser.Object.SimpleSearchKey.Length > 0)
                            {
                                words.Add(_fluentCommandLineParser.Object.SimpleSearchKey);
                            } 
                            if (_fluentCommandLineParser.Object.SimpleSearchValue.Length > 0)
                            {
                                words.Add(_fluentCommandLineParser.Object.SimpleSearchValue);
                            } 
                            if (_fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0)
                            {
                                if (_fluentCommandLineParser.Object.RegEx)
                                {
                                    words.Add(_fluentCommandLineParser.Object.SimpleSearchValueData);
                                }
                                else
                                {
                                    if (searchHit.Value?.VkRecord.DataType == VkCellRecord.DataTypeEnum.RegBinary)
                                    {
                                        words.Add(searchHit.HitString);
                                    }
                                    else
                                    {
                                        words.Add(_fluentCommandLineParser.Object.SimpleSearchValueData);
                                    }
                                }
                            } 
                            if (_fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
                            {
                                if (_fluentCommandLineParser.Object.RegEx)
                                {
                                    words.Add(_fluentCommandLineParser.Object.SimpleSearchValueSlack);
                                }
                                else
                                {
                                    words.Add(searchHit.HitString);
                                }
                            }
                        }

                        AddHighlightingRules(words.ToList(), _fluentCommandLineParser.Object.RegEx);

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
                                    if (_fluentCommandLineParser.Object.SuppressData)
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
                                        if (searchHit.Value != null &&  _fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0)
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
                                    if (_fluentCommandLineParser.Object.SuppressData)
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
                                            _fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
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

                        var target = (ColoredConsoleTarget) LogManager.Configuration.FindTargetByName("console");
                        target.WordHighlightingRules.Clear();

//                    //TODO search deleted?? should only need to look in reg.UnassociatedRegistryValues
                    } //End s* options
                    
                    if (_fluentCommandLineParser.Object.KeyName.IsNullOrEmpty() == false)
                    {
                        //dumping key and/or values
                        searchUsed = true;

                        var key = reg.GetKey(_fluentCommandLineParser.Object.KeyName);

                        if (key == null && _fluentCommandLineParser.Object.KeyName.ToUpperInvariant() == "ROOT")
                        {
                            _logger.Info($"\tUsing 'ROOT' alias. Actual ROOT key name: '{reg.Root.KeyName}'");
                            key = reg.Root;
                        }

                        KeyValue val = null;

                        if (key == null)
                        {
                            _logger.Warn($"Key '{_fluentCommandLineParser.Object.KeyName}' not found.");

                            continue;
                        }

                        if (_fluentCommandLineParser.Object.ValueName.Length > 0)
                        {
                            val = key.Values.SingleOrDefault(c =>
                                c.ValueName == _fluentCommandLineParser.Object.ValueName);

                            if (val == null)
                            {
                                _logger.Warn(
                                    $"Value '{_fluentCommandLineParser.Object.ValueName}' not found for key '{_fluentCommandLineParser.Object.KeyName}'.");

                                continue;
                            }

                            if (_fluentCommandLineParser.Object.SaveToName.Length > 0)
                            {
                                var baseDir = Path.GetDirectoryName(_fluentCommandLineParser.Object.SaveToName);
                                if (Directory.Exists(baseDir) == false)
                                {
                                    Directory.CreateDirectory(baseDir);
                                }

                                _logger.Warn(
                                    $"Saving contents of '{val.ValueName}' to '{_fluentCommandLineParser.Object.SaveToName}\r\n'");
                                try
                                {
                                    File.WriteAllBytes(_fluentCommandLineParser.Object.SaveToName, val.ValueDataRaw);
                                }
                                catch (Exception ex)
                                {
                                    _logger.Error(
                                        $"Save failed to '{_fluentCommandLineParser.Object.SaveToName}'. Error: {ex.Message}");
                                }
                            }
                        }

                        var keyIsDeleted = (key.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) ==
                                           RegistryKey.KeyFlagsEnum.Deleted;

                        //dump key here
                        if (_fluentCommandLineParser.Object.ValueName.IsNullOrEmpty())
                        {
                            if (_fluentCommandLineParser.Object.Json.IsNullOrEmpty() == false)
                            {
                                //export to json
                                if (Directory.Exists(_fluentCommandLineParser.Object.Json) == false)
                                {
                                    Directory.CreateDirectory(_fluentCommandLineParser.Object.Json);
                                }

                                var jso = BuildJson(key);

                                try
                                {
                                    var outFile = Path.Combine(_fluentCommandLineParser.Object.Json,
                                        $"{StripInvalidCharsFromFileName(key.KeyName, "_")}.json");

                                    if (_fluentCommandLineParser.Object.JsonName.IsNullOrEmpty() == false)
                                    {
                                        outFile = Path.Combine(_fluentCommandLineParser.Object.Json,Path.GetFileName(_fluentCommandLineParser.Object.JsonName));
                                    }

                                    _logger.Warn($"Saving key to json file '{outFile}'\r\n");
                                    File.WriteAllText(outFile, jso.ToJson());
                                }
                                catch (Exception e)
                                {
                                    _logger.Error(
                                        $"Error saving key '{key.KeyPath}' to directory '{_fluentCommandLineParser.Object.Json}': {e.Message}");
                                }
                            }

                            if (_fluentCommandLineParser.Object.Detailed)
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

                                foreach (var sk in key.SubKeys)
                                {
                                    var skeyIsDeleted = (sk.KeyFlags & RegistryKey.KeyFlagsEnum.Deleted) ==
                                                        RegistryKey.KeyFlagsEnum.Deleted;
                                    if (skeyIsDeleted)
                                    {
                                        _logger.Fatal($"------------ Subkey #{i:N0} (DELETED) ------------");
                                        _logger.Fatal(
                                            $"Name: {sk.KeyName} (Last write: {sk.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}) Value count: {sk.Values.Count:N0}");
                                    }
                                    else
                                    {
                                        _logger.Info($"------------ Subkey #{i:N0} ------------");
                                        _logger.Info(
                                            $"Name: {sk.KeyName} (Last write: {sk.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}) Value count: {sk.Values.Count:N0}");
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
                    else if (_fluentCommandLineParser.Object.MinimumSize > 0)
                    {
                        searchUsed = true;
                        var hits = reg.FindByValueSize(_fluentCommandLineParser.Object.MinimumSize).ToList();

                        if (hits.Count > 0)
                        {
                            var suffix2 = hits.Count == 1 ? "" : "s";

                            if (hiveToProcess.StartsWith(VssDir))
                            {
                                _logger.Warn(
                                    $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {_fluentCommandLineParser.Object.MinimumSize:N0} bytes in 'VSS{hiveToProcess.Replace($"{VssDir}\\", "")}'");
                            }
                            else
                            {
                                _logger.Warn(
                                    $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {_fluentCommandLineParser.Object.MinimumSize:N0} bytes in '{hiveToProcess}'");
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
                    else if (_fluentCommandLineParser.Object.Base64 > 0)
                    {
                        searchUsed = true;
                        var hits = reg.FindBase64(_fluentCommandLineParser.Object.Base64).ToList();

                        if (hits.Count > 0)
                        {
                            var suffix2 = hits.Count == 1 ? "" : "s";

                            if (hiveToProcess.StartsWith(VssDir))
                            {
                                _logger.Warn(
                                    $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {_fluentCommandLineParser.Object.Base64:N0} bytes in 'VSS{hiveToProcess.Replace($"{VssDir}\\", "")}'");
                            }
                            else
                            {
                                _logger.Warn(
                                    $"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {_fluentCommandLineParser.Object.Base64:N0} bytes in '{hiveToProcess}'");
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
                    else if (_fluentCommandLineParser.Object.BatchName?.Length > 0) //batch mode
                    {
                        ProcessBatch(reBatch, reg);
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

            if (_fluentCommandLineParser.Object.BatchName.IsNullOrEmpty() == false)
            {
                _logger.Info("");

                var suffix2 = _batchCsvOutList.Count == 1 ? "" : "s";
                var suffix4 = hivesToProcess.Count == 1 ? "" : "s";

                _logger.Info(
                    $"Found {_batchCsvOutList.Count:N0} key/value pair{suffix2} across {hivesToProcess.Count:N0} file{suffix4}");
                _logger.Info($"Total search time: {totalSeconds:N3} seconds");
                if (_batchCsvOutList.Count > 0)
                {
                    if (Directory.Exists(_fluentCommandLineParser.Object.CsvDirectory) == false)
                    {
                        _logger.Warn(
                            $"Path to '{_fluentCommandLineParser.Object.CsvDirectory}' doesn't exist. Creating...");
                        Directory.CreateDirectory(_fluentCommandLineParser.Object.CsvDirectory);
                    }

                    var outName =
                        $"{RunTimestamp}_RECmd_Batch_{Path.GetFileNameWithoutExtension(_fluentCommandLineParser.Object.BatchName)}_Output.csv";

                    if (_fluentCommandLineParser.Object.CsvName.IsNullOrEmpty() == false)
                    {
                        outName = Path.GetFileName(_fluentCommandLineParser.Object.CsvName);
                    }

                    var outFile = Path.Combine(_fluentCommandLineParser.Object.CsvDirectory, outName);

                    _logger.Info($"\r\nSaving batch mode CSV file to '{outFile}'");

                    var swCsv = new StreamWriter(outFile, false, Encoding.UTF8);
                    var csvWriter = new CsvWriter(swCsv,CultureInfo.InvariantCulture);

                    var foo = csvWriter.Context.AutoMap<BatchCsvOut>();

                    foo.Map(t => t.LastWriteTimestamp).Convert(t =>
                        $"{t.Value.LastWriteTimestamp?.ToString(_fluentCommandLineParser.Object.DateTimeFormat)}");

                    csvWriter.Context.RegisterClassMap(foo);

                    csvWriter.WriteHeader<BatchCsvOut>();
                    csvWriter.NextRecord();

                    csvWriter.WriteRecords(_batchCsvOutList);

                    swCsv.Flush();
                    swCsv.Close();
                }
            }


            if (searchUsed && _fluentCommandLineParser.Object.Directory?.Length > 0)
            {
                _logger.Info("");

                var suffix2 = totalHits == 1 ? "" : "s";
                var suffix3 = hivesWithHits == 1 ? "" : "s";
                var suffix4 = hivesToProcess.Count == 1 ? "" : "s";
                var suffix5 = _fluentCommandLineParser.Object.Vss ? " (and VSCs)" : "";
                
                _logger.Info("---------------------------------------------");
                _logger.Info($"Directory: {_fluentCommandLineParser.Object.Directory}{suffix5}");
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

            if (_fluentCommandLineParser.Object.Vss)
            {
                if (Directory.Exists(VssDir))
                {
                    foreach (var directory in Directory.GetDirectories(VssDir))
                    {
                        Directory.Delete(directory);
                    }
                    Directory.Delete(VssDir,true,true);
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

            using (var client = new WebClient())
            {
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                client.DownloadFile("https://github.com/EricZimmerman/RECmd/archive/master.zip", archivePath);
            }

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

            var newMapPath = Path.Combine(BaseDirectory, "RECmd-master","BatchExamples");

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

        private static void ProcessBatchKey(RegistryKey key, Key batchKey, string hivePath)
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
                    BatchDumpKey(key, batchKey, hivePath);
                }
            }

            if (regVal == null && batchKey.ValueName.IsNullOrEmpty())
            {
                //do not need to find a value, 
                _logger.Trace($"Found key '{key.KeyPath}'!");
                BatchDumpKey(key, batchKey, hivePath);
            }


            if (!batchKey.Recursive)
            {
                return;
            }

            foreach (var regKeySubKey in regKey.SubKeys)
            {
                ProcessBatchKey(regKeySubKey, batchKey, hivePath);
            }
        }

        private static void ProcessBatch(ReBatch reBatch, RegistryHive regHive)
        {
            foreach (var key in reBatch.Keys)
            {
                if ((int) regHive.HiveType != (int) key.HiveType)
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

                    ProcessBatchKey(regKey, key, regHive.HivePath);
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

                        ///switch to this for better recursive support vs BatchDumpKey
                        ProcessBatchKey(regKey, key, regHive.HivePath);
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

                    ProcessBatchKey(regKey, key, regHive.HivePath);
                }
            }
        }

        private static bool CheckMinSwitches()
        {
            if (_fluentCommandLineParser.Object.SimpleSearchKey.IsNullOrEmpty() &&
                _fluentCommandLineParser.Object.SimpleSearchValue.IsNullOrEmpty() &&
                _fluentCommandLineParser.Object.SimpleSearchValueData.IsNullOrEmpty() &&
                _fluentCommandLineParser.Object.SimpleSearchValueSlack.IsNullOrEmpty() &&
                _fluentCommandLineParser.Object.SimpleSearchAll.IsNullOrEmpty() &&
                _fluentCommandLineParser.Object.KeyName.IsNullOrEmpty() &&
                _fluentCommandLineParser.Object.MinimumSize == 0 &&
                _fluentCommandLineParser.Object.Base64 == 0 &&
                _fluentCommandLineParser.Object.BatchName.IsNullOrEmpty())
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

                    var rPath = path.Replace("*", ".+?").Replace("\\","\\\\");

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

        private static void BatchDumpKey(RegistryKey regKey, Key key, string hivePath)
        {
            _logger.Trace($"Batch dumping '{regKey.KeyPath}' in '{hivePath}'");

            //1. dump all key values (valuename is null)
            //2. dump only value in a key (value name is specified)

            var pluginsToActivate = GetPluginsToActivate(regKey, key);

            if (key.DisablePlugin == false && pluginsToActivate.Count > 0)
            {
                foreach (var registryPluginBase in pluginsToActivate)
                {
                    var pig = (IRegistryPluginGrid) registryPluginBase;

                    pig.ProcessValues(regKey);

                    var pluginDetailsFile = DumpPluginValues(pig, hivePath);

                    var path = hivePath;

                    if (hivePath.StartsWith(VssDir))
                    {
                        path = $"VSS{hivePath.Replace($"{VssDir}\\", "")}'";
                    }

                    foreach (var pigValue in pig.Values)
                    {
                        var conv = (IValueOut) pigValue;

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
                        var rebOut = BuildBatchCsvOut(regKey, key, hivePath, regVal);

                        _batchCsvOutList.Add(rebOut);
                    }
                }
                else
                {
                    if (regKey.Values.Any() == false)
                    {
                        //dump the key itself since there are no values to pull in the times, etc.
                        var keyOut = BuildBatchCsvOut(regKey, key, hivePath, null);

                        _batchCsvOutList.Add(keyOut);
                    }
                    

                    //dump all values from current key
                    foreach (var regKeyValue in regKey.Values)
                    {
                        var rebOut = BuildBatchCsvOut(regKey, key, hivePath, regKeyValue);

                        _batchCsvOutList.Add(rebOut);
                    }
                }
            }
        }

        private static string DumpPluginValues(IRegistryPluginGrid plugin, string hivePath)
        {
            var pluginType = plugin.GetType();

            if (plugin.Values.Count == 0)
            {
                return null;
            }

            var dirName = _fluentCommandLineParser.Object.Directory;

            if (_fluentCommandLineParser.Object.HiveFile.IsNullOrEmpty() == false)
            {
                dirName = Path.GetDirectoryName(_fluentCommandLineParser.Object.HiveFile);
            }

            var hiveName1 = hivePath.Replace(dirName,"").Replace(":", "").Replace("\\", "_");

            if (hivePath.StartsWith(VssDir))
            {
                hiveName1 = $"VSS{hivePath.Replace($"{VssDir}\\","").Replace(":", "").Replace("\\", "_")}";
            }
            
            var outbase = $"{RunTimestamp}_{pluginType.Name}_{hiveName1}.csv";

            if (Directory.Exists(_fluentCommandLineParser.Object.CsvDirectory) == false)
            {
                _logger.Warn(
                    $"Path to '{_fluentCommandLineParser.Object.CsvDirectory}' doesn't exist. Creating...");
                Directory.CreateDirectory(_fluentCommandLineParser.Object.CsvDirectory);
            }

            if (_fluentCommandLineParser.Object.CsvName.IsNullOrEmpty() == false)
            {
                outbase =
                    $"{Path.GetFileNameWithoutExtension(_fluentCommandLineParser.Object.CsvName)}_{pluginType.Name}{Path.GetExtension(_fluentCommandLineParser.Object.CsvName)}";
            }

            var outFile = Path.Combine(_fluentCommandLineParser.Object.CsvDirectory, RunTimestamp,outbase);

            if (Directory.Exists(Path.GetDirectoryName(outFile)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outFile));
            }

            var exists = File.Exists(outFile);

            using (var sw = new StreamWriter(outFile, true,Encoding.UTF8))
            {
                var csvWriter = new CsvWriter(sw,CultureInfo.InvariantCulture);

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

        private static BatchCsvOut BuildBatchCsvOut(RegistryKey regKey, Key key, string hivePath, KeyValue regVal)
        {
            var path = hivePath;

            if (hivePath.StartsWith(VssDir))
            {
                path = $"VSS{hivePath.Replace($"{VssDir}\\", "")}'";
            }
            
            var rebOut = new BatchCsvOut
            {
                ValueName = regVal == null ? "": regVal.ValueName,
                Deleted = regKey.NkRecord.IsDeleted,
                Description = key.Description,
                Category = key.Category,
                Comment = key.Comment,
                HivePath = path,
                HiveType = key.HiveType.ToString(),
                KeyPath = regKey.KeyPath,
                LastWriteTimestamp = regKey.LastWriteTime.Value,
                Recursive = key.Recursive,
                ValueType = regVal == null ? "": regVal.ValueType
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
                                    var ft = DateTimeOffset.FromFileTime((long) BitConverter.ToUInt64(regVal.ValueDataRaw,  0));
                                    rebOut.ValueData = ft.ToUniversalTime()
                                        .ToString(_fluentCommandLineParser.Object.DateTimeFormat);
                                }
                                catch (Exception)
                                {
                                    _logger.Warn($"Error converting to FILETIME. Using bytes instead!");
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
                                    var ft = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(regVal.ValueDataRaw,  0));
                                    rebOut.ValueData = ft.ToUniversalTime()
                                        .ToString(_fluentCommandLineParser.Object.DateTimeFormat);
                                }
                                catch (Exception)
                                {
                                    _logger.Warn($"Error converting to Epoch. Using bytes instead!");
                                    rebOut.ValueData = regVal.ValueData;
                                }
                                break;
                            case Key.BinConvert.Sid:
                                try
                                {
                                    var sid = new SecurityIdentifier(regVal.ValueDataRaw, 0);

                                    rebOut.ValueData = sid.ToString();
                                }
                                catch (Exception)
                                {
                                    _logger.Warn($"Error converting to SID. Using bytes instead!");
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
                            var ft = DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(regVal.ValueDataRaw,  0));
                            rebOut.ValueData = ft.ToUniversalTime()
                                .ToString(_fluentCommandLineParser.Object.DateTimeFormat);
                        }
                        catch (Exception)
                        {
                            _logger.Warn($"Error converting to Epoch. Using bValueDataytes instead!");
                            rebOut.ValueData = regVal.ValueData;
                        }
                        break;

                    case Key.BinConvert.Filetime:
                        try
                        {
                            var ft = DateTimeOffset.FromFileTime((long) BitConverter.ToUInt64(regVal.ValueDataRaw,  0));
                            rebOut.ValueData = ft.ToUniversalTime()
                                .ToString(_fluentCommandLineParser.Object.DateTimeFormat);
                        }
                        catch (Exception)
                        {
                            _logger.Warn($"Error converting to FILETIME. Using bytes instead!");
                            rebOut.ValueData = regVal.ValueData;
                        }
                               
                        break;
                }
            }

            return rebOut;
        }

        private static ReBatch ValidateBatchFile()
        {
            var deserializer = new DeserializerBuilder()
                .Build();

            var hasError = false;

            ReBatch re = null;

            try
            {
                re = deserializer.Deserialize<ReBatch>(File.ReadAllText(_fluentCommandLineParser.Object.BatchName));
                var validator = new ReBatchValidator();

                var validate = validator.Validate(re);
                DisplayValidationResults(validate, _fluentCommandLineParser.Object.BatchName);
            }
            catch (SyntaxErrorException se)
            {
                _logger.Warn($"\r\nSyntax error in '{_fluentCommandLineParser.Object.BatchName}':");
                _logger.Fatal(se.Message);

                var lines = File.ReadLines(_fluentCommandLineParser.Object.BatchName).ToList();
                var fileContents = _fluentCommandLineParser.Object.BatchName.ReadAllText();

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
                _logger.Warn($"\r\nSyntax error in '{_fluentCommandLineParser.Object.BatchName}':");
                _logger.Fatal(ye.Message);

                _logger.Fatal(ye.InnerException?.Message);

                hasError = true;
            }

            catch (Exception e)
            {
                _logger.Warn($"\r\nError when validating '{_fluentCommandLineParser.Object.BatchName}'");
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
            var target = (ColoredConsoleTarget) LogManager.Configuration.FindTargetByName("console");
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
                        return dto.ToString(_fluentCommandLineParser.Object.DateTimeFormat);
                    case DateTime dt:
                        return dt.ToString(_fluentCommandLineParser.Object.DateTimeFormat);
                    default:
                        return base.ConvertToString(value, row, memberMapData);
                }
            }
        }
    }

    internal class ApplicationArguments
    {
        public string HiveFile { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public bool RecoverDeleted { get; set; } = false;
        public string KeyName { get; set; } = string.Empty;
        public string BatchName { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;

        public string SaveToName { get; set; } = string.Empty;

        public string DateTimeFormat { get; set; }

        public bool Detailed { get; set; } = false;

        public string SimpleSearchAll { get; set; } = string.Empty;
        public string SimpleSearchKey { get; set; } = string.Empty;

        public string SimpleSearchValue { get; set; } = string.Empty;
        public string SimpleSearchValueData { get; set; } = string.Empty;
        public string SimpleSearchValueSlack { get; set; } = string.Empty;
        public int MinimumSize { get; set; }
        public int Base64 { get; set; }
        public string Json { get; set; }

        public bool Vss { get; set; }
        public bool Dedupe { get; set; }

        public string EndDate { get; set; }
        public string CsvDirectory { get; set; }
        public string CsvName { get; set; }
        public string JsonName { get; set; }

        public bool RegEx { get; set; }
        public bool Literal { get; set; }
        public bool SuppressData { get; set; }
         public bool Sync { get; set; }
         public bool Quiet { get; set; }
        public bool NoTransLogs { get; set; }
        public bool DisablePlugins { get; set; }
        public bool Debug { get; set; }
        public bool Trace { get; set; }
    }
}