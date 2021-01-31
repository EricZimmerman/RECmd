Description: Boot Config
Author: Troy Larson
Version: 1
Id: 7884cf1a-1fea-4efd-821c-87790ea11663
Keys:
    -
        Description: Boot Volume(s)
        HiveType: BCD
        Category: System Info
        KeyPath: Objects\{9dea862c-5cdd-4e70-acc1-f32b344d4795}\Elements\24000001
        ValueName: Element
        Recursive: false
        Comment: For BCD hive at \boot\bcd, more than one GUID in value data means multi-boot system.
