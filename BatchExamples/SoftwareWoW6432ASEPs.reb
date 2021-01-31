Description: CIS Software WOW6432 ASEPs
Author: Troy Larson
Version: 1
Id: 2d73c560-2b4f-4ab0-9129-6f76c20ce362
Keys:
    -
        Description: WOW6432Node CLSID
        HiveType: Software
        Category: ASEP Classes
        KeyPath: Classes\WOW6432Node\CLSID\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: WOW6432Node CLSID InprocServer32
        HiveType: Software
        Category: ASEP
        KeyPath: Classes\WOW6432Node\CLSID\{*}\InprocServer32
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: WOW6432Node CLSID Instance
        HiveType: Software
        Category: ASEP
        KeyPath: Classes\WOW6432Node\CLSID\{*}\Instance\{*}
        ValueName: CLSID
        Recursive: true
        Comment:
    -
        Description: WOW6432Node CLSID Instance
        HiveType: Software
        Category: ASEP Classes
        KeyPath: Classes\WOW6432Node\CLSID\{*}\Instance\{*}
        ValueName: FriendlyName
        Recursive: true
        Comment:
    -
        Description: WOW6432Node CLSID PersistentHandler
        HiveType: Software
        Category: ASEP Classes
        KeyPath: Classes\WOW6432Node\CLSID\{*}\PersistentHandler
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: WOW6432Node CLSID ProgID
        HiveType: Software
        Category: ASEP Classes
        KeyPath: Classes\WOW6432Node\CLSID\{*}\ProgID
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Wow6432Node Active Setup Installed Components
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Active Setup\Installed Components\*
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Wow6432Node Active Setup Installed Components
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Active Setup\Installed Components\*
        ValueName: ShellComponent
        Recursive: true
        Comment:
    -
        Description: Wow6432Node Active Setup Installed Components
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Active Setup\Installed Components\*
        ValueName: StubPath
        Recursive: true
        Comment:
    -
        Description: CLSID Instances
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Classes\CLSID\{*}\Instance\{*}
        ValueName: CLSID
        Recursive: true
        Comment:
    -
        Description: CLSID Instances
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Classes\CLSID\{*}\Instance\{*}
        ValueName: FriendlyName
        Recursive: true
        Comment:
    -
        Description: .NETFramework
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\.NETFramework
        ValueName: DbgManagedDebugger
        Recursive: false
        Comment:
    -
        Description: Active Setup Installed Components
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Active Setup\Installed Components\*
        Recursive: true
        Comment:
    -
        Description: Internet ExplorerExtensions
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Internet Explorer\Extensions\*
        Recursive: true
        Comment:
    -
        Description: WOW6432Node\Microsoft\Internet Explorer\Low Rights\DragDrop\{*}
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Internet Explorer\Low Rights\DragDrop\{*}
        ValueName: AppName
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Low Rights DragDrop
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Internet Explorer\Low Rights\DragDrop\{*}
        ValueName: AppPath
        Recursive: true
        Comment:
    -
        Description: Office Addins
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Office\*[office product]\Addins\*
        Recursive: true
        Comment:
    -
        Description: Windows CE Services AutoStartOnConnect
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows CE Services\AutoStartOnConnect
        Recursive: true
        Comment:
    -
        Description: Windows CE Services AutoStartOnDisconnect
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows CE Services\AutoStartOnDisconnect
        Recursive: true
        Comment:
    -
        Description: Drivers
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows NT\CurrentVersion\Drivers
        Recursive: true
        Comment:
    -
        Description: Drivers32
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Drivers32
        Recursive: true
        Comment:
    -
        Description: TaskCache Boot
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Boot\{*}
        Recursive: true
        Comment:
    -
        Description: TaskCacheLogon
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Logon\{*}
        Recursive: true
        Comment:
    -
        Description: TaskCache Maintenance
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Maintenance\{*}
        Recursive: true
        Comment:
    -
        Description: TaskCache Plain
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Plain\{*}
        Recursive: true
        Comment:
    -
        Description: TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        Recursive: true
        Comment:
    -
        Description: TaskCache Tree
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\*
        ValueName: id
        Recursive: true
        Comment:
    -
        Description: TaskCache Tree Wow6432Node
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\Wow6432Node\Microsoft\*\*
        ValueName: id
        Recursive: true
        Comment:
    -
        Description: SvcHost
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\SvcHost
        Recursive: true
        Comment:
    -
        Description: Windows AppInit_DLLs
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows NT\CurrentVersion\Windows
        ValueName: AppInit_DLLs
        Recursive: true
        Comment:
    -
        Description: Windows RequireSignedAppInit_DLLs
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows NT\CurrentVersion\Windows
        ValueName: RequireSignedAppInit_DLLs
        Recursive: true
        Comment:
    -
        Description: Winlogon
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Winlogon
        Recursive: false
        Comment:
    -
        Description: Winlogon AlternateShells AvailableShells
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Winlogon\AlternateShells\AvailableShells
        Recursive: true
        Comment:
    -
        Description: Winlogon GPExtensions
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Winlogon\GPExtensions\{*}
        ValueName: (default)
        Recursive: false
        Comment:
    -
        Description: Winlogon GPExtensions
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Winlogon\GPExtensions\{*}
        ValueName: DllName
        Recursive: false
        Comment:
    -
        Description: Winlogon Notify
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows NT\CurrentVersion\Winlogon\Notify\*
        ValueName: dllname
        Recursive: true
        Comment:
    -
        Description: Explorer Browser Helper Objects
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\*
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Explorer SharedTaskScheduler
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\SharedTaskScheduler
        Recursive: true
        Comment:
    -
        Description: Explorer ShellIconOverlayIdentifiers
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers\*
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Explorer ShellServiceObjects
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Explorer\ShellServiceObjects\{*}
        ValueName: AutoStart
        Recursive: true
        Comment:
    -
        Description: Ext PreApproved
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows\CurrentVersion\Ext\PreApproved\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Group Policy Scripts Shutdown
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Shutdown\*
        Recursive: true
        Comment:
    -
        Description: Group Policy Scripts Startup
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Startup\*
        Recursive: true
        Comment:
    -
        Description: Policies System
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\System
        ValueName: Shell
        Recursive: false
        Comment:
    -
        Description: Policies System
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\System
        ValueName: UIHOST
        Recursive: false
        Comment:
    -
        Description: Policies System
        HiveType: Software
        Category: ASEP
        KeyPath: Wow6432Node\Microsoft\Windows\CurrentVersion\Policies\System
        ValueName: UserInit
        Recursive: false
        Comment:
    -
        Description: Run
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows\CurrentVersion\Run
        Recursive: true
        Comment:
    -
        Description: RunOnce
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce
        Recursive: true
        Comment:
    -
        Description: RunOnceEx
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnceEx
        Recursive: true
        Comment:
    -
        Description: Shell Extensions Approved
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows\CurrentVersion\Shell Extensions\Approved
        Recursive: true
        Comment:
    -
        Description: ShellServiceObjectDelayLoad
        HiveType: Software
        Category: ASEP
        KeyPath: WOW6432Node\Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad
        Recursive: true
        Comment:
