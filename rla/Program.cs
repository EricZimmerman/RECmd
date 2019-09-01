using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Exceptionless;
using Exceptionless.Extensions;
using Fclp;
using NLog;
using NLog.Config;
using NLog.Targets;
using RawCopy;
using Registry;
using ServiceStack;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

namespace rla
{
    class Program
    {
        private class ApplicationArguments
        {
            public string HiveFile { get; set; } = string.Empty;
            public string Directory { get; set; } = string.Empty;
            public string OutDirectory { get; set; } = string.Empty;

            public bool Debug { get; set; }
            public bool Trace { get; set; }
            public bool CopyAlways { get; set; }
            public bool CompressNames { get; set; }
        }

        private static Logger _logger;
        private static FluentCommandLineParser<ApplicationArguments> _fluentCommandLineParser;
        private static Stopwatch _sw;

        static void Main(string[] args)
        {
            Exceptionless.ExceptionlessClient.Default.Startup("fTcEOUkt1CxljTyOZfsr8AcSGQwWE4aYaYqk7cE1");
            SetupNLog();

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
                .WithDescription("Hive to process. -f or -d is required.\r\n");

            _fluentCommandLineParser.Setup(arg => arg.OutDirectory)
                .As("out")
                .WithDescription(
                    "Directory to save updated hives to. Only dirty hives with logs applied will end up in --out directory\r\n");

            _fluentCommandLineParser.Setup(arg => arg.CopyAlways)
                .As("ca")
                .WithDescription(
                    "When true, always copy hives to --out directory, even if they aren't dirty. Default is TRUE").SetDefault(true);

            _fluentCommandLineParser.Setup(arg => arg.CompressNames)
                .As("cn")
                .WithDescription(
                    "When true, compress names for profile based hives. Default is TRUE\r\n").SetDefault(true);
            
            _fluentCommandLineParser.Setup(arg => arg.Debug)
                .As("debug")
                .WithDescription("Show debug information during processing").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.Trace)
                .As("trace")
                .WithDescription("Show trace information during processing").SetDefault(false);

            var header =
                $"rla version {Assembly.GetExecutingAssembly().GetName().Version}" +
                "\r\n\r\nAuthor: Eric Zimmerman (saericzimmerman@gmail.com)" +
                "\r\nhttps://github.com/EricZimmerman/RECmd\r\n\r\nNote: Enclose all strings containing spaces (and all RegEx) with double quotes";

            var footer = @"Example: rla.exe --f ""C:\Temp\UsrClass 1.dat"" --out C:\temp" +
                         "\r\n\t " +
                         @"rla.exe --d ""D:\temp\"" --out c:\temp" + "\r\n";

            _fluentCommandLineParser.SetupHelp("?", "help").WithHeader(header)
                .Callback(text => _logger.Info(text + "\r\n" + footer));

            var result = _fluentCommandLineParser.Parse(args);

            if (_fluentCommandLineParser.Object.HiveFile.IsNullOrEmpty() == false ||
                _fluentCommandLineParser.Object.Directory.IsNullOrEmpty() == false)
            {
                if (_fluentCommandLineParser.Object.OutDirectory.IsNullOrEmpty())
                {
                    _fluentCommandLineParser.HelpOption.ShowHelp(_fluentCommandLineParser.Options);
                    Console.WriteLine();
                    _logger.Warn($"--out is required. Exiting");
                    Console.WriteLine();
                    return;
                }
            }

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

            var hivesToProcess = new List<string>();

            _logger.Info(header);
            _logger.Info("");
            _logger.Info($"Command line: {string.Join(" ", Environment.GetCommandLineArgs().Skip(1))}\r\n");

