Description: Sample RECmd batch file of a whole lot of stuff
Author: Eric Zimmerman
Version: 1
Id: ab13eb5f-31db-5cdc-83df-88ec12dc1a
Keys:
    -
        Description: Typed URLs
        HiveType: NTUSER
        Category: Browser history
        KeyPath: Software\Microsoft\Internet Explorer\TypedURLs
        Recursive: false
        Comment: A comment about Typed URLs
    -
        Description: WordWheelQuery
        HiveType: NTUSER
        Category: User searches
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery
        Recursive: true
        Comment: Dear lawyer, this is what a bad guy searched for
    -
        Description: Network MRU
        HiveType: NTUSER
        Category: Network shares
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\Map Network Drive MRU
        ValueName: MRUList
        Recursive: false
        Comment: An example limited to a key and value
    -
        Description: UserAssist
        HiveType: NTUSER
        Category: Execution
        KeyPath: Software\Microsoft\Windows\CurrentVersion\Explorer\UserAssist
        Recursive: true
        Comment: No comment
    -
        Description: Some non-existent key
        HiveType: SYSTEM
        Category: Fake
        KeyPath: Software\Wizzo\john\doe
        Recursive: true
        Comment: this is only here to show you what will happen if a key isnt found!
    -
        Description: Some non-existent key 2
        HiveType: NTUSER
        Category: Fake
        KeyPath: Software\Wizzo\john\doe
        Recursive: true
        Comment: Another non-existent key to keep you on your toes
    -
        Description: MountedDevices
        HiveType: SYSTEM
        Category: Devices
        KeyPath: MountedDevices
        Recursive: false
        Comment: Drive info yo!
    -
        Description: MountedDevicesNope
        HiveType: SYSTEM
        Category: Devices
        KeyPath: MountedDevicesNope
        Recursive: false
        Comment: So close
    -
        Description: Select current
        HiveType: SYSTEM
        Category: Devices
        KeyPath: Select
        ValueName: Current
        Recursive: false
        Comment: What is the current control set?
    -
        Description: Setup
        HiveType: SYSTEM
        Category: Devices
        KeyPath: Setup
        Recursive: false
        Comment: The entire setup key + values
