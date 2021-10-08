# RECmd

## Ongoing Projects

 * [Kroll Batch File](https://github.com/EricZimmerman/RECmd/projects/1) - Development roadmap for the [Kroll Batch File](https://github.com/EricZimmerman/RECmd/blob/master/BatchExamples/Kroll_Batch.reb). Please feel free to contribute by adding ideas or by finishing tasks in the `To Do` column. Any help is appreciated! 

## Command Line Interface

    RECmd version 1.6.0.0
    
    Author: Eric Zimmerman (saericzimmerman@gmail.com)
    https://github.com/EricZimmerman/RECmd
    
    Note: Enclose all strings containing spaces (and all RegEx) with double quotes

            d               Directory to look for hives (recursively). -f or -d is required.
            f               Hive to search. -f or -d is required.

            q               Quiet mode. When true, hide processing details. Default is FALSE

            kn              Display details for key name. Includes subkeys and values
            vn              Value name. Only this value will be dumped
            bn              Use settings from supplied file to find keys/values. See included sample file for examples
            csv             Directory to save CSV formatted results to. Required when -bn is used.
            csvf            File name to save CSV formatted results to. When present, overrides default name
            saveTo          Saves --vn value data in binary form to file. Expects path to a FILE
            json            Export --kn to directory specified by --json. Ignored when --vn is specified
            jsonf           File name to save JSON formatted results to. When present, overrides default name

            details         Show more details when displaying results. Default is FALSE

            Base64          Find Base64 encoded values with size >= Base64 (specified in bytes)
            MinSize         Find values with data size >= MinSize (specified in bytes)

            sa              Search for <string> in keys, values, data, and slack.
            sk              Search for <string> in key names.
            sv              Search for <string> in value names
            sd              Search for <string> in value record's value data
            ss              Search for <string> in value record's value slack
            literal         If true, --sd and --ss search value will not be interpreted as ASCII or Unicode byte strings
            nd              If true, do not show data when using --sd or --ss. Default is FALSE
            regex           If present, treat <string> in --sk, --sv, --sd, and --ss as a regular expression. Default is FALSE

            dt              The custom date/time format to use when displaying time stamps. Default is: yyyy-MM-dd HH:mm:ss.fffffff
            nl              When true, ignore transaction log files for dirty hives. Default is FALSE
            recover         If true, recover deleted keys/values. Default is TRUE

            vss             Process all Volume Shadow Copies that exist on drive specified by -f or -d . Default is FALSE
            dedupe          Deduplicate -f or -d & VSCs based on SHA-1. First file found wins. Default is TRUE

            sync            If true, the latest batch files from https://github.com/EricZimmerman/RECmd/tree/master/BatchExamples are downloaded and local files updated. Default is FALSE

            debug           Show debug information during processing
            trace           Show trace information during processing

    Example: RECmd.exe --f "C:\Temp\UsrClass 1.dat" --sk URL --recover false --nl
             RECmd.exe --f "D:\temp\UsrClass 1.dat" --StartDate "11/13/2014 15:35:01"
             RECmd.exe --f "D:\temp\UsrClass 1.dat" --RegEx --sv "(App|Display)Name"

## Documentation

Command line Registry access, including batch mode!

See the manual for more examples.

If you get an error message like "error loading plugin" when running RECmd after downloading the ZIP archive and extracting it using Windows' ZIP tool, use the following PowerShell command to unblock the DLLs:

``` PowerShell
PS> Unblock-File .\Plugins\*.dll
```

## Batch Files

RECmd uses Batch Files to make your Registry output more actionable. Learn about Batch Files [here](https://github.com/EricZimmerman/RECmd/tree/master/BatchExamples#readme)!

As of September 2021, there is a README specifically for the Kroll_Batch file used by RECmd and KAPE. Find it [here](https://github.com/EricZimmerman/RECmd/blob/master/BatchExamples/Kroll_Batch.md)!

# RLA

## Command Line Interface

    rla version 1.6.0.0
    
    Author: Eric Zimmerman (saericzimmerman@gmail.com)
    https://github.com/EricZimmerman/RECmd
    
    Note: Enclose all strings containing spaces (and all RegEx) with double quotes

            d               Directory to look for hives (recursively). -f or -d is required.
            f               Hive to process. -f or -d is required.

            out             Directory to save updated hives to. Only dirty hives with logs applied will end up in --out directory

            ca              When true, always copy hives to --out directory, even if they aren't dirty. Default is TRUE
            cn              When true, compress names for profile based hives. Default is TRUE
    
            debug           Show debug information during processing
            trace           Show trace information during processing
    
    Example: rla.exe --f "C:\Temp\UsrClass 1.dat" --out C:\temp
             rla.exe --d "D:\temp\" --out c:\temp

## Documentation

RLA is a single purpose tool to replay transaction logs in Registry hives. This is useful when parsing with tools that don't recognize and replay transaction logs on their own.

# Download Eric Zimmerman's Tools

All of Eric Zimmerman's tools can be downloaded [here](https://ericzimmerman.github.io/#!index.md). Use the [Get-ZimmermanTools](https://f001.backblazeb2.com/file/EricZimmermanTools/Get-ZimmermanTools.zip) PowerShell script to automate the download and updating of the EZ Tools suite. Additionally, you can automate each of these tools using [KAPE](https://www.kroll.com/en/services/cyber-risk/incident-response-litigation-support/kroll-artifact-parser-extractor-kape)!

# Special Thanks

Open Source Development funding and support provided by the following contributors: [SANS Institute](http://sans.org/) and [SANS DFIR](http://dfir.sans.org/).
