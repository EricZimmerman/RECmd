using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Alphaleonis.Win32.Filesystem;
using Exceptionless;
using Exceptionless.Extensions;
using NLog;
using NLog.Config;
using NLog.Targets;
using RawCopy;
using Registry;
using ServiceStack;
using Directory = System.IO.Directory;
using File = System.IO.File;
using FileInfo = System.IO.FileInfo;
using Path = System.IO.Path;

namespace rla;

internal class Program
{
    private static Logger _logger;
    private static Stopwatch _sw;

    private static RootCommand _rootCommand;

    private static readonly string Header =
        $"rla version {Assembly.GetExecutingAssembly().GetName().Version}" +
        "\r\n\r\nAuthor: Eric Zimmerman (saericzimmerman@gmail.com)" +
        "\r\nhttps://github.com/EricZimmerman/RECmd\r\n\r\nNote: Enclose all strings containing spaces with double quotes";

    private static readonly string Footer = @"Example: rla.exe --f ""C:\Temp\UsrClass 1.dat"" --out C:\temp" +
                                            "\r\n\t " +
                                            @"    rla.exe --d ""D:\temp\"" --out c:\temp" + "\r\n";


    private static async Task Main(string[] args)
    {
        ExceptionlessClient.Default.Startup("fTcEOUkt1CxljTyOZfsr8AcSGQwWE4aYaYqk7cE1");
        SetupNLog();

        _logger = LogManager.GetCurrentClassLogger();

        _rootCommand = new RootCommand
        {
            new Option<string>(
                "-d",
                "Directory to look for hives (recursively). -f or -d is required"),

            new Option<string>(
                "-f",
                "Hive to process. -f or -d is required"),

            new Option<bool>(
                "--out",
                () => false,
                "Directory to save updated hives to. Only dirty hives with logs applied will end up in --out directory"),

            new Option<bool>(
                "--ca",
                () => true,
                "When true, always copy hives to --out directory, even if they aren't dirty."),

            new Option<bool>(
                "--cn",
                () => true,
                "When true, compress names for profile based hives."),

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

    private static void DoWork(string d, string f, string @out, bool ca, bool cn, bool debug, bool trace)
    {
        if (f.IsNullOrEmpty() == false ||
            d.IsNullOrEmpty() == false)
        {
            if (@out.IsNullOrEmpty())
            {
                var helpBld = new HelpBuilder(LocalizationResources.Instance, Console.WindowWidth);
                var hc = new HelpContext(helpBld, _rootCommand, Console.Out);

                helpBld.Write(hc);

                Console.WriteLine();
                _logger.Warn("--out is required. Exiting");
                Console.WriteLine();
                return;
            }
        }

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


        var hivesToProcess = new List<string>();

        _logger.Info(Header);
        _logger.Info("");
        _logger.Info($"Command line: {string.Join(" ", Environment.GetCommandLineArgs().Skip(1))}\r\n");

        if (f?.Length > 0)
        {
            if (File.Exists(f) == false)
            {
                _logger.Error($"File '{f}' does not exist.");
                return;
            }

            hivesToProcess.Add(f);
        }
        else if (d?.Length > 0)
        {
            if (Directory.Exists(d) == false)
            {
                _logger.Error($"Directory '{d}' does not exist.");
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

            var directoryEnumerationFilters = new DirectoryEnumerationFilters();
            directoryEnumerationFilters.InclusionFilter = fsei =>
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
                    }

                    return false;
                }
            };

            directoryEnumerationFilters.RecursionFilter = entryInfo => !entryInfo.IsMountPoint && !entryInfo.IsSymbolicLink;

            directoryEnumerationFilters.ErrorFilter = (errorCode, errorMessage, pathProcessed) => true;

            var dirEnumOptions =
                DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive |
                DirectoryEnumerationOptions.SkipReparsePoints | DirectoryEnumerationOptions.ContinueOnException |
                DirectoryEnumerationOptions.BasicSearch;

            if (Directory.Exists(@out) == false)
            {
                _logger.Info($"Creating --out directory '{@out}'...");
                Directory.CreateDirectory(@out);
            }
            else
            {
                if (Directory.GetFiles(@out).Length > 0 && cn)
                {
                    _logger.Warn($"'{@out}' contains files! This may cause --cn to revert back to uncompressed names. Ideally, '{@out}' should be empty.");
                    Console.WriteLine();
                }
            }

            _logger.Fatal($"Searching '{d}' for hives...");

            var files2 =
                Alphaleonis.Win32.Filesystem.Directory.EnumerateFileSystemEntries(d, dirEnumOptions, directoryEnumerationFilters);

            var count = 0;

            try
            {
                hivesToProcess.AddRange(files2);
                count = hivesToProcess.Count;

                _logger.Info($"\tHives found: {count:N0}");
            }
            catch (Exception ex)
            {
                _logger.Fatal($"Could not access all files in '{d}'! Error: {ex.Message}");
                _logger.Error("");
                _logger.Fatal("Rerun the program with Administrator privileges to try again\r\n");
                //Environment.Exit(-1);
            }
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
                    using (var fs = new FileStream(hiveToProcess, FileMode.Open, FileAccess.Read))
                    {
                        reg = new RegistryHive(fs.ReadFully(), hiveToProcess);
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

                    rawFiles = Helper.GetRawFiles(files);

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
                        if (ca)
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
                    if (ca)
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
                var outFileAll = Path.Combine(@out, outFile);

                if (cn &&
                    (outFileAll.ToUpperInvariant().Contains("NTUSER") || outFileAll.ToUpperInvariant().Contains("USRCLASS")))
                {
                    var dl = hiveToProcess[0].ToString();
                    var segs = hiveToProcess.SplitAndTrim('\\');

                    var profile = segs[2];
                    var filename = Path.GetFileName(hiveToProcess);

                    var outFile2 = $"{dl}_{profile}_{filename}";

                    outFileAll = Path.Combine(@out, outFile2);
                }

                if (File.Exists(outFileAll))
                {
                    var oldOut = outFileAll;

                    outFileAll = Path.Combine(@out, outFile);

                    _logger.Warn($"\tFile '{oldOut}' exists! Saving as non-compressed name: '{outFileAll}'");
                }

                _logger.Fatal($"\tSaving updated hive to '{outFileAll}'");

                using (var fs = new FileStream(outFileAll, FileMode.Create))
                {
                    fs.Write(updatedBytes, 0, updatedBytes.Length);

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