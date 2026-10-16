# RECmd

## Ongoing Projects

 * [DFIR Batch File (Formally Kroll Batch)](https://github.com/EricZimmerman/RECmd/projects/1) - Development roadmap for the [DFIR Batch File](https://github.com/EricZimmerman/RECmd/blob/master/BatchExamples/DFIRBatch.reb). Please feel free to contribute by adding ideas or by finishing tasks in the `To Do` column. Any help is appreciated! 

## Command Line Interface

    Description:
      RECmd version 2026.10.0
      
      Author: Eric Zimmerman (saericzimmerman@gmail.com)
      https://github.com/EricZimmerman/RECmd
      
      Note: Enclose all strings containing spaces (and all RegEx) with double quotes
      
      Example: RECmd.exe -f "C:\Temp\UsrClass 1.dat" --sk URL --recover false --nl
             RECmd.exe -f "D:\temp\UsrClass 1.dat" --regex --sv "(App|Display)Name"
             RECmd.exe -d "D:\triage" --bn BatchExamples\DFIRBatch.reb --csv "D:\out"
      

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
      -?, -h, --help       Show help and usage information
      --version            Show version information

## Documentation

Command line Registry access, including batch mode!

See the manual for more examples.

If you get an error message like "error loading plugin" when running RECmd after downloading the ZIP archive and extracting it using Windows' ZIP tool, use the following PowerShell command to unblock the DLLs:

``` PowerShell
PS> Unblock-File .\Plugins\*.dll
```

Both tools return exit code 0 on success and 1 when the arguments are bad or a hive could not be processed (for example a dirty hive without its transaction logs, unless `--nl` is used).

## Batch Files

RECmd uses Batch Files to make your Registry output more actionable. Learn about Batch Files [here](https://github.com/EricZimmerman/RECmd/tree/master/BatchExamples#readme)!

As of May 2024, there is a README specifically for the DFIRBatch file used by RECmd and KAPE. Find it [here](https://github.com/EricZimmerman/RECmd/blob/master/BatchExamples/DFIRBatch.md)!

# RLA

## Command Line Interface

    Description:
      rla version 2026.10.0
      
      Author: Eric Zimmerman (saericzimmerman@gmail.com)
      https://github.com/EricZimmerman/RECmd
      
      Note: Enclose all strings containing spaces with double quotes
      
      Example: rla.exe -f "C:\Temp\UsrClass 1.dat" --out C:\temp
             rla.exe -d "D:\temp\" --out c:\temp
      

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
      -?, -h, --help  Show help and usage information
      --version       Show version information

## Documentation

RLA is a single purpose tool to replay transaction logs in Registry hives. This is useful when parsing with tools that don't recognize and replay transaction logs on their own.

By default (`--nop false`), each hive is saved under its original path in `--out`: `C:\Windows\System32\config\SAM` becomes `<out>\C\Windows\System32\config\SAM`. With `--nop true` hives are saved directly in `--out`; `--cn` then names profile hives `drive_profile_name` (`C_eric_NTUSER.DAT`), and a hive whose name is already taken gets a number (`SAM_2`) instead of overwriting it.

A hive is dirty when its sequence numbers differ, or when its header checksum is wrong and it has transaction logs (`.LOG`, `.LOG1`, `.LOG2`, in any case).

# Download Eric Zimmerman's Tools

All of Eric Zimmerman's tools can be downloaded [here](https://ericzimmerman.github.io/#!index.md). 

# Special Thanks

Open Source Development funding and support provided by the following contributors: 
- [SANS Institute](http://sans.org/) and [SANS DFIR](http://dfir.sans.org/).
- [Tines](https://www.tines.com/?utm_source=oss&utm_medium=sponsorship&utm_campaign=ericzimmerman)
