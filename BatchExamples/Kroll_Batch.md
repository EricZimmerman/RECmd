# Acknowledgments

Special thanks to Mike Cary and Troy Larson for their work on the other RECmd Batch files that helped inspire development of this Batch file

Special thanks to those who have contributed to this Batch file:

* [Andreas Hunkeler (@Karneades)](https://github.com/Karneades)
* [Tony Knutson](https://twitter.com/bigt252002)
* Chris Kudless

# Version History

## Version History Entry Template

Example entry, please follow this format:
  
```
| X.X | YYYY-MM-DD | Added Google Chrome [Web Browsers]. Added *insert Threat Hunting artifact here* [Threat Hunting]. Added 1Password [Installed Software] |
```

## Version History Log

| Version | Date | Release Notes |
|---|---|---|
| 1.0 | 2021-02-14 | Initial release |
| 1.1 | 2021-02-20 | Added Total Commander [Third Party Applications]. Added CCleaner Browser [Web Browsers]. Created category [Event Logs] |
| 1.2 | 2021-04-08 | Changed ProfileList's recursive value to false to prevent duplicate/unnecessary entries, created Threat Hunting category, added ShadowRDP [Threat Hunting] |
| 1.3 | 2021-04-20 | Fixed an issue with Cloud Storage -> DropBox previously mapping to OneDrive |
| 1.4 | 2021-04-22 | Added more artifacts for Cloud Storage -> OneDrive |
| 1.5 | 2021-04-23 | Added more Threat Hunting artifacts |
| 1.6 | 2021-05-04 | Added more Network Share artifacts |
| 1.7 | 2021-05-15 | Added Windows Clipboard History and Windows 10 Timeline artifacts [System Info] |
| 1.8 | 2021-05-29 | Removed duplicative entry via changing from Recursive:true to Recursive:false for multiple artifacts with plugins and ensured plugins are being properly utilized. As a result, greatly reduced CSV output size while increasing amount of useful data parsed. In my testing, 72k lines (33mb) -> 13k lines (6.88mb). Added Visual Studio artifacts [Installed Software]. Fixed FirstFolder mislabeling [User Activity]. Cleaned up Internet Explorer artifacts [Web Browsers]. Added binary values using BinaryConvert to replace (Binary data) entries, when possible. |
| 1.9 | 2021-06-24 | Revised Version History formatting [Version History]. Added running Special Thanks list [Acknowledgment]. Added PortProxy artifacts [Threat Hunting]. Added WinLogon and LogonUI artifacts [System Info]. Added QNAP QFinder, 4K Video Downloader, and TeamViewer artifacts [Third Party Applications]. Added Hades IOCs [Threat Hunting]. Fixed OneDrive UserSyncRoots artifact [Cloud Storage] |
| 1.10 | 2021-06-28 | Added Defender Exclusions [Antivirus] |
| 1.11 | 2021-07-06 | Added IncludeBinary to DHCPHardwareCount ValueName [System Info]. Removed duplicate entries (i.e., values being parsed twice) from the Uninstall Key [Installed Software] resulting in 2k less rows in testing. Added relevant Key related to Kaseya Ransomware attack of July 2021 [Threat Hunting]. Expanded WinLogon artifacts based on same attack [System Info] |
| 1.12 | 2021-07-12 | Added SysInternals Tools [Installed Software] |
| 1.13 | 2021-09-03 | Removed duplicate artifacts (LastVisitedMRU), added more documentation to various artifacts, added LockBit IOC [Threat Hunting] |
| 1.14 | 2021-09-09 | Added MuiCache and AppCompatFlags [Program Execution]. Added Restricted Admin Status and more Windows Defender artifacts [Threat Hunting]. Added RealVNC - VNC Viewer, add WinRAR plugin [Third Party Applications]. Added Products artifacts [Installed Software]. Updated Microsoft Office Trusted Documents description [Microsoft Office] |
| 1.15 | 2021-09-15 | Added webcam and microphone values [Devices]. Moved Acknowledgments, Version History, Documentation, and Guidelines to a dedicated README on GitHub repo |
| 1.16 | 2021-09-28 | Added Microsoft Exchange category for identifying patch entries [Microsoft Exchange] |
| 1.17 | 2021-11-24 | Added various documentation to preexisting artifacts, removed [Antivirus] category and moved all to [Threat Hunting], added Symantec Endpoint Protection quarantine records [Threat Hunting] |
| 1.18 | 2021-12-09 | Added exefile open command registry keys [Threat Hunting] |
| 1.19 | 2022-02-01 | Remove `Mapped Network Drives` [User Activity] since it was a duplicate artifact of `Network Drive MRU` [Network Shares]. Added Image File Execution Options Injection [Threat Hunting]. BTHPORT: Recursive: true -> false due to duplication of effort with BTHPORT Plugin. Revised [Microsoft Exchange] section and adjusted to capture more patching scenarios |
| 1.20 | 2022-06-01 | Added Registry artifacts for CVE-2022-30190 [Threat Hunting] |

# Documentation

https://docs.microsoft.com/en-US/troubleshoot/windows-server/performance/windows-registry-advanced-users

# Guidelines

If you're not going to `Recursive: true` on a key or subkey, please prepend with a `Category -> Description` comment before the series of multiple entries for the values to be parsed
In the above instance, if possible, save all documentation for the last entry in a series, unless a specific helpful reference exists for a given `ValueName`
If an entry is using a Plugin to generate output, please include a comment about which Plugin is being used below that entry in this batch file.
