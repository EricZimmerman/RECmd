Description: RECmd batch file
Author: Mike Cary
Version: 1
Id: 4eec0ce6-d1c3-4b65-9f0e-3ccd429d506c
Keys:
    -
        Description: Network
        HiveType: NTUSER
        Category: Devices
        KeyPath: Network
        Recursive: true
        Comment: Network Drives
    -
        Description: Typed URLs
        HiveType: NTUSER
        Category: Browser history
        KeyPath: Software\Microsoft\Internet Explorer\TypedURLs
        Recursive: false
        Comment: IE/Edge Typed URLs 
    -
        Description: MS Office MRU
        HiveType: NTUSER
        Category: File and Folder Opening
        KeyPath: SOFTWARE\Microsoft\Office\*\*\User MRU\*\*
        Recursive: true
        Comment: MS Office MRU   
    -
        Description: Terminal Server Client
        HiveType: NTUSER
        Category: Terminal Server Client
        KeyPath: Software\Microsoft\Terminal Server Client 
        Recursive: false
        Comment: Terminal Server Client
    -
        Description: CIDSizeMRU
        HiveType: NTUSER
        Category: File Access and Opening
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\CIDSizeMRU
        Recursive: false
        Comment: CIDSizeMRU
    -
        Description: FirstFolder
        HiveType: NTUSER
        Category: File Access and Opening
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\FirstFolder
        Recursive: false
        Comment: OpenSavePidlMRU
    -
        Description: LastVisitedPidlMRU
        HiveType: NTUSER
        Category: File Access and Opening
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU
        Recursive: false
        Comment: LastVisitedPidlMRU
    -
        Description: LastVisitedPidlMRULegacy
        HiveType: NTUSER
        Category: File Access and Opening
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRULegacy
        Recursive: false
        Comment: LastVisitedPidlMRULegacy
    -
        Description: OpenSavePidlMRU
        HiveType: NTUSER
        Category: File Access and Opening
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU
        Recursive: true
        Comment: OpenSavePidlMRU
    -
        Description: File Extensions
        HiveType: NTUSER
        Category: System Info
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts
        Recursive: false
        Comment: File Extensions
    -
        Description: RecentDocs
        HiveType: NTUSER
        Category: File Access and Opening
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs
        Recursive: true
        Comment: RecentDocs
    -
        Description: MountPoints2
        HiveType: NTUSER
        Category: Devices
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2
        Recursive: true
        Comment: Mount Points - NTUSER
    -
        Description: User Run Key
        HiveType: NTUSER
        Category: Autoruns
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Run
        Recursive: false
        Comment: User Run Key
    -
        Description: User RunOnce Key
        HiveType: NTUSER
        Category: Autoruns
        KeyPath: Software\Microsoft\Windows\CurrentVersion\RunOnce
        Recursive: false
        Comment: User RunOnce Key
    -
        Description: RunMRU
        HiveType: NTUSER
        Category: Program Execution
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU
        Recursive: false
        Comment: RunMRU(Start>Run)
    -
        Description: TypedPaths
        HiveType: NTUSER
        Category: File Access and Opening
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\TypedPaths
        Recursive: false
        Comment: TypedPaths
    -
        Description: UserAssist
        HiveType: NTUSER
        Category: Program Execution
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist
        Recursive: true
        Comment: user assist
    -
        Description: WordWheelQuery
        HiveType: NTUSER
        Category: User searches
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery
        Recursive: true
        Comment: User Searches
    -
        Description: RecentApps
        HiveType: NTUSER
        Category: Program Execution
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Search\RecentApps
        Recursive: true
        Comment: RecentApps
    -    
        Description: SAM Users
        HiveType: SAM
        Category: Users
        KeyPath: SAM\Domains\Account\Users
        Recursive: true
        Comment: User accounts in SAM file
    -
        Description: Windows NT Current Version
        HiveType: SOFTWARE
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        Recursive: false
        Comment: OS version and install info
    -
        Description: Network List 
        HiveType: SOFTWARE
        Category: Autoruns
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkList
        Recursive: false
        Comment: Network List
    -
        Description: Group Policy Run Key
        HiveType: SOFTWARE
        Category: Autoruns
        KeyPath: Microsoft\Windows\CurrentVersion\policies\Explorer\Run
        Recursive: false
        Comment: Group Policy Run Key
    -
        Description: System Run Key
        HiveType: SOFTWARE
        Category: Autoruns
        KeyPath: Microsoft\Windows\CurrentVersion\Run
        Recursive: false
        Comment: System Run Key
    -
        Description: System RunOnce Key
        HiveType: SOFTWARE
        Category: Autoruns
        KeyPath: Microsoft\Windows\CurrentVersion\RunOnce
        Recursive: false
        Comment: System RunOnce Key
    -
        Description: Portable Devices
        HiveType: SOFTWARE
        Category: Devices
        KeyPath: Microsoft\Windows Portable Devices\Devices\
        Recursive: true
        Comment: Portable Devices
    -    
        Description: ComputerName
        HiveType: SYSTEM
        Category: System Info
        KeyPath: ControlSet00*\Control\ComputerName\ComputerName
        ValueName: ComputerName
        Recursive: false
        Comment: Computer name
    -    
        Description: AppCompatCache
        HiveType: SYSTEM
        Category: Program Execution
        KeyPath: ControlSet00*\Control\Session Manager\AppCompatCache
        Recursive: false
        Comment: AppCompatCache
    -    
        Description: TimeZoneInformation
        HiveType: SYSTEM
        Category: System Info
        KeyPath: ControlSet00*\Control\TimeZoneInformation
        Recursive: false
        Comment: TimeZoneInformation
    -
        Description: Services
        HiveType: SYSTEM
        Category: Services
        KeyPath: ControlSet00*\Services
        Recursive: true
        Comment: Services
    -    
        Description: BAM
        HiveType: SYSTEM
        Category: Program Execution
        KeyPath: ControlSet00*\Services\bam\UserSettings
        Recursive: true
        Comment: BAM
    -    
        Description: DAM
        HiveType: SYSTEM
        Category: Program Execution
        KeyPath: ControlSet00*\Services\dam\UserSettings
        Recursive: true
        Comment: DAM
    -    
        Description: Network Shares
        HiveType: SYSTEM
        Category: Network
        KeyPath: ControlSet00*\Services\lanmanserver\Shares
        Recursive: false
        Comment: Network Shares
    -    
        Description: DHCP Network Hints
        HiveType: SYSTEM
        Category: Network Configuration
        KeyPath: ControlSet00*\Services\Tcpip\Parameters\Interfaces
        Recursive: true
        Comment: DHCP Hints 
    -    
        Description: Network Interfaces
        HiveType: SYSTEM
        Category: Network Configuration
        KeyPath: ControlSet00*\Services\Tcpip\Parameters\Interfaces\*
        Recursive: true
        DisablePlugin: true
        Comment: Network Interfaces       
    -
        Description: MountedDevices
        HiveType: SYSTEM
        Category: Devices
        KeyPath: MountedDevices
        Recursive: false
        Comment: Mounted Drives
    -
        Description: Setup
        HiveType: SYSTEM
        Category: Devices
        KeyPath: Setup
        Recursive: false
        Comment: Setup key
    -
        Description: Current Control Set Name
        HiveType: SYSTEM
        Category: Devices
        KeyPath: Select
        ValueName: Current
        Recursive: false
        Comment: Current Control Set Name
    -
        Description: Shutdown Time
        HiveType: SYSTEM
        Category: System Info
        KeyPath: ControlSet00*\Control\Windows
        ValueName: ShutdownTime
        Recursive: false
        Comment: Shutdown Time
    -
        Description: ProfileList Flags
        HiveType: Software
        Category: ProfileList
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: Flags
        Recursive: false
        Comment: 
    -
        Description: ProfileList ProfileImagepath
        HiveType: Software
        Category: ProfileList
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: ProfileImagepath
        Recursive: false
        Comment: 
    -
        Description: ProfileList RunLogonScriptsync
        HiveType: Software
        Category: ProfileList
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: RunLogonScriptsync
        Recursive: false
        Comment: 
    -
        Description: ProfileList Sid
        HiveType: Software
        Category: ProfileList
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: Sid
        Recursive: false
        Comment: 
    -
        Description: ProfileList State
        HiveType: Software
        Category: ProfileList
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: State
        Recursive: false
        Comment: 
