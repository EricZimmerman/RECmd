using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Exceptionless;
using Exceptionless.Extensions;
using RawCopy;
using Registry;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using ServiceStack;

#if NET462
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
        $"rla version {Assembly.GetExecutingAssembly().GetName().Version}" +
        "\r\n\r\nAuthor: Eric Zimmerman (saericzimmerman@gmail.com)" +
        "\r\nhttps://github.com/EricZimmerman/RECmd\r\n\r\nNote: Enclose all strings containing spaces with double quotes";

    private static readonly string Footer = @"Example: rla.exe -f ""C:\Temp\UsrClass 1.dat"" --out C:\temp" +
                                            "\r\n\t " +
                                            @"  rla.exe -d ""D:\temp\"" --out c:\temp" + "\r\n";

    private static async Task Main(string[] args)
    {
        ExceptionlessClient.Default.Startup("fTcEOUkt1CxljTyOZfsr8AcSGQwWE4aYaYqk7cE1");

        _rootCommand = new RootCommand
        {
            
            new Option<string>(
                "-f",
                "Hive to process. -f or -d is required"),
            new Option<string>(
                "-d",
                "Directory to look for hives (recursively). -f or -d is required"),

            new Option<string>(
                "--out",
                
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
        
        Log.CloseAndFlush();
    }

    #if NET6_0
    
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

    private static void DoWork(string f, string d, string @out, bool ca, bool cn, bool debug, bool trace)
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
                var helpBld = new HelpBuilder(LocalizationResources.Instance, Console.WindowWidth);
                var hc = new HelpContext(helpBld, _rootCommand, Console.Out);

                helpBld.Write(hc);

                Console.WriteLine();
                Log.Warning("--out is required. Exiting");
                Console.WriteLine();
                return;
            }
        }

        var hivesToProcess = new List<string>();

        Console.WriteLine();
        Log.Information("{Header}",Header);
        Console.WriteLine();
        
        Log.Information("Command line: {Args}",string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
        Console.WriteLine();

        if (f?.Length > 0)
        {
            if (File.Exists(f) == false)
            {
                Log.Error("File {F} does not exist",f);
                Console.WriteLine();
                return;
            }

            hivesToProcess.Add(f);
        }
        else if (d?.Length > 0)
        {
           
            
            if (Directory.Exists(d) == false)
            {
                Log.Error("Directory {D} does not exist",d);
                Console.WriteLine();
                return;
            }
           
            IEnumerable<string> files;

#if NET462
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
                Log.Information("Creating --out directory {Out}...",@out);
                Directory.CreateDirectory(@out);
            }
            else
            {
                if (Directory.GetFiles(@out).Length > 0 && cn)
                {
                    Log.Warning("{Out} contains files! This may cause {Switch} to revert back to uncompressed names. Ideally, {Out2} should be empty",@out,"--cn",@out);
                    Console.WriteLine();
                }
            }

            Log.Information("Searching {D} for hives...",d);
            
            files =
                Alphaleonis.Win32.Filesystem.Directory.EnumerateFileSystemEntries(d, dirEnumOptions, directoryEnumerationFilters);

#elif NET6_0
          
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

                Log.Information("\tHives found: {Count:N0}",count);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex,"Could not access all files in {D}! Error: {Message}",d,ex.Message);
                Console.WriteLine();
                Log.Fatal("Rerun the program with Administrator privileges to try again");
                Console.WriteLine();
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
            Log.Warning("No hives were found. Exiting...");

            return;
        }

        _sw = new Stopwatch();
        _sw.Start();

        foreach (var hiveToProcess in hivesToProcess)
        {
            Console.WriteLine();

            byte[] updatedBytes = null;

            Log.Information("Processing hive {HiveToProcess}",hiveToProcess);

            if (File.Exists(hiveToProcess) == false)
            {
                Log.Warning("{HiveToProcess} does not exist. Skipping",hiveToProcess);
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

                    Log.Warning("\t{HiveToProcess} is in use. Rerouting...",hiveToProcess);
                    Console.WriteLine();

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
                            Log.Information("\tHive {HiveToProcess} is dirty, but no logs were found in the same directory. {Switch} is true. Copying...",hiveToProcess,"--ca");
                            updatedBytes = File.ReadAllBytes(hiveToProcess);
                        }
                        else
                        {
                            Log.Information("\tHive {HiveToProcess} is dirty and no transaction logs were found in the same directory. {Switch} is false. Skipping...",hivesToProcess,"--cn");
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
                        Log.Information("\tHive {HiveToProcess} is not dirty, but {Switch} is {Val}. Copying...",hiveToProcess,"--ca",true);
                        updatedBytes = File.ReadAllBytes(hiveToProcess);
                    }
                    else
                    {
                        Log.Information("\tHive {HiveToProcess} is not dirty and {Switch} is {Val}. Skipping...",hiveToProcess,"--ca",false);
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

                    Log.Warning("\tFile {OldOut} exists! Saving as non-compressed name: {OutFileAll}",oldOut,outFileAll);
                }

                Log.Information("\tSaving updated hive to {OutFileAll}",outFileAll);

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
                        Log.Fatal("Could not access {HiveToProcess} because it is in use",hiveToProcess);
                        Console.WriteLine();
                        Log.Fatal("Rerun the program with Administrator privileges to try again");
                        Console.WriteLine();
                    }
                    else if (ex.Message.Contains("Data in byte array is not a Registry hive") || ex.Message.Contains("No logs were supplied"))
                    {
                        //it already gets reported
                        //Log.Error("\t{Message}",ex.Message);
                    }
                    else
                    {
                        Log.Error(ex,"There was an error: {Message}",ex.Message);
                    }
                }
            }
        }

        _sw.Stop();
        Console.WriteLine();

        Log.Information("Total processing time: {TotalSeconds:N3} seconds",_sw.Elapsed.TotalSeconds);
        Console.WriteLine();
    }

   
}