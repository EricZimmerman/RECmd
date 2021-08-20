
# RECmd Batch Files

RECmd uses Batch Files (`.reb` file extension) as a means to filter out potentially irrelevant information from the Windows Registry. There is an incredible amount of data stored within the Windows Registry, but much of it is not human readable or useful to an examiner. Batch Files attempt to provide the most high fidelity information and present them in an easy to digest format. 

As of 2021, the [Kroll Batch File](https://github.com/EricZimmerman/RECmd/blob/master/BatchExamples/Kroll_Batch.reb) is the most frequently maintained Batch File. It serves as the default Registry output for KAPE's [!EZParser](https://github.com/EricZimmerman/KapeFiles/blob/master/Modules/!EZParser.mkape) Module. This Batch File has been curated to take advantage of most, if not all, available [Registry Plugins](https://github.com/EricZimmerman/RegistryPlugins).

## Disclaimer

Using a Batch File when parsing with RECmd means you are **NOT** seeing everything that resides within a respective Registry hive. You are only seeing what the author of the respective Batch File specified for RECmd to parse from a Registry hive, so long as the data exists. While this is incredibly useful for reducing the noise within the Windows Registry, of which there can be a LOT of, it's important to know that if you want to see the entire contents of a Registry hive, you have a few options:

1. When using Registry Explorer, import the Registry hive(s) into Registry Explorer to view the contents manually.
    * Pro-tip: drag and drop a hive into Registry Explorer while holding the Shift key. This will automatically replay transaction logs!
    * Pro-tip: use `Ctrl + F` or `Tools -> Find` to search across the entire contents of imported Registry hives using the Registry Explorer GUI
    * You can export the contents of Registry hive(s) from Registry Explorer's GUI similar to how you would parse with RECmd using `File -> Export Registry hives` to export to various formats
2. When using KAPE, use the [KapeResearch_Registry](https://github.com/EricZimmerman/KapeFiles/blob/master/Modules/KapeResearch/KapeResearch_Registry.mkape) Modules to dump the entire contents of a Registry hive to JSON. From here, you can search/grep across the output to potentially find new areas of interest within the Registry.
3. When using RECmd, you can dump a Registry hive from the `ROOT` key. Thanks to a mid-2021 update to RECmd, you no longer have to manually specify the name of the `ROOT` key. If you simply dump from `ROOT`, it'll know to dump from the topmost Key within a Registry hive. Very useful!
    * Example syntax: `recmd.exe -f path\to\Registry\hive --kn ROOT --nl false --json C:\output\path\goes\here --jsonf HiveName_ROOT.json -q`
        * `--kn ROOT` specifies the name of the Registry Key from which to dump the contents of a Registry hive recursively
        * `--nl false` means transaction logs will be replayed
