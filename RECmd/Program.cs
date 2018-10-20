using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Alphaleonis.Win32.Filesystem;
using Fclp;
using NLog;
using NLog.Config;
using NLog.Targets;
using Registry;
using Registry.Abstractions;
using Registry.Cells;
using Registry.Other;
using ServiceStack;
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

        private static void SetupNLog()
        {
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

        private static void Main(string[] args)
        {
            //TODO Live Registry support
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
                .WithDescription("Hive to search. -f or -d is required.\r\n");

            _fluentCommandLineParser.Setup(arg => arg.KeyName)
                .As("kn")
                .WithDescription(
                    "Key name. All values under this key will be dumped");

            _fluentCommandLineParser.Setup(arg => arg.ValueName)
                .As("vn")
                .WithDescription(
                    "Value name. Only this value will be dumped");

            _fluentCommandLineParser.Setup(arg => arg.SaveToName)
                .As("saveTo")
                .WithDescription("Saves --vn value data in binary form to file. Expects path to a FILE");

//            _fluentCommandLineParser.Setup(arg => arg.Recursive)
//                .As("recurse")
//                .WithDescription(
//                    "Display keys/values recursively (ignored if --vn used). Default is FALSE").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.Detailed)
                .As("details")
                .WithDescription(
                    "Show more details when displaying results. Default is FALSE\r\n").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg. Base64)
                .As("Base64")
                .WithDescription("Find Base64 encoded values with size >= Base64 (specified in bytes)");
            _fluentCommandLineParser.Setup(arg => arg.MinimumSize)
                .As("MinSize")
                .WithDescription("Find values with data size >= MinSize (specified in bytes)\r\n");

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


            _fluentCommandLineParser.Setup(arg => arg.NoTransLogs)
                .As("nl")
                .WithDescription(
                    "When true, ignore transaction log files for dirty hives. Default is FALSE").SetDefault(false);

            _fluentCommandLineParser.Setup(arg => arg.RecoverDeleted)
                .As("recover")
                .WithDescription("If true, recover deleted keys/values. Default is TRUE").SetDefault(true);

            var header =
                $"RECmd version {Assembly.GetExecutingAssembly().GetName().Version}" +
                "\r\n\r\nAuthor: Eric Zimmerman (saericzimmerman@gmail.com)" +
                "\r\nhttps://github.com/EricZimmerman/RECmd\r\n\r\nNote: Enclose all strings containing spaces (and all RegEx) with double quotes";

            var footer = @"Example: RECmd.exe --f ""C:\Temp\UsrClass 1.dat"" --sk URL --recover" + "\r\n\t " +
                         @"RECmd.exe --f ""D:\temp\UsrClass 1.dat"" --StartDate ""11/13/2014 15:35:01"" " +
                         "\r\n\t " +
                         @"RECmd.exe --f ""D:\temp\UsrClass 1.dat"" --RegEx --sv ""(App|Display)Name"" " + "\r\n";

            _fluentCommandLineParser.SetupHelp("?", "help").WithHeader(header)
                .Callback(text => _logger.Info(text + "\r\n" + footer));

            var result = _fluentCommandLineParser.Parse(args);

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


                var f = new DirectoryEnumerationFilters();
                f.InclusionFilter = fsei =>
                {
                    if (fsei.Extension.ToUpperInvariant() == ".LOG1" || fsei.Extension.ToUpperInvariant() == ".LOG2" ||
                        fsei.Extension.ToUpperInvariant() == ".DLL" ||
                        fsei.Extension.ToUpperInvariant() == ".TXT" || fsei.Extension.ToUpperInvariant() == ".INI")
                    {
                        return false;
                    }

                    var fi = new FileInfo(fsei.FullPath);

                    if (fi.Length < 4)
                    {
                        return false;
                    }

                    using (var fs = new FileStream(fsei.FullPath, FileMode.Open, FileAccess.Read))
                    {
                        using (var br = new BinaryReader(fs, new ASCIIEncoding()))
                        {
                            try
                            {
                                var chunk = br.ReadBytes(4);

                                var sig = BitConverter.ToInt32(chunk, 0);

                                if (sig != 0x66676572)
                                {
                                    return false;
                                }
                            }
                            catch
                            {
                                return false;
                            }
                        }
                    }

                    return true;
                };

                f.RecursionFilter = entryInfo => !entryInfo.IsMountPoint && !entryInfo.IsSymbolicLink;

                f.ErrorFilter = (errorCode, errorMessage, pathProcessed) => true;

                var dirEnumOptions =
                    DirectoryEnumerationOptions.Files | DirectoryEnumerationOptions.Recursive |
                    DirectoryEnumerationOptions.SkipReparsePoints | DirectoryEnumerationOptions.ContinueOnException |
                    DirectoryEnumerationOptions.BasicSearch;

                var files2 =
                    Directory.EnumerateFileSystemEntries(_fluentCommandLineParser.Object.Directory, dirEnumOptions, f);

                hivesToProcess.AddRange(files2);
            }
            else
            {
                _fluentCommandLineParser.HelpOption.ShowHelp(_fluentCommandLineParser.Options);
                return;
            }

            _logger.Info(header);
            _logger.Info("");

            if (hivesToProcess.Count == 0)
            {
                _logger.Warn("No hives were found. Exiting...");

                return;
            }


            var totalHits = 0;
            var hivesWithHits = 0;
            double totalSeconds = 0;
            var searchUsed = false;

            foreach (var hiveToProcess in hivesToProcess)
            {
                _logger.Info("");

                _logger.Info($"Processing hive '{hiveToProcess}'");

                _logger.Info("");

                if (File.Exists(hiveToProcess) == false)
                {
                    _logger.Warn($"'{hiveToProcess}' does not exist. Skipping");
                    continue;
                }

                try
                {
                    var reg = new RegistryHive(hiveToProcess)
                    {
                        RecoverDeleted = _fluentCommandLineParser.Object.RecoverDeleted
                    };

                    _sw = new Stopwatch();
                    _sw.Start();

                    if (reg.Header.PrimarySequenceNumber != reg.Header.SecondarySequenceNumber)
                    {
                        var hiveBase = Path.GetFileName(hiveToProcess);

                        var dirname = Path.GetDirectoryName(hiveToProcess);

                        if (string.IsNullOrEmpty(dirname))
                        {
                            dirname = ".";
                        }

                        var logFiles = Directory.GetFiles(dirname, $"{hiveBase}.LOG?");

                        if (logFiles.Length == 0)
                        {
                            var log = LogManager.GetCurrentClassLogger();

                            if (_fluentCommandLineParser.Object.NoTransLogs == false)
                            {
                                log.Warn(
                                    "Registry hive is dirty and no transaction logs were found in the same directory! LOGs should have same base name as the hive. Aborting!!");
                                throw new Exception(
                                    "Sequence numbers do not match and transaction logs were not found in the same directory as the hive. Aborting");
                            }

                            log.Warn(
                                "Registry hive is dirty and no transaction logs were found in the same directory. Data may be missing! Continuing anyways...");
                        }
                        else
                        {
                            reg.ProcessTransactionLogs(logFiles.ToList(), true);
                        }
                    }

                    reg.ParseHive();

                    _logger.Info("");

                    //hive is ready for searching/interaction

                    if (_fluentCommandLineParser.Object.SimpleSearchKey.Length > 0 ||
                        _fluentCommandLineParser.Object.SimpleSearchValue.Length > 0 ||
                        _fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0 ||
                        _fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
                    {
                        searchUsed = true;

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
                            _logger.Fatal($"Found {hits.Count:N0} search hit{suffix2} in '{hiveToProcess}'");

                            hivesWithHits += 1;
                            totalHits += hits.Count;
                        }
                        else
                        {
                            _logger.Info("Nothing found");
                        }

                        var words = new HashSet<string>();
                        foreach (var searchHit in hits)
                        {
                            if (_fluentCommandLineParser.Object.SimpleSearchKey.Length > 0)
                            {
                                words.Add(_fluentCommandLineParser.Object.SimpleSearchKey);
                            }
                            else if (_fluentCommandLineParser.Object.SimpleSearchValue.Length > 0)
                            {
                                words.Add(_fluentCommandLineParser.Object.SimpleSearchValue);
                            }
                            else if (_fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0)
                            {
                                if (_fluentCommandLineParser.Object.RegEx)
                                {
                                    words.Add(_fluentCommandLineParser.Object.SimpleSearchValueData);
                                }
                                else
                                {
                                    if (searchHit.Value.VkRecord.DataType == VkCellRecord.DataTypeEnum.RegBinary)
                                    {
                                        words.Add(searchHit.HitString);
                                    }
                                    else
                                    {
                                        words.Add(_fluentCommandLineParser.Object.SimpleSearchValueData);
                                    }
                                }
                            }
                            else if (_fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
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

                            if (_fluentCommandLineParser.Object.SimpleSearchValueData.Length > 0 ||
                                _fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
                            {
                                if (_fluentCommandLineParser.Object.SuppressData)
                                {
                                    _logger.Info(
                                        $"Key: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}'");
                                }
                                else
                                {
                                    if (_fluentCommandLineParser.Object.SimpleSearchValueSlack.Length > 0)
                                    {
                                        _logger.Info(
                                            $"Key: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}', Slack: '{searchHit.Value.ValueSlack}'");
                                    }
                                    else
                                    {
                                        _logger.Info(
                                            $"Key: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}', Data: '{searchHit.Value.ValueData}'");
                                    }
                                }
                            }
                            else if (_fluentCommandLineParser.Object.SimpleSearchKey.Length > 0)
                            {
                                _logger.Info($"Key: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}'");
                            }
                            else if (_fluentCommandLineParser.Object.SimpleSearchValue.Length > 0)
                            {
                                _logger.Info(
                                    $"Key: '{Helpers.StripRootKeyNameFromKeyPath(searchHit.Key.KeyPath)}', Value: '{searchHit.Value.ValueName}'");
                            }
                        }

                        var target = (ColoredConsoleTarget) LogManager.Configuration.FindTargetByName("console");
                        target.WordHighlightingRules.Clear();

//                    //TODO search deleted?? should only need to look in reg.UnassociatedRegistryValues
                    } //End s* options

                    else if (_fluentCommandLineParser.Object.KeyName.IsNullOrEmpty() == false)
                    {
                        //dumping key and/or values
                        searchUsed = true;
                     
                        var key = reg.GetKey(_fluentCommandLineParser.Object.KeyName);
                        KeyValue val = null;

                        if (key == null)
                        {
                            _logger.Warn($"Key '{_fluentCommandLineParser.Object.KeyName}' not found.");
                            
                            continue;
                        }

                        if (_fluentCommandLineParser.Object.ValueName.Length > 0)
                        {
                             val = key.Values.SingleOrDefault(c => c.ValueName == _fluentCommandLineParser.Object.ValueName);

                            if (val == null)
                            {
                                _logger.Warn($"Value '{_fluentCommandLineParser.Object.ValueName}' not found for key '{_fluentCommandLineParser.Object.KeyName}'.");
                              
                                continue;
                            }

                            if (_fluentCommandLineParser.Object.SaveToName.Length > 0)
                            {
                                var baseDir = Path.GetDirectoryName(_fluentCommandLineParser.Object.SaveToName);
                                if (Directory.Exists(baseDir) == false)
                                {
                                    Directory.CreateDirectory(baseDir);
                                }

                                _logger.Warn($"Saving contents of '{val.ValueName}' to '{_fluentCommandLineParser.Object.SaveToName}\r\n'");
                                try
                                {
                                    File.WriteAllBytes(_fluentCommandLineParser.Object.SaveToName, val.ValueDataRaw);
                                }
                                catch (Exception ex)
                                {
                                   _logger.Error($"Save failed to '{_fluentCommandLineParser.Object.SaveToName}'. Error: {ex.Message}");
                                }
                            }
                        }

                        //dump key here
                        if (_fluentCommandLineParser.Object.ValueName.IsNullOrEmpty())
                        {
                            //key info only
                            _logger.Warn($"Key path: '{Helpers.StripRootKeyNameFromKeyPath(key.KeyPath)}'");
                            _logger.Info($"Last write time: {key.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff}");
                            _logger.Info($"Subkey count: {key.SubKeys.Count:N0}");
                            _logger.Info($"Values count: {key.Values.Count:N0}");

                            var i = 0;

                            foreach (var sk in key.SubKeys)
                            {
                                _logger.Info($"------------ Subkey #{i:N0} ------------");
                                _logger.Info($"Name: {sk.KeyName} (Last write: {sk.LastWriteTime.Value:yyyy-MM-dd HH:mm:ss.ffffff})");
                                i += 1;
                            }

                            i = 0;
                            _logger.Info("");

                            foreach (var keyValue in key.Values)
                            {
                                _logger.Info($"------------ Value #{i:N0} ------------");
                                _logger.Info($"Name: {keyValue.ValueName} ({keyValue.ValueType})");

                                var slack = "";

                                if (keyValue.ValueSlack.Length > 0)
                                {
                                    slack = $"(Slack: {keyValue.ValueSlack})";
                                }

                                _logger.Info($"Data: {keyValue.ValueData} {slack}");

                                i += 1;
                            }
                            
                        }
                        else
                        {
                            //value only
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
                        _logger.Info("");

                    } //end kn options
                    else if (_fluentCommandLineParser.Object.MinimumSize > 0)
                    {
                        searchUsed = true;
                        var hits = reg.FindByValueSize(_fluentCommandLineParser.Object.MinimumSize).ToList();
                       
                        if (hits.Count > 0)
                        {
                            var suffix2 = hits.Count == 1 ? "" : "s";
                            _logger.Fatal($"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {_fluentCommandLineParser.Object.MinimumSize:N0} bytes in '{hiveToProcess}'");

                            hivesWithHits += 1;
                            totalHits += hits.Count;
                        }
                        else
                        {
                            _logger.Info("Nothing found");
                        }

                        foreach (var valueBySizeInfo in hits)
                        {
                            _logger.Info(
                                $"Key: {Helpers.StripRootKeyNameFromKeyPath(valueBySizeInfo.Key.KeyPath)}, Value: {valueBySizeInfo.Value.ValueName}, Size: {valueBySizeInfo.Value.ValueDataRaw.Length:N0}");
                        }

                        _logger.Info("");
                    }//end min size option
                    else if (_fluentCommandLineParser.Object.Base64 > 0)
                    {
                        searchUsed = true;
                        var hits = reg.FindBase64(_fluentCommandLineParser.Object.Base64).ToList();
                       
                        if (hits.Count > 0)
                        {
                            var suffix2 = hits.Count == 1 ? "" : "s";
                            _logger.Fatal($"Found {hits.Count:N0} search hit{suffix2} with size greater or equal to {_fluentCommandLineParser.Object.Base64:N0} bytes in '{hiveToProcess}'");

                            hivesWithHits += 1;
                            totalHits += hits.Count;
                        }
                        else
                        {
                            _logger.Info("Nothing found");
                        }

                        foreach (var valueBySizeInfo in hits)
                        {
                            _logger.Info(
                                $"Key: {Helpers.StripRootKeyNameFromKeyPath(valueBySizeInfo.Key.KeyPath)}, Value: {valueBySizeInfo.Value.ValueName}, Size: {valueBySizeInfo.Value.ValueDataRaw.Length:N0}");
                        }

                        _logger.Info("");
                    }//end min size option

                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("Sequence numbers do not match and transaction") == false)
                    {
                        _logger.Error($"There was an error: {ex.Message}");
                    }
                }
            }

            _sw.Stop();
            totalSeconds += _sw.Elapsed.TotalSeconds;

            if (searchUsed && _fluentCommandLineParser.Object.Directory?.Length > 0)
            {
                _logger.Info("");

                var suffix2 = totalHits == 1 ? "" : "s";
                var suffix3 = hivesWithHits == 1 ? "" : "s";
                var suffix4 = hivesToProcess.Count == 1 ? "" : "s";

                _logger.Info("---------------------------------------------");
                _logger.Info($"Directory: {_fluentCommandLineParser.Object.Directory}");
                _logger.Info(
                    $"Found {totalHits:N0} hit{suffix2} in {hivesWithHits:N0} hive{suffix3} out of {hivesToProcess.Count:N0} file{suffix4}");
                _logger.Info($"Total search time: {totalSeconds:N3} seconds");
                _logger.Info("");
            }
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

        private static void DumpRootKeyName(RegistryHive reg)
        {
            _logger.Info($"Root key name: {reg.Root.KeyName}");
            _logger.Info("");
        }

        private static void DumpKey(RegistryKey key, bool recursive)
        {
            if (recursive)
            {
                _logger.Info(key);
            }
            else
            {
                _logger.Info($"Key: {Helpers.StripRootKeyNameFromKeyPath(key.KeyPath)}");
                _logger.Info($"Last write time: {key.LastWriteTime}");
                _logger.Info($"Number of Values: {key.Values.Count:N0}");
                _logger.Info($"Number of Subkeys: {key.SubKeys.Count:N0}");
                _logger.Info("");

                var i = 0;

                foreach (var sk in key.SubKeys)
                {
                    _logger.Info($"------------ Subkey #{i:N0} ------------");
                    _logger.Info($"Name: {sk.KeyName} (Last write: {sk.LastWriteTime})");
                    i += 1;
                }

                i = 0;
                _logger.Info("");

                foreach (var keyValue in key.Values)
                {
                    _logger.Info($"------------ Value #{i:N0} ------------");
                    _logger.Info($"Name: {keyValue.ValueName} ({keyValue.ValueType})");

                    var slack = "";

                    if (keyValue.ValueSlack.Length > 0)
                    {
                        slack = $"(Slack: {keyValue.ValueSlack})";
                    }

                    _logger.Info($"Data: {keyValue.ValueData} {slack}");

                    i += 1;
                }
            }
        }

        private static void DumpStopWatchInfo()
        {
            _sw.Stop();
            _logger.Info("");
            _logger.Info($"Search took {_sw.Elapsed.TotalSeconds:N3} seconds");
        }
    }

    internal class ApplicationArguments
    {
        public string HiveFile { get; set; } = string.Empty;
        public string Directory { get; set; } = string.Empty;
        public bool RecoverDeleted { get; set; } = false;
        public string KeyName { get; set; } = string.Empty;
        public string ValueName { get; set; } = string.Empty;
        public string SaveToName { get; set; } = string.Empty;
  //      public bool Recursive { get; set; } = false;
        public bool Detailed { get; set; } = false;

        public string SimpleSearchKey { get; set; } = string.Empty;

//        public string DumpKey { get; set; } = string.Empty;
//        public string DumpDir { get; set; } = string.Empty;
        public string SimpleSearchValue { get; set; } = string.Empty;
        public string SimpleSearchValueData { get; set; } = string.Empty;
        public string SimpleSearchValueSlack { get; set; } = string.Empty;
        public int MinimumSize { get; set; }
        public int Base64 { get; set; }
        public string StartDate { get; set; }

        public string EndDate { get; set; }

        //     public bool Sort { get; set; }
        public bool RegEx { get; set; }
        public bool Literal { get; set; }
        public bool SuppressData { get; set; }

        public bool NoTransLogs { get; set; }
//        public bool Quiet { get; set; }
//        public bool VeryQuiet { get; set; }
    }
}