            if (_fluentCommandLineParser.Object.HiveFile?.Length > 0)
            {
                if (File.Exists(_fluentCommandLineParser.Object.HiveFile) == false)
                {
                    _logger.Error($"File '{_fluentCommandLineParser.Object.HiveFile}' does not exist.");
                    return;
                }

                hivesToProcess.Add(_fluentCommandLineParser.Object.HiveFile);

            } 
            else if (_fluentCommandLineParser.Object.Directory?.Length > 0)
            {
                if (Directory.Exists(_fluentCommandLineParser.Object.Directory) == false)
                {
                    _logger.Error($"Directory '{_fluentCommandLineParser.Object.Directory}' does not exist.");
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

                if (Directory.Exists(_fluentCommandLineParser.Object.OutDirectory) == false)
                {
                    _logger.Info($"Creating --out directory '{_fluentCommandLineParser.Object.OutDirectory}'...");
                    Directory.CreateDirectory(_fluentCommandLineParser.Object.OutDirectory);
                }
                else
                {
                    if (Directory.GetFiles(_fluentCommandLineParser.Object.OutDirectory).Length > 0 && _fluentCommandLineParser.Object.CompressNames)
                    {
                        _logger.Warn($"'{_fluentCommandLineParser.Object.OutDirectory}' contains files! This may cause --cn to revert back to uncompressed names. Ideally, '{_fluentCommandLineParser.Object.OutDirectory}' should be empty.");
                        Console.WriteLine();
                    }
                }

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
                catch (Exception ex)
                {
                    _logger.Fatal($"Could not access all files in '{_fluentCommandLineParser.Object.Directory}'! Error: {ex.Message}");
                    _logger.Error("");
                    _logger.Fatal("Rerun the program with Administrator privileges to try again\r\n");
                    //Environment.Exit(-1);
                }
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

            _sw = new Stopwatch();
            _sw.Start();

            foreach (var hiveToProcess in hivesToProcess)
            {
                _logger.Info("");

                byte[] updatedBytes = null;
                
                _logger.Info($"Processing hive '{hiveToProcess}'");

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
                        using (var fs = new FileStream(hiveToProcess,FileMode.Open,FileAccess.Read))
                        {
                            reg = new RegistryHive(fs.ReadFully(),hiveToProcess)
                            {
                            };

                        }
                    }
                    catch (IOException)
                    {
                        //file is in use

                        if (Helper.IsAdministrator() == false)
                        {
                            throw new UnauthorizedAccessException("Administrator privileges not found!");
                        }

                        _logger.Warn($"\t'{hiveToProcess}' is in use. Rerouting...\r\n");

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
                            if (_fluentCommandLineParser.Object.CopyAlways)
                            {
                                _logger.Info($"\tHive '{hiveToProcess}' is dirty, but no logs were found in the same directory. --ca is true. Copying...");
                                updatedBytes = File.ReadAllBytes(hiveToProcess);
                            }
                            else
                            {
                                _logger.Info($"\tHive '{hiveToProcess}' is dirty and no transaction logs were found in the same directory. --ca is false. Skipping...");
                                continue;    
                            }
                        }

                        if (updatedBytes == null)
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

                                updatedBytes = reg.ProcessTransactionLogs(lt);
                            }
                            else
                            {
                                updatedBytes = reg.ProcessTransactionLogs(logFiles.ToList());
                            }
                        }
                    }

                    if (updatedBytes == null)
                    {
                        if (_fluentCommandLineParser.Object.CopyAlways)
                        {
                            _logger.Info($"\tHive '{hiveToProcess}' is not dirty, but --ca is true. Copying...");
                            updatedBytes = File.ReadAllBytes(hiveToProcess);
                        }
                        else
                        {
                            _logger.Info($"\tHive '{hiveToProcess}' is not dirty and --ca is false. Skipping...");
                            continue;    
                        }
                    }

                    var outFile = hiveToProcess.Replace(":", "").Replace("\\", "_");
                    var outFileAll = Path.Combine(_fluentCommandLineParser.Object.OutDirectory, outFile);

                    if (_fluentCommandLineParser.Object.CompressNames &&
                        (outFileAll.ToUpperInvariant().Contains("NTUSER") || outFileAll.ToUpperInvariant().Contains("USRCLASS")))
                    {
                        var dl = hiveToProcess[0].ToString();
                        var segs = hiveToProcess.SplitAndTrim('\\');

                        var profile = segs[2];
                        var filename = Path.GetFileName(hiveToProcess);

                       var outFile2 = $"{dl}_{profile}_{filename}";

                        outFileAll = Path.Combine(_fluentCommandLineParser.Object.OutDirectory, outFile2);
                    }

                    if (File.Exists(outFileAll))
                    {
                        var oldOut = outFileAll;
                        
                        outFileAll = Path.Combine(_fluentCommandLineParser.Object.OutDirectory, outFile);

                        _logger.Warn($"\tFile '{oldOut}' exists! Saving as non-compressed name: '{outFileAll}'");
                    }

                    _logger.Fatal($"\tSaving updated hive to '{outFileAll}'");

                    using (var fs = new FileStream(outFileAll,FileMode.Create))
                    {
                        fs.Write(updatedBytes,0,updatedBytes.Length);

                        fs.Flush();

                        fs.Close();
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
            }

            _sw.Stop();
            _logger.Info("");

            _logger.Info($"Total processing time: {_sw.Elapsed.TotalSeconds:N3} seconds");
            _logger.Info("");

        }


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
    }
}
