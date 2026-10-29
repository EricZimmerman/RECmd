# RECmd and rla

RECmd is command line Registry access: it searches hives for keys, values, data and slack, dumps keys and values, and in
batch mode uses a batch file (`.reb`) and the Registry plugins to pull the useful keys and values out of many hives into
one CSV or JSON file. rla (Registry log applier) replays transaction logs into dirty hives and saves them, for tools that
don't apply transaction logs themselves.

| | |
|---|---|
| **Version** | RECmd 2026.10.3 &middot; rla 2026.10.1 |
| **Download** | RECmd: [.NET 10](https://download.ericzimmermanstools.com/net/RECmd.zip) (recommended) &middot; [.NET Framework 4.7.2](https://download.ericzimmermanstools.com/RECmd.zip)<br>rla: [.NET 10](https://download.ericzimmermanstools.com/net/rla.zip) (recommended) &middot; [.NET Framework 4.7.2](https://download.ericzimmermanstools.com/rla.zip) |
| **All tools** | [ericzimmerman.github.io](https://ericzimmerman.github.io) or [Get-ZimmermanTools](https://download.ericzimmermanstools.com/Get-ZimmermanTools.zip), which downloads them all and keeps them current |
| **Changes** | [ChangeLog](https://ericzimmerman.github.io/changelog/) |
| **Issues** | [EricZimmerman/RECmd](https://github.com/EricZimmerman/RECmd/issues) or [EricZimmerman/Issues](https://github.com/EricZimmerman/Issues/issues) |

## Requirements

- **.NET 10 build**: the [.NET 10 runtime](https://dotnet.microsoft.com/download/dotnet/10.0). Run `RECmd.exe` or `rla.exe`
  on Windows, or `dotnet RECmd.dll` or `dotnet rla.dll` on any system with the runtime (`--vss` and reading hives that
  are in use need Windows).
- **.NET Framework build**: .NET Framework 4.7.2 or later (Windows only).

## Command line

```text
Usage:
  RECmd [options]

Options:
  -f <f>               Hive to search. -f or -d is required
  -d <d>               Directory to look for hives (recursively). -f or -d is required
  --kn <kn>            Display details for key name. Includes subkeys and values
  --vn <vn>            Value name. Only this value will be dumped
  --bn <bn>            Use settings from supplied file to find keys/values. See included sample file for examples
  --csv <csv>          Directory to save CSV formatted results to. Be sure to include the full path in double quotes
  --csvf <csvf>        File name to save CSV formatted results to. When present, overrides default name
  --saveTo <saveTo>    Saves --vn value data in binary form to file. Expects path to a FILE
  --json <json>        Directory to save JSON formatted results to. Be sure to include the full path in double quotes
  --jsonf <jsonf>      File name to save JSON formatted results to. When present, overrides default name
  --details            Show more details when displaying results
  --base64 <base64>    Find Base64 encoded values with size >= Base64 (specified in bytes)
  --minSize <minSize>  Find values with data size >= MinSize (specified in bytes)
  --sa <sa>            Search for <string> in keys, values, data, and slack
  --sk <sk>            Search for <string> in value record's key names
  --sv <sv>            Search for <string> in value record's value names
  --sd <sd>            Search for <string> in value record's value data
  --ss <ss>            Search for <string> in value record's value slack
  --literal            If true, --sd and --ss search value will not be interpreted as ASCII or Unicode byte strings
  --nd                 If true, do not show data when using --sd or --ss
  --regex              If present, treat <string> in --sk, --sv, --sd, and --ss as a regular expression
  --dt <dt>            The custom date/time format to use when displaying time stamps. See https://goo.gl/CNVq0k for options [default: yyyy-MM-dd HH:mm:ss.fffffff]
  --nl                 When true, allow transaction log files to not exist for dirty hives
  --recover            If true, recover deleted keys/values. Default is true
  --vss                Process all Volume Shadow Copies that exist on drive specified by -f or -d
  --dedupe             Deduplicate -f or -d & VSCs based on SHA-1. First file found wins
  --sync               If true, the latest batch files from https://github.com/EricZimmerman/RECmd/tree/master/BatchExamples are downloaded and local files updated
  --debug              Show debug information during processing
  --trace              Show trace information during processing
  --co                 When true, save all console messages to <timestamp>_RECmd_<version>_ConsoleLog.txt in the output directory (the first of --csv, --json given, else the folder of --saveTo). Use --co false to turn it off
  -?, -h, --help       Show help and usage information
  --version            Show version information
```

Short options (a single letter) start with one dash, long options with two. Enclose all strings containing spaces (and
all regular expressions) in double quotes.

## Examples

```text
RECmd.exe -f "C:\Temp\UsrClass 1.dat" --sk URL --recover false --nl
RECmd.exe -f "D:\temp\UsrClass 1.dat" --regex --sv "(App|Display)Name"
RECmd.exe -d "D:\triage" --bn BatchExamples\DFIRBatch.reb --csv "D:\out"
```

## Output

- **Console**: the search hits (`--sa`, `--sk`, `--sv`, `--sd`, `--ss`, `--base64`, `--minSize`), or the key with its
  subkeys and values (`--kn`; with `--vn` only that value; `--details` shows more).
- **Batch mode** (`--bn` with `--csv` or `--json`, one of them): one file for all hives,
  `<timestamp>_RECmd_Batch_<batch file name>_Output.csv` (or `.json`), with the columns `HivePath`, `HiveType`,
  `Description`, `Category`, `KeyPath`, `ValueName`, `ValueType`, `ValueData`, `ValueData2`, `ValueData3`, `Comment`,
  `Recursive`, `Deleted`, `LastWriteTimestamp` and `PluginDetailFile`. Keys a Registry plugin knows get a row with
  `(plugin)` as the value type, and the plugin's full output is written to a `<timestamp>` folder in the output folder
  (`<timestamp>_<plugin>_<hive>.csv` or `.json`), named in `PluginDetailFile`.
- **JSON** (`--kn` with `--json`): the key, its subkeys and values in `<key name>.json`. Time stamps in JSON are ISO 8601
  UTC (`2024-01-02T03:04:05.6780000Z`).
- **Binary** (`--vn` with `--saveTo`): the value's data, as it is, in that file.

Deleted keys and values are recovered unless `--recover false` is given. A dirty hive (its sequence numbers differ, or its
header checksum is wrong and it has logs) gets its transaction logs (`.LOG`, `.LOG1` and `.LOG2` next to it, in any case)
replayed; without them it is an error unless `--nl` is given. A hive that is in use is read raw (Windows, administrator),
with its logs.

## Plugins

The Registry plugins (`RegistryPlugin.*.dll` in the `Plugins` folder next to RECmd, from the
[RegistryPlugins](https://github.com/EricZimmerman/RegistryPlugins) repository) parse the values they know in batch mode.

If you get an error message like "error loading plugin" when running RECmd after downloading the ZIP archive and
extracting it using Windows' ZIP tool, use the following PowerShell command to unblock the DLLs:

```powershell
PS> Unblock-File .\Plugins\*.dll
```

## Batch files

RECmd uses batch files to make your Registry output more actionable: a batch file lists the keys and values to report.
The ones in [BatchExamples](BatchExamples) come with RECmd and are maintained mostly by the community; `--sync` downloads
the latest ones from this repository and updates the local files. Learn about batch files
[here](BatchExamples/README.md), and about the DFIR batch file (`DFIRBatch.reb`) used by RECmd and KAPE
[here](BatchExamples/DFIRBatch.md).

## rla

### Command line

```text
Usage:
  rla [options]

Options:
  -f <f>          File to process. Either this or -d is required
  -d <d>          Directory to recursively process. Either this or -f is required
  --out <out>     Directory to save hives to, dirty ones with their transaction logs applied (see --ca)
  --ca            When true, always copy hives to --out directory, even if they aren't dirty
  --cn            When true and --nop is true, name profile based hives (NTUSER.DAT, UsrClass.dat) drive_profile_name, so they don't collide
  --nop           When true, save hives directly in --out. When false, save them under their original path in --out (C:\Windows\System32\config\SAM as <out>\C\Windows\System32\config\SAM)
  --debug         Show debug information during processing
  --trace         Show trace information during processing
  --co            When true, save all console messages to <timestamp>_rla_<version>_ConsoleLog.txt in the --out directory. Use --co false to turn it off
  -?, -h, --help  Show help and usage information
  --version       Show version information
```

### Examples

```text
rla.exe -f "C:\Temp\UsrClass 1.dat" --out C:\temp
rla.exe -d "D:\temp\" --out c:\temp
```

### Output

Dirty hives are saved to `--out` with their transaction logs applied (with `--ca`, clean hives are copied there too). A
hive is dirty when its sequence numbers differ, or when its header checksum is wrong and it has transaction logs (`.LOG`,
`.LOG1`, `.LOG2`, in any case).

By default (`--nop false`), each hive is saved under its original path in `--out`: `C:\Windows\System32\config\SAM`
becomes `<out>\C\Windows\System32\config\SAM`. With `--nop true` hives are saved directly in `--out`; `--cn` then names
profile hives `drive_profile_name` (`C_eric_NTUSER.DAT`), and a hive whose name is already taken gets a number (`SAM_2`)
instead of overwriting it.

## Exit codes

| Code | Meaning |
|---|---|
| `0` | Success. Damaged hives are reported (as could not be processed) but are not a failure, nor is nothing to do (no hives found, `--kn` or `--vn` not found) |
| `1` | The tool failed: invalid arguments or batch file, a missing `-f` or `-d` path, a hive or its logs that can't be read, a dirty hive without its transaction logs (RECmd without `--nl`, rla with `--ca false`), output that can't be written, or (RECmd batch mode) a plugin that throws or can't be loaded. Folders under `-d` that can't be read are reported (`Unable to read folder <path>: <reason>`) and the rest is still processed |

## What's new in 2026.10

- RECmd 2026.10.3: RegistryPluginBase 2026.10.7 (ExtensionBlocks 2026.10.9) and the current plugins: FTP shares in OpenSavePidlMRU and LastVisitedPidlMRU (Issues #261), Office 2010 File/Place MRUs, Acrobat Reader 5.0-11.0 recent files and Windows 7 TaskCache tasks are listed (they were errors), and one bad value no longer loses the rest of its key in any plugin.
- RECmd 2026.10.2, rla 2026.10.1: `--co` (on by default; `--co false` turns it off) saves everything written to the console to
  `<timestamp>_<tool>_<version>_ConsoleLog.txt` in the output directory (Issues #259). RECmd: with `--debug` or `--trace`
  each message shows its time (`HH:mm:ss.fff`) instead of the full date.
- RECmd 2026.10.1: the LastVisitedPidlMRU, OpenSavePidlMRU, RecentDocs and Taskband plugins read 0x2E root folder items with extension blocks after the GUID (a known folder such as Downloads) are read, and zip contents with data in bytes 4-9 (RegistryPluginBase 2026.10.6, ExtensionBlocks 2026.10.8).
- Hives load about 2x faster with less memory (Registry 2026.10.0). Over the test hives, DFIRBatch finds about 9% more
  rows (logs found and replayed in full, damaged hives read, more deleted keys).
- Transaction logs are applied as Windows applies them; old format (`.LOG`) logs are used, log files are found whatever
  their case, and empty ones are skipped.
- RECmd: `--recover false` is honored, and hives in use read raw recover deleted keys too; `--vss` works again and
  removes only its own links from `C:\___vssMount`; JSON time stamps are ISO 8601 UTC.
- RECmd plugins: shell items in OpenSavePidlMRU, LastVisitedPidlMRU and Pinned Taskbar Items are read as ShellBags
  Explorer reads them; new 7-Zip copy history, folder history and extraction path history plugins.
- rla: `--nop false` (the default) saves each hive under its original path again, and hives with the same name no longer
  overwrite each other with `--nop true` (#112); hives in use are copied from the raw read.
- Exit code 1 when the tool fails (it was always 0). Folders under `-d` that can't be read are reported instead of
  skipped silently. Error reports include source file and line numbers.

The full list is in the [ChangeLog](https://ericzimmerman.github.io/changelog/).

## Building

```text
dotnet build RECmd.slnx -c Release
```

Builds RECmd and rla for `net10.0` and `net472`, and the tests. Hive parsing is in the
[Registry](https://github.com/EricZimmerman/Registry) library ([NuGet](https://www.nuget.org/packages/Registry)), and the
plugins are in [RegistryPlugins](https://github.com/EricZimmerman/RegistryPlugins).

## Special thanks

Open source development funding and support provided by the [SANS Institute](https://www.sans.org/) and
[SANS DFIR](https://www.sans.org/digital-forensics-incident-response/).

## License

[MIT](LICENSE.md), Copyright (c) Eric R. Zimmerman 2027.
