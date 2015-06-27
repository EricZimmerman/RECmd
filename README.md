# RECmd

Get the latest binary at http://binaryforay.blogspot.com/p/software.html

Command line Registry access:
	
	λ .\RECmd.exe

	RECmd version 0.7.0.0

	Author: Eric Zimmerman (saericzimmerman@gmail.com)
	https://github.com/EricZimmerman/RECmd

	Note: Enclose all strings containing spaces (and all RegEx) with double quotes

        Hive                    Hive to search.
        Literal                 If present, --sd and --ss search value will not be interpreted as ASCII or Unicode byte strings
        Recover                 If present, recover deleted keys/values
        Recursive               Dump keys/values recursively (ignored if --ValueName used). This option provides FULL details about keys and values
        RegEx                   If present, treat <string> in --sk, --sv, --sd, and --ss as a regular expression
        Sort                    If present, sort the output
        SuppressData            If present, do not show data when using --sd or --ss

        KeyName                 Key name. All values under this key will be dumped
        ValueName               Value name. Only this value will be dumped
        SaveToName              Saves ValueName value data in binary form to file

        StartDate               Start date to look for last write timestamps (UTC). If EndDate is not supplied, last writes AFTER this date will be returned
        EndDate                 End date to look for last write timestamps (UTC). If StartDate is not supplied, last writes BEFORE this date will be returned
        MinSize                 Find values with data size >= MinSize (specified in bytes)
        sk                      Search for <string> in key names.
        sv                      Search for <string> in value names
        sd                      Search for <string> in value record's value data
        ss                      Search for <string> in value record's value slack

	Example: RECmd.exe --Hive "C:\Temp\UsrClass 1.dat" --sk URL --Recover
         RECmd.exe --Hive "D:\temp\UsrClass 1.dat" --StartDate "11/13/2014 15:35:01"
         RECmd.exe --Hive "D:\temp\UsrClass 1.dat" --RegEx --sv "(App|Display)Name"
         RECmd.exe --Hive "D:\temp\UsrClass 1.dat" --StartDate "05/20/2014 19:00:00" --EndDate "05/20/2014 23:59:59"
         RECmd.exe --Hive "D:\temp\UsrClass 1.dat" --StartDate "05/20/2014 07:00:00 AM" --EndDate "05/20/2014 07:59:59 PM"
