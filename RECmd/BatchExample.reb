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
        Name: Some non-existent key
        HiveType: SYSTEM
        Category: Fake
        KeyPath: Software\Wizzo\john\doe
        Recursive: true
        Comment: "does not exist"