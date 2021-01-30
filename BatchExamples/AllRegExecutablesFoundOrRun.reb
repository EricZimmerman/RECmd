Description: Executables discovered or used
Author: Troyla
Version: 1
Id: 230a19c6-4234-459e-a4da-fb10b19e8101
Keys:
    -
        Description: AppCompatFlags CIT System
        HiveType: Software
        Category: Executables
        KeyPath: Microsoft\Windows NT\CurrentVersion\AppCompatFlags\CIT\System
        Recursive: false
        Comment:
    -
        Description: AppCompatFlags CIT Module
        HiveType: Software
        Category: Executables
        KeyPath: Microsoft\Windows NT\CurrentVersion\AppCompatFlags\CIT\Module
        Recursive: true
        Comment:
    -
        Description: Bam
        HiveType: System
        Category: Executables
        KeyPath: ControlSet*\Services\bam\State\UserSettings\*
        Recursive: false
        Comment:
    -
        Description: Bam
        HiveType: System
        Category: Executables
        KeyPath: ControlSet*\Services\bam\UserSettings\*
        Recursive: false
        Comment:
    -
        Description: Dam
        HiveType: System
        Category: Executables
        KeyPath: ControlSet*\Services\dam\State\UserSettings\*
        Recursive: false
        Comment:
    -
        Description: Dam
        HiveType: System
        Category: Executables
        KeyPath: ControlSet*\Services\dam\UserSettings\*
        Recursive: false
        Comment:
    -
        Description: Regedit.exe Last Run
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Applets\Regedit
        Recursive: false
        Comment:
    -
        Description: Explorer ComDlg32 LastVisitedPidlMRU
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRU
        Recursive: false
        Comment:
    -
        Description: Explorer ComDlg32 LastVisitedPidlMRULegacy
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\LastVisitedPidlMRULegacy
        Recursive: false
        Comment:
    -
        Description: Explorer RunMRU
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU
        Recursive: false
        Comment:
    -
        Description: UserAssist Executables
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{CEBFF5CD-ACE2-4F4F-9178-9926F41749EA}\Count
        Recursive: false
        Comment:
    -
        Description: UserAssist .lnk
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist\{F4E57C4B-2036-45F0-A9AB-443BCFE33D9F}\Count
        Recursive: false
        Comment:
    -
        Description: Search RecentApps
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Search\RecentApps\*
        Recursive: false
        Comment:
    -
        Description: AppCompatFlags Compatibility Assistant Persisted
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Persisted
        Recursive: false
        Comment:
    -
        Description: AppCompatFlags Compatibility Assistant Store
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store
        Recursive: false
        Comment:
    -
        Description: AppCompatFlags Layers
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers
        Recursive: false
        Comment:
    -
        Description: Sysinternals Tools Run
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Sysinternals\*
        ValueName: EulaAccepted
        Recursive: false
        Comment:
    -
        Description: FeatureUsage
        HiveType: NtUser
        Category: Executables
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\FeatureUsage
        Recursive: true
        Comment:
