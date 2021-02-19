Description: CIS System ASEPs
Author: Troyla
Version: 1.0
Id: 2d9ca90f-3ffd-410d-a36b-0992883e45a2
Keys:
    -
        Description: ServiceControlManagerExtension
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control
        ValueName: ServiceControlManagerExtension
        Recursive: false
        Comment:
    -
        Description: BootVerificationProgram
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\BootVerificationProgram
        ValueName: Imagepath
        Recursive: false
        Comment:
    -
        Description: LSA Authentication Packages
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\LSA
        ValueName: Authentication Packages
        Recursive: false
        Comment:
    -
        Description: LSA Notification Packages
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\LSA
        ValueName: Notification Packages
        Recursive: false
        Comment:
    -
        Description: LSA Security Packages
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\LSA
        ValueName: Security Packages
        Recursive: false
        Comment:
    -
        Description: LSA OsConfig
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\LSA\OsConfig
        ValueName: Security Packages
        Recursive: false
        Comment:
    -
        Description: NetworkProvider Order
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: \ControlSet*\Control\NetworkProvider\Order
        ValueName: ProviderOrder
        Recursive: false
        Comment:
    -
        Description: Print Driver
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Print\Monitors\*
        ValueName: Driver
        Recursive: true
        Comment:
    -
        Description: Print Providers
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Print\Providers\*
        Recursive: true
        Comment:
    -
        Description: SafeBoot
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: \ControlSet*\Control\SafeBoot
        ValueName: AlternateShell
        Recursive: false
        Comment:
    -
        Description: SafeBoot Minimal
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\SafeBoot\Minimal\*
        Recursive: true
        Comment:
    -
        Description: SafeBoot Network
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\SafeBoot\Network\*
        Recursive: true
        Comment:
    -
        Description: SecurityProviders
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\SecurityProviders
        ValueName: SecurityProviders
        Recursive: false
        Comment:
    -
        Description: Session Manager BootExecute
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Session Manager
        ValueName: BootExecute
        Recursive: false
        Comment:
    -
        Description: Session Manager PendingFileRenameOperations
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Session Manager
        ValueName: PendingFileRenameOperations*
        Recursive: true
        Comment:
    -
        Description: Session Manager SETUPEXECUTE
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Session Manager
        ValueName: SETUPEXECUTE
        Recursive: false
        Comment:
    -
        Description: Session Manager KnownDLLs
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Session Manager\KnownDLLs
        Recursive: false
        Comment:
    -
        Description: Session Manager SubSystems
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Session Manager\SubSystems
        Recursive: false
        Comment:
    -
        Description: Terminal Server StartupPrograms
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\Terminal Server\Wds\rdpwd
        ValueName: StartupPrograms
        Recursive: false
        Comment:
    -
        Description: Terminal Server WinStations RDP-Tcp
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: \ControlSet*\Control\Terminal Server\WinStations\RDP-Tcp\*
        Recursive: true
        Comment:
    -
        Description: WOW KnownDLLs
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Control\WOW
        ValueName: KnownDLLs
        Recursive: false
        Comment:
    -
        Description: Services
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: ControlSet*\Services
        Recursive: true
        Comment:
    -
        Description: Setup
        HiveType: SYSTEM
        Category: ASEP
        KeyPath: Setup
        ValueName: CmdLine
        Recursive: true
        Comment:
