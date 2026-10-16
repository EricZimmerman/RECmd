using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Exceptionless;
using RawCopy;
using Registry;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ServiceStack;
#if !NET9_0_OR_GREATER
using System.Text;
using Alphaleonis.Win32.Filesystem;
using Directory = Alphaleonis.Win32.Filesystem.Directory;
using File = Alphaleonis.Win32.Filesystem.File;
using FileInfo = Alphaleonis.Win32.Filesystem.FileInfo;
using Path = Alphaleonis.Win32.Filesystem.Path;

#else
using Directory = System.IO.Directory;
using File = System.IO.File;
using FileInfo = System.IO.FileInfo;
using Path = System.IO.Path;
#endif

namespace rla;

internal class Program
{
    private static Stopwatch _sw;

    private static RootCommand _rootCommand;

    private static readonly string Header =
        $"rla version {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)}" +
        "\r\n\r\nAuthor: Eric Zimmerman (saericzimmerman@gmail.com)" +
        "\r\nhttps://github.com/EricZimmerman/RECmd\r\n\r\nNote: Enclose all strings containing spaces with double quotes";

    private static readonly string Footer = @"Example: rla.exe -f ""C:\Temp\UsrClass 1.dat"" --out C:\temp" +
                                            "\r\n\t " +
                                            @"  rla.exe -d ""D:\temp\"" --out c:\temp" + "\r\n";

    private static async Task<int> Main(string[] args)
    {
        ExceptionlessClient.Default.Startup("fTcEOUkt1CxljTyOZfsr8AcSGQwWE4aYaYqk7cE1");

        var fOpt = new Option<string>("-f")
        {
            Description = "File to process. Either this or -d is required"
        };
        var dOpt = new Option<string>("-d")
        {
            Description = "Directory to recursively process. Either this or -f is required"
        };
        var outOpt = new Option<string>("--out")
        {
            Description = "Directory to save hives to, dirty ones with their transaction logs applied (see --ca)"
        };
        var caOpt = new Option<bool>("--ca")
        {
            Description = "When true, always copy hives to --out directory, even if they aren't dirty",
            DefaultValueFactory = _ => true
        };
        var cnOpt = new Option<bool>("--cn")
        {
            Description = "When true and --nop is true, name profile based hives (NTUSER.DAT, UsrClass.dat) drive_profile_name, so they don't collide",
            DefaultValueFactory = _ => true
        };
        var nopOpt = new Option<bool>("--nop")
        {
            Description = "When true, save hives directly in --out. When false, save them under their original path in --out (C:\\Windows\\System32\\config\\SAM as <out>\\C\\Windows\\System32\\config\\SAM)",
            DefaultValueFactory = _ => false
        };
        var debugOpt = new Option<bool>("--debug")
        {
            Description = "Show debug information during processing",
            DefaultValueFactory = _ => false
        };
        var traceOpt = new Option<bool>("--trace")
        {
            Description = "Show trace information during processing",
            DefaultValueFactory = _ => false
        };
        
        _rootCommand = new RootCommand
        {
           fOpt,
           dOpt,
           outOpt,
           caOpt,
           cnOpt,
           nopOpt,
           debugOpt,
           traceOpt
        };

        _rootCommand.Description = Header + "\r\n\r\n" + Footer;

        _rootCommand.SetAction(result => DoWork(result.GetValue(fOpt), result.GetValue(dOpt), result.GetValue(outOpt),
            result.GetValue(caOpt), result.GetValue(cnOpt), result.GetValue(nopOpt), result.GetValue(debugOpt),
            result.GetValue(traceOpt)));
            
        var exitCode = await _rootCommand.Parse(args).InvokeAsync();

        Log.CloseAndFlush();

        return exitCode;
    }

