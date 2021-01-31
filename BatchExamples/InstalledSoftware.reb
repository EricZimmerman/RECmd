Description: Installed Software
Author: Troy Larson
Version: 1
Id: 49ff9762-4dce-413f-928b-786daa8aec5f
Keys:
    -
        Description: User Products InstallProperties
        HiveType: Software
        Category: Installed Software
        KeyPath: Microsoft\Windows\CurrentVersion\Installer\UserData\*\Products\*\InstallProperties
        ValueName: DisplayName
        Recursive: false
        Comment:
    -
        Description: User Products InstallProperties
        HiveType: Software
        Category: Installed Software
        KeyPath: Microsoft\Windows\CurrentVersion\Installer\UserData\*\Products\*\InstallProperties
        ValueName: InstallDate
        Recursive: false
        Comment:
    -
        Description: User Products InstallProperties
        HiveType: Software
        Category: Installed Software
        KeyPath: Microsoft\Windows\CurrentVersion\Installer\UserData\*\Products\*\InstallProperties
        ValueName: Publisher
        Recursive: false
        Comment:
    -
        Description: Uninstall DisplayName
        HiveType: Software
        Category: Installed Software
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: DisplayName
        Recursive: false
        Comment:
    -
        Description: Uninstall InstallDate
        HiveType: Software
        Category: Installed Software
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: InstallDate
        Recursive: false
        Comment:
    -
        Description: Uninstall Publisher
        HiveType: Software
        Category: Installed Software
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: Publisher
        Recursive: false
        Comment:
    -
        Description: Wow6432 Uninstall DisplayName
        HiveType: Software
        Category: Installed Software
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: DisplayName
        Recursive: false
        Comment:
    -
        Description: Wow6432 Uninstall InstallDate
        HiveType: Software
        Category: Installed Software
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: InstallDate
        Recursive: false
        Comment:
    -
        Description: Wow6432 Uninstall Publisher
        HiveType: Software
        Category: Installed Software
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: Publisher
        Recursive: false
        Comment:
    -
        Description: Uninstall DisplayName
        HiveType: NtUser
        Category: Installed Software
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: DisplayName
        Recursive: false
        Comment:
    -
        Description: Uninstall InstallDate
        HiveType: NtUser
        Category: Installed Software
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: InstallDate
        Recursive: false
        Comment:
    -
        Description: Uninstall Publisher
        HiveType: NtUser
        Category: Installed Software
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: Publisher
        Recursive: false
        Comment:
    -
        Description: Wow6432Node Uninstall DisplayName
        HiveType: NtUser
        Category: Installed Software
        KeyPath: Wow6432Node\Software\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: DisplayName
        Recursive: false
        Comment:
    -
        Description: Wow6432Node Uninstall InstallDate
        HiveType: NtUser
        Category: Installed Software
        KeyPath: Wow6432Node\Software\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: InstallDate
        Recursive: false
        Comment:
    -
        Description: Wow6432Node Uninstall Publisher
        HiveType: NtUser
        Category: Installed Software
        KeyPath: Wow6432Node\Software\Microsoft\Windows\CurrentVersion\Uninstall\*
        ValueName: Publisher
        Recursive: false
        Comment:
