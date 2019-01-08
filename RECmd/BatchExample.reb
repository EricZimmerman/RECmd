Description: Sample RECmd batch file
Author: Eric Zimmerman
Version: 1
Id: ab13eb5f-31db-5cdc-83df-88ec83dc7a
Keys:
    -
        Name: Typed URLs
        HiveType: NTUSER
        Category: Browser history
        KeyPath: Software\Microsoft\Internet Explorer\TypedURLs
        Recursive: false
        Comment: "some comment"
    -
        Name: WordWheelQuery
        HiveType: NTUSER
        Category: User searches
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery
        Recursive: true
        Comment: "search comment"
    -
        Name: Network MRU
        HiveType: NTUSER
        Category: Network shares
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\Map Network Drive MRU
        ValueName: MRUList
        Recursive: false
        Comment: "value only"
    -
        Name: UserAssist
        HiveType: NTUSER
        Category: Execution
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist
        Recursive: true
        Comment: "user assist"
    -
        Name: JustDeletedTest
        HiveType: NTUSER
        Category: Deletion
        KeyPath: Software\Microsoft\Office\15.0\Common\Internet\WebServiceCache\AllUsers\officeimg.vo.msecnd.net\en-US-file.aspx-AssetId=ZA102826685
        Recursive: false
        Comment: "should be a deleted key"
    -
        Name: Some non-existent key
        HiveType: SYSTEM
        Category: Fake
        KeyPath: Software\Wizzo\john\doe
        Recursive: true
        Comment: "does not exist"
    -
        Name: Some non-existent key 2
        HiveType: NTUSER
        Category: Fake
        KeyPath: Software\Wizzo\john\doe
        Recursive: true
        Comment: "does not exist"
    -
        Name: MountedDevices
        HiveType: SYSTEM
        Category: Devices
        KeyPath: MountedDevices
        Recursive: false
        Comment: "get them drives"
    -
        Name: MountedDevicesNope
        HiveType: SYSTEM
        Category: Devices
        KeyPath: MountedDevicesNope
        Recursive: false
        Comment: "get them drives nope"
    -
        Name: Select current
        HiveType: SYSTEM
        Category: Devices
        KeyPath: Select
        ValueName: Current
        Recursive: false
        Comment: "gcurrent value"
    -
        Name: Setup
        HiveType: SYSTEM
        Category: Devices
        KeyPath: Setup
        Recursive: false
        Comment: "Setup key"