    /// <summary>The hive's transaction logs (.LOG, .LOG1, .LOG2, in any case) next to it that aren't empty</summary>
    private static List<string> LogFiles(string dirname, string hiveBase)
    {
        var logs = new List<string>();
        foreach (var file in Directory.GetFiles(dirname))
        {
            var name = Path.GetFileName(file);
            if (name.StartsWith(hiveBase + ".LOG", StringComparison.OrdinalIgnoreCase) == false || name.Length > hiveBase.Length + 5)
            {
                continue;
            }

            //.LOG, .LOG1, .LOG2
            var suffix = name.Substring(hiveBase.Length + 4);
            if (suffix is "" or "1" or "2" && new FileInfo(file).Length > 0)
            {
                logs.Add(file);
            }
        }

        return logs.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).ToList();
    }

#if NET9_0_OR_GREATER
    static IEnumerable<string> FindFiles(string directory, IEnumerable<string> masks, HashSet<string> ignoreMasks, EnumerationOptions options,long minimumSize = 0)
    {
        foreach (var file in masks.AsParallel().SelectMany(searchPattern => Directory.EnumerateFiles(directory, searchPattern, options)))
        {
            var fi = new FileInfo(file);
            if (fi.Length < minimumSize)
            {
                Log.Debug("Skipping {File} with size {Length:N0}",file,fi.Length);
                continue;
            }

            var ext = Path.GetExtension(file);
            if (ignoreMasks.Contains(ext))
            {
                Log.Debug("Skipping {File} since its extension ({Ext}) is in ignoreMasks",file,ext);
                continue;
            }
        
            yield return file;
        }
    }
#endif

    /// <returns>0, or 1 when the arguments are bad or a hive could not be processed</returns>
    private static int DoWork(string f, string d, string @out, bool ca, bool cn, bool nop, bool debug, bool trace)
    {
        var levelSwitch = new LoggingLevelSwitch();

        var template = "{Message:lj}{NewLine}{Exception}";

        if (debug)
        {
            levelSwitch.MinimumLevel = LogEventLevel.Debug;
            template = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        }

        if (trace)
        {
            levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            template = "[{Timestamp:HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}";
        }

        var conf = new LoggerConfiguration()
            .WriteTo.Console(outputTemplate: template)
            .MinimumLevel.ControlledBy(levelSwitch);

        Log.Logger = conf.CreateLogger();

        if (f.IsNullOrEmpty() == false ||
            d.IsNullOrEmpty() == false)
        {
            if (@out.IsNullOrEmpty())
            {
                var aaa = new CustomHelpAction(new HelpAction());
                aaa.Invoke(_rootCommand.Parse("--out is required. Exiting"));
                
                Console.WriteLine();
                return 1;
            }
        }

        var hivesToProcess = new List<string>();

        Console.WriteLine();
        Log.Information("{Header}", Header);
        Console.WriteLine();

        Log.Information("Command line: {Args}", string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
        Console.WriteLine();

        if (f?.Length > 0)
        {
            if (File.Exists(f) == false)
            {
                Log.Error("File {F} does not exist", f);
                Console.WriteLine();
                return 1;
            }

            hivesToProcess.Add(f);
        }
        else if (d?.Length > 0)
        {
            if (Directory.Exists(d) == false)
            {
                Log.Error("Directory {D} does not exist", d);
                Console.WriteLine();
                return 1;
            }

            IEnumerable<string> files;

#if !NET9_0_OR_GREATER
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
            okFileParts.Add("DEFAULT");
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
                    using var fs = new FileStream(fsei.FullPath, FileMode.Open, FileAccess.Read);
                    using var br = new BinaryReader(fs, new ASCIIEncoding());
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
                Log.Information("Creating --out directory {Out}...", @out);
                Directory.CreateDirectory(@out);
            }
            else
            {
                if (Directory.GetFiles(@out).Length > 0 && cn)
                {
                    Log.Warning("{Out} contains files! This may cause {Switch} to revert back to uncompressed names. Ideally, {Out2} should be empty", @out, "--cn", @out);
                    Console.WriteLine();
                }
            }

            Log.Information("Searching {D} for hives...", d);

            files =
                Directory.EnumerateFileSystemEntries(d, dirEnumOptions, directoryEnumerationFilters);

#elif NET9_0_OR_GREATER
            var enumerationOptions = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive,
                RecurseSubdirectories = true,
                AttributesToSkip = 0
            };
            
            var mask = new List<string>
            {
                "USRCLASS.DAT",
                "NTUSER.DAT",
                "SYSTEM",
                "SAM",
                "SOFTWARE",
                "AMCACHE.HVE",
                "SYSCACHE.hve",
                "SECURITY",
                "DRIVERS",
                "DEFAULT",
                "COMPONENTS"
            };
            var ignoreExt = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".dll",
                ".LOG",
                ".LOG1",
                ".LOG2",
                ".csv",
                ".blf",
                ".regtrans-ms",
                ".exe",
                ".txt",
                ".ini"
            };

            files = FindFiles(d, mask, ignoreExt, enumerationOptions, 4);


#endif
            var count = 0;

            try
            {
                hivesToProcess.AddRange(files);
                count = hivesToProcess.Count;

                Log.Information("\tHives found: {Count:N0}", count);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Could not access all files in {D}! Error: {Message}", d, ex.Message);
                Console.WriteLine();
                Log.Fatal("Rerun the program with Administrator privileges to try again");
                Console.WriteLine();
                //Environment.Exit(-1);
            }
        }
        else
        {
            var aaa = new CustomHelpAction(new HelpAction());
            aaa.Invoke(_rootCommand.Parse(""));

            return 1;
        }


        if (hivesToProcess.Count == 0)
        {
            Log.Warning("No hives were found. Exiting...");

            return 0;
        }

        _sw = new Stopwatch();
        _sw.Start();

        var outputNames = new OutputNames();
        var failed = 0;

        foreach (var hiveToProcess in hivesToProcess)
        {
            Console.WriteLine();

            byte[] updatedBytes = null;

            Log.Information("Processing hive {HiveToProcess}", hiveToProcess);

            if (File.Exists(hiveToProcess) == false)
            {
                Log.Warning("{HiveToProcess} does not exist. Skipping", hiveToProcess);
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
                    using var fs = new FileStream(hiveToProcess, FileMode.Open, FileAccess.Read);
                    reg = new RegistryHive(fs.ReadFully(), hiveToProcess);
                }
                catch (IOException)
                {
                    //file is in use

                    if (Helper.IsAdministrator() == false)
                    {
                        throw new UnauthorizedAccessException("Administrator privileges not found!");
                    }

                    Log.Warning("\t{HiveToProcess} is in use. Rerouting...", hiveToProcess);
                    Console.WriteLine();

                    var files = new List<string>();
                    files.Add(hiveToProcess);

                    files.AddRange(LogFiles(string.IsNullOrEmpty(dirname) ? "." : dirname, hiveBase));

                    rawFiles = Helper.GetRawFiles(files);

                    if (rawFiles.First().FileStream.Length == 0)
                    {
                        continue;
                    }

                    var bb = rawFiles.First().FileStream.ReadFully();

                    reg = new RegistryHive(bb, rawFiles.First().InputFilename);
                }

                if (string.IsNullOrEmpty(dirname))
                {
                    dirname = ".";
                }

                //dirty: the sequence numbers differ, or the header's checksum is wrong and there are logs to take it from (as Windows does)
                var logFiles = LogFiles(dirname, hiveBase);
                var hasLogs = logFiles.Count > 0 || rawFiles?.Count > 1;
                var dirty = reg.Header.PrimarySequenceNumber != reg.Header.SecondarySequenceNumber || reg.Header.ValidateCheckSum() == false && hasLogs;

                if (dirty)
                {

                    if (logFiles.Count == 0 && (rawFiles == null || rawFiles.Count < 2))
                    {
                        if (ca)
                        {
                            Log.Information("\tHive {HiveToProcess} is dirty, but no logs were found in the same directory. {Switch} is true. Copying...", hiveToProcess, "--ca");
                            updatedBytes = rawFiles != null ? reg.FileBytes : File.ReadAllBytes(hiveToProcess);
                        }
                        else
                        {
                            Log.Information("\tHive {HiveToProcess} is dirty and no transaction logs were found in the same directory. {Switch} is false. Skipping...", hiveToProcess, "--ca");
                            continue;
                        }
                    }

                    if (updatedBytes == null)
                    {
                        try
                        {
                            if (rawFiles != null)
                            {
                                var lt = new List<TransactionLogFileInfo>();
                                foreach (var rawCopyReturn in rawFiles.Skip(1).ToList())
                                {
                                    var bb1 = rawCopyReturn.FileStream.ReadFully();

                                    if (bb1.Length > 0)
                                    {
                                        lt.Add(new TransactionLogFileInfo(rawCopyReturn.InputFilename, bb1));
                                    }
                                }

                                updatedBytes = reg.ProcessTransactionLogs(lt);
                            }
                            else
                            {
                                updatedBytes = reg.ProcessTransactionLogs(logFiles);
                            }
                        }
                        catch (Exception ex)
                        {
                            //it used to be skipped without a word
                            Log.Error("\tTransaction logs for {HiveToProcess} could not be applied: {Message}", hiveToProcess, ex.Message);
                            failed++;
                            continue;
                        }
                    }
                }

                if (updatedBytes == null)
                {
                    if (ca)
                    {
                        Log.Information("\tHive {HiveToProcess} is not dirty, but {Switch} is {Val}. Copying...", hiveToProcess, "--ca", true);
                        updatedBytes = rawFiles != null ? reg.FileBytes : File.ReadAllBytes(hiveToProcess);
                    }
                    else
                    {
                        Log.Information("\tHive {HiveToProcess} is not dirty and {Switch} is {Val}. Skipping...", hiveToProcess, "--ca", false);
                        continue;
                    }
                }

                //the path under --out (#112), with a number when another hive in this run got the same name
                var outFileAll = outputNames.Get(hiveToProcess, @out, nop, cn);

                Log.Information("\tSaving updated hive to {OutFileAll}", outFileAll);

                if (Directory.Exists(Path.GetDirectoryName(outFileAll)) == false)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(outFileAll));
                }

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
                        Log.Fatal("Could not access {HiveToProcess} because it is in use", hiveToProcess);
                        Console.WriteLine();
                        Log.Fatal("Rerun the program with Administrator privileges to try again");
                        Console.WriteLine();
                    }
                    else if (ex.Message.Contains("Data in byte array is not a Registry hive") && f.IsNullOrEmpty())
                    {
                        //a file in -d with a hive's name that isn't one (a script named system, say)
                        Log.Debug("\tSkipping {HiveToProcess}: {Message}", hiveToProcess, ex.Message);
                        continue;
                    }
                    else
                    {
                        Log.Error("\tThere was an error processing {HiveToProcess}: {Message}", hiveToProcess, ex.Message);
                    }
                }
                else
                {
                    Log.Error("\t{HiveToProcess}: {Message}", hiveToProcess, ex.Message);
                }

                failed++;
            }
        }

        _sw.Stop();
        Console.WriteLine();

        Log.Information("Total processing time: {TotalSeconds:N3} seconds", _sw.Elapsed.TotalSeconds);
        Console.WriteLine();

        if (failed > 0)
        {
            Log.Warning("{Failed:N0} hive(s) could not be processed", failed);
            Console.WriteLine();
        }

        return failed > 0 ? 1 : 0;
    }
    
    private class CustomHelpAction : SynchronousCommandLineAction
    {
        private readonly HelpAction _defaultHelp;

        public CustomHelpAction(HelpAction action)
        {
            _defaultHelp = action;
        }

        public override int Invoke(ParseResult parseResult)
        {
            var result = _defaultHelp.Invoke(parseResult);

            Log.Warning("{Msg}", string.Join(" ",parseResult.Tokens));

            return result;
        }
    }
}