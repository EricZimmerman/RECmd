Description: Basic System Information
Author: Troy Larson
Version: 1
Id: 1e145fa4-70ca-478f-b0b9-a148e4ba1b90
Keys:
    -
        Description: Account Aliases
        HiveType: Sam
        Category: System Info
        KeyPath: Sam\Domains\Account\Aliases
        Recursive: true
        Comment: 
    -
        Description: Account Aliases
        HiveType: Sam
        Category: System Info
        KeyPath: Sam\Domains\Account\Groups
        Recursive: true
        Comment: 
    -
        Description: Account Users
        HiveType: Sam
        Category: System Info
        KeyPath: Sam\Domains\Account\Users
        Recursive: true
        Comment: 
    -
        Description: Machine SID
        HiveType: Security
        Category: System Info
        KeyPath: Policy\PolAcDmS
        ValueName: (Default)
        Recursive: false
        Comment: 
    -
        Description: Domain SID
        HiveType: Security
        Category: System Info
        KeyPath: Policy\PolPrDmS
        ValueName: (Default)
        Recursive: false
        Comment: 
    -
        Description: VM DhcpWithFabricAddressTime
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Virtual Machine\Guest
        ValueName: DhcpWithFabricAddressTime
        Recursive: false
        Comment: 
    -
        Description: VM GuestAgentVersion
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Virtual Machine\Guest
        ValueName: GuestAgentVersion
        Recursive: false
        Comment: 
    -
        Description: VM OSVersion
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Virtual Machine\Guest
        ValueName: OSVersion
        Recursive: false
        Comment: 
    -
        Description: VM GuestAgentStartTime
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Virtual Machine\Guest
        ValueName: GuestAgentStartTime
        Recursive: false
        Comment: 
    -
        Description: VM oobeSystem_PA_CompletionTime
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Virtual Machine\Guest
        ValueName: oobeSystem_PA_CompletionTime
        Recursive: false
        Comment: 
    -
        Description: VM oobeSystem_PA_OSVersion
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Virtual Machine\Guest
        ValueName: oobeSystem_PA_OSVersion
        Recursive: false
        Comment: 
    -
        Description: Windows Defender Exclusions
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows Defender\Exclusions\*
        Recursive: false
        Comment: 
    -
        Description: Defender Real-Time Protection
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows Defender\Real-Time Protection
        Recursive: false
        Comment: 
    -
        Description: BuildLab
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: BuildLab
        Recursive: false
        Comment: 
    -
        Description: BuildLabEx
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: BuildLabEx
        Recursive: false
        Comment: 
    -
        Description: BuildBranch
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: BuildBranch
        Recursive: false
        Comment: 
    -
        Description: BuildGUID
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: BuildGUID
        Recursive: false
        Comment: 
    -
        Description: CompositionEditionID
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: CompositionEditionID
        Recursive: false
        Comment: 
    -
        Description: CurrentBuild
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: CurrentBuild
        Recursive: false
        Comment: 
    -
        Description: CurrentBuildNumber
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: CurrentBuildNumber
        Recursive: false
        Comment: 
    -
        Description: CurrentMajorVersionNumber
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: CurrentMajorVersionNumber
        Recursive: false
        Comment: 
    -
        Description: CurrentMinorVersionNumber
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: CurrentMinorVersionNumber
        Recursive: false
        Comment: 
    -
        Description: CurrentType
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: CurrentType
        Recursive: false
        Comment: 
    -
        Description: CurrentVersion
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: CurrentVersion
        Recursive: false
        Comment: 
    -
        Description: Customizations
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: Customizations
        Recursive: false
        Comment: 
    -
        Description: EditionID
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: EditionID
        Recursive: false
        Comment: 
    -
        Description: InstallDate
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: InstallDate
        Recursive: false
        Comment: 
    -
        Description: ProductID
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: ProductID
        Recursive: false
        Comment: 
    -
        Description: ProductName
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: ProductName
        Recursive: false
        Comment: 
    -
        Description: RegisteredOrganization
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: RegisteredOrganization
        Recursive: false
        Comment: 
    -
        Description: RegisteredOwner
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion
        ValueName: RegisteredOwner
        Recursive: false
        Comment: 
    -
        Description: NetworkCards ServiceName
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkCards
        ValueName: ServiceName
        Recursive: false
        Comment: 
    -
        Description: NetworkCards Description
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkCards\*
        ValueName: Description
        Recursive: false
        Comment: 
    -
        Description: NetworkList Profiles Category
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\*
        ValueName: Category
        Recursive: false
        Comment: 
    -
        Description: NetworkList Profiles Description
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\*
        ValueName: Description
        Recursive: false
        Comment: 
    -
        Description: NetworkList Profiles Managed
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\*
        ValueName: Managed
        Recursive: false
        Comment: 
    -
        Description: NetworkList Profiles NameType
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\*
        ValueName: NameType
        Recursive: false
        Comment: 
    -
        Description: NetworkList Profiles ProfileName
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\NetworkList\Profiles\*
        ValueName: ProfileName
        Recursive: false
        Comment: 
    -
        Description: ProfileList Flags
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: Flags
        Recursive: false
        Comment: 
    -
        Description: ProfileList ProfileImagepath
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: ProfileImagepath
        Recursive: false
        Comment: 
    -
        Description: ProfileList RunLogonScriptsync
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: RunLogonScriptsync
        Recursive: false
        Comment: 
    -
        Description: ProfileList Sid
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: Sid
        Recursive: false
        Comment: 
    -
        Description: ProfileList State
        HiveType: Software
        Category: System Info
        KeyPath: Microsoft\Windows NT\CurrentVersion\ProfileList\*
        ValueName: State
        Recursive: false
        Comment: 
    -
        Description: FirmwareBootDevice
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control
        ValueName: FirmwareBootDevice
        Recursive: false
        Comment: 
    -
        Description: SystemBootDevice
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control
        ValueName: SystemBootDevice
        Recursive: false
        Comment: 
    -
        Description: ComputerName
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\ComputerName\*
        ValueName: ComputerName
        Recursive: false
        Comment: 
    -
        Description: DisableDeleteNotification
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\FileSystem
        ValueName: DisableDeleteNotification
        Recursive: false
        Comment: Is TRIM disabled?
    -
        Description: NtfsEncryptPagingFile
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\FileSystem
        ValueName: NtfsEncryptPagingFile
        Recursive: false
        Comment: 
    -
        Description: InstallLanguage
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\Nls\Language
        ValueName: InstallLanguage
        Recursive: false
        Comment: 
    -
        Description: ProductOptions\ProductSuite
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\ProductOptions
        ValueName: ProductSuite
        Recursive: false
        Comment:
    -
        Description: ProductOptions
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\ProductOptions
        ValueName: ProductType
        Recursive: false
        Comment: "LanmanNT = DC"
    -
        Description: Session Manager Environment
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\Session Manager\Environment
        Recursive: false
        Comment: 
    -
        Description: TimeZone Bias
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\TimeZoneInformation
        ValueName: Bias
        Recursive: false
        Comment: 
    -
        Description: TimeZoneKeyName
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Control\TimeZoneInformation
        ValueName: TimeZoneKeyName
        Recursive: false
        Comment: 
    -
        Description: Shares
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Services\Lanmanserver\Shares
        Recursive: false
        Comment: 
    -
        Description: Tcpip Domain
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Services\Tcpip\Parameters
        ValueName: Domain
        Recursive: false
        Comment: 
    -
        Description: Tcpip Hostname
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Services\Tcpip\Parameters
        ValueName: Hostname
        Recursive: false
        Comment: 
    -
        Description: Tcpip4 Interfaces
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Services\Tcpip\Parameters\Interfaces\*
        Recursive: false
        Comment: 
    -
        Description: Tcpip6 Interfaces
        HiveType: System
        Category: System Info
        KeyPath: ControlSet*\Services\Tcpip6\Parameters\Interfaces\*
        Recursive: false
        Comment: 
    -
        Description: Mounted Devices
        HiveType: System
        Category: System Info
        KeyPath: MountedDevices
        Recursive: false
        Comment: 
    -
        Description: SystemPartition
        HiveType: System
        Category: System Info
        KeyPath: Setup
        ValueName: SystemPartition
        Recursive: false
        Comment: 
    -
        Description: Tcpip Interfaces
        HiveType: System
        Category: System Info
        KeyPath: Select
        Recursive: false
        Comment: 
