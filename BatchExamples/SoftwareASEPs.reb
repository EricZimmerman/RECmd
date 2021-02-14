Description: CIS Software ASEPs
Author: Troyla
Version: 1.0
Id: 0aca270c-9969-4c61-ad5e-6fe551dff59d
Keys:
    -
        Description: Clients StartMenuInternet shell open command
        HiveType: Software
        Category: ASEP
        KeyPath: Clients\StartMenuInternet\*\shell\open\command
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Clients StartMenuInternet Shell RunAs Command
        HiveType: Software
        Category: ASEP
        KeyPath: Clients\StartMenuInternet\*\Shell\RunAs\Command
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Firefox Components
        HiveType: Software
        Category: ASEP
        KeyPath: Firefox\*\Components
        Recursive: true
        Comment:
    -
        Description: Firefox Extensions Components
        HiveType: Software
        Category: ASEP
        KeyPath: Firefox\*\Extensions\Components
        Recursive: true
        Comment:
    -
        Description: Firefox Extensions Plugins
        HiveType: Software
        Category: ASEP
        KeyPath: Firefox\*\Extensions\Plugins
        Recursive: true
        Comment:
    -
        Description: Firefox Plugins
        HiveType: Software
        Category: ASEP
        KeyPath: Firefox\*\Plugins
        Recursive: true
        Comment:
    -
        Description: Chrome Extensions
        HiveType: Software
        Category: ASEP
        KeyPath: Google\Chrome\Extensions\*\Path
        Recursive: true
        Comment:
    -
        Description: .NETFramework
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\.NETFramework
        ValueName: DbgManagedDebugger
        Recursive: true
        Comment:
    -
        Description: Active Setup Installed Components
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Active Setup\Installed Components\*
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Active Setup Installed Components
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Active Setup\Installed Components\*
        ValueName: ShellComponent
        Recursive: true
        Comment:
    -
        Description: Active Setup Installed Components
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Active Setup\Installed Components\*
        ValueName: StubPath
        Recursive: true
        Comment:
    -
        Description: Command Processor
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Command Processor
        ValueName: autorun
        Recursive: true
        Comment:
    -
        Description: Cryptography Offload
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Cryptography\Offload
        ValueName: ExpoOffload
        Recursive: true
        Comment:
    -
        Description: Ctf LangBarAddin
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Ctf\LangBarAddin\{*}
        ValueName: Filepath
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Approved Extensions
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Approved Extensions
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Explorer BARS
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Explorer BARS\*
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Extension Validation
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Extension Validation
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Extensions
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Extensions\{*}
        ValueName: ClsidExtension
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Low Rights DragDrop
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Low Rights\DragDrop\{*}
        ValueName: AppName
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Low Rights DragDrop
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Low Rights\DragDrop\{*}
        ValueName: AppPath
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Low Rights ElevationPolicy
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Low Rights\ElevationPolicy\{*}
        ValueName: AppName
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Plugin Extension
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\*\Extension
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Toolbar
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Toolbar
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Toolbar ShellBrowser
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Toolbar\ShellBrowser
        Recursive: true
        Comment:
    -
        Description: Internet Explorer Toolbar WebBrowser
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\Toolbar\WebBrowser
        Recursive: true
        Comment:
    -
        Description: Microsoft\Internet Explorer\URLSearchHooks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Internet Explorer\URLSearchHooks
        Recursive: true
        Comment:
    -
        Description: Office Addins
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Office\*\Addins\*
        ValueName: Description
        Recursive: true
        Comment:
    -
        Description: Office Addins
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Office\*\Addins\*
        ValueName: FriendlyName
        Recursive: true
        Comment:
    -
        Description: Office Addins
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Office\*\Addins\*
        ValueName: LoadBehavior
        Recursive: true
        Comment:
    -
        Description: Windows CE Services AutoStartOnConnect
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows CE Services\AutoStartOnConnect
        Recursive: true
        Comment:
    -
        Description: Windows CE Services AutoStartOnDisconnect
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows CE Services\AutoStartOnDisconnect
        Recursive: true
        Comment:
    -
        Description: AppCompatFlags Layers
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\Current Version\AppCompatFlags\Layers
        Recursive: true
        Comment:
    -
        Description: AeDebug
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\AeDebug
        ValueName: auto
        Recursive: true
        Comment:
    -
        Description: AeDebug
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\AeDebug
        ValueName: Debugger
        Recursive: true
        Comment:
    -
        Description: AeDebug
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\AeDebug
        ValueName: UserDebuggerHotKey
        Recursive: true
        Comment:

    -
        Description: AppCompatFlags
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\AppCompatFlags
        ValueName: Custom
        Recursive: true
        Comment:
    -
        Description: AppCompatFlags
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\AppCompatFlags
        ValueName: InstalledSDB
        Recursive: true
        Comment:
    -
        Description: Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Custom
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Custom
        Recursive: true
        Comment:
    -
        Description: AppCompatFlags InstalledSDB\{*}
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\AppCompatFlags\InstalledSDB\{*}
        Recursive: true
        Comment:
    -
        Description: Drivers
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Drivers
        Recursive: true
        Comment:
    -
        Description: Drivers32
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Drivers32
        Recursive: true
        Comment:
    -
        Description: Font Drivers
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Font Drivers
        Recursive: true
        Comment:
    -
        Description: Image File Execution Options
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*
        ValueName: GlobalFlag
        Recursive: true
        Comment:
    -
        Description: Image File Execution Options
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Image File Execution Options\*
        ValueName: Debugger
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Boot
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Boot\{*}
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Logon
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Logon\{*}
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Maintenance
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Maintenance\{*}
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Plain
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Plain\{*}
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Actions
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Author
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Description
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: DynamicInfo
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Hash
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Path
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Schema
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: SecurityDescriptor
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Source
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Triggers
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: URI
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tasks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tasks\{*}
        ValueName: Version
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tree
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\*
        ValueName: Id
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tree
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\*\*
        ValueName: Id
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tree
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\Microsoft\*\*
        ValueName: Id
        Recursive: true
        Comment:
    -
        Description: Schedule TaskCache Tree
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Schedule\TaskCache\Tree\Microsoft\Windows\*\*
        ValueName: Id
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT SvcHost
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\SvcHost
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT Terminal Server Run
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Terminal Server\install\Software\Microsoft\Windows\CurrentVersion\Run
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT Terminal Server Runonce
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Terminal Server\install\Software\Microsoft\Windows\CurrentVersion\Runonce
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT Terminal Server Runonceex
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Terminal Server\install\Software\Microsoft\Windows\CurrentVersion\Runonceex
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT OsImagesFolder
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Virtualization\LayerRootLocations\OsImagesFolder
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT CurrentVersion Windows
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Windows
        ValueName: AppInit*
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT CurrentVersion Windows
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Windows
        ValueName: RequireSignedAppInit*
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT CurrentVersion Windows
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Windows
        ValueName: Load
        Recursive: true
        Comment:
    -
        Description: Microsoft Windows NT CurrentVersion Windows
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Windows
        ValueName: Run
        Recursive: true
        Comment:
    -
        Description: Winlogon GinaDLL
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: Ginadll
        Recursive: false
        Comment:
    -
        Description: Winlogon DefaultUserName
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: DefaultUserName
        Recursive: false
        Comment:
    -
        Description: Winlogon Userinit
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: Userinit
        Recursive: false
        Comment:
    -
        Description: Winlogon VMApplet
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: VMApplet
        Recursive: false
        Comment:
    -
        Description: Winlogon AppSetup
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: AppSetup
        Recursive: false
        Comment:
    -
        Description: Winlogon Shell
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: Shell
        Recursive: false
        Comment:
    -
        Description: Winlogon SYSTEM
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: SYSTEM
        Recursive: false
        Comment:
    -
        Description: Winlogon UIHOST
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: UIHOST
        Recursive: false
        Comment:
    -
        Description: Winlogon Taskman
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon
        ValueName: Taskman
        Recursive: false
        Comment:
    -
        Description: Winlogon AlternateShells AvailableShells
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon\AlternateShells\AvailableShells
        Recursive: true
        Comment:
    -
        Description: Winlogon GPExtensions
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon\GPExtensions\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Winlogon GPExtensions
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon\GPExtensions\{*}
        ValueName: dllname
        Recursive: true
        Comment:
    -
        Description: Winlogon Notify
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Winlogon\Notify\*
        ValueName: dllname
        Recursive: true
        Comment:
    -
        Description: Authentication Credential Provider Filters
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Authentication\Credential Provider Filters\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Authentication Credential Providers
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Authentication\Credential Providers\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Authentication PLAP Providers
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Authentication\PLAP Providers\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Explorer Browser Helper Objects
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Explorer FindExtensions
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Explorer\FindExtensions\*
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Explorer FindExtensions Static
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Explorer\FindExtensions\Static
        Recursive: true
        Comment:
    -
        Description: Explorer SharedTaskScheduler
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Explorer\SharedTaskScheduler
        Recursive: true
        Comment:
    -
        Description: Explorer ShellExecuteHooks
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Explorer\ShellExecuteHooks
        Recursive: true
        Comment:
    -
        Description: Explorer ShellIconOverlayIdentifiers\*
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Explorer\ShellIconOverlayIdentifiers\*
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Microsoft\Windows\CurrentVersion\Explorer\ShellServiceObjects\{*}
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Explorer\ShellServiceObjects\{*}
        ValueName: autostart
        Recursive: true
        Comment:
    -
        Description: Ext PreApproved
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Ext\PreApproved\{*}
        ValueName: (default)
        Recursive: true
        Comment:
    -
        Description: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Shutdown\*
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Shutdown\*
        Recursive: true
        Comment:
    -
        Description: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Shutdown\*\*
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Shutdown\*\*
        Recursive: true
        Comment:
    -
        Description: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Startup\*
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Startup\*
        Recursive: true
        Comment:
    -
        Description: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Startup\*\*
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Group Policy\Scripts\Startup\*\*
        Recursive: true
        Comment:
    -
        Description: Internet Settings
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Internet Settings
        ValueName: AutoConfigURL
        Recursive: false
        Comment:
    -
        Description: Explorer Run
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Policies\Explorer\Run
        Recursive: true
        Comment:
    -
        Description: Policies System
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Policies\System
        ValueName: Shell
        Recursive: true
        Comment:
    -
        Description: Policies System
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Policies\System
        ValueName: UIHOST
        Recursive: true
        Comment:
    -
        Description: Policies System
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Policies\System
        ValueName: Userinit
        Recursive: true
        Comment:
    -
        Description: Run
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Run
        Recursive: true
        Comment:
    -
        Description: RunOnce
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Runonce
        Recursive: true
        Comment:
    -
        Description: RunOnceEx
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\RunOnceEx
        Recursive: true
        Comment:
    -
        Description: RunServices
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\RunServices
        Recursive: true
        Comment:
    -
        Description: RunServicesOnce
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\RunServicesOnce
        Recursive: true
        Comment:
    -
        Description: SharedDLLs
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Shareddlls
        Recursive: true
        Comment:
    -
        Description: Shell Extensions Approved
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Shell Extensions\Approved
        Recursive: true
        Comment:
    -
        Description: ShellServiceObjectDelayLoad
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\ShellServiceObjectDelayLoad
        Recursive: true
        Comment:
    -
        Description: Installed SDB
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\{*}.sdb
        ValueName: InstallDate
        Recursive: true
        Comment:
    -
        Description: Installed SDB
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\{*}.sdb
        ValueName: InstallLocation
        Recursive: true
        Comment:
    -
        Description: Installed SDB
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\{*}.sdb
        ValueName: InstallSource
        Recursive: true
        Comment:
    -
        Description: Installed SDB
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\{*}.sdb
        ValueName: DisplayName
        Recursive: true
        Comment:
    -
        Description: Installed SDB
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows\CurrentVersion\Uninstall\{*}.sdb
        ValueName: UninstallString
        Recursive: true
        Comment:
    -
        Description: Mozilla Components
        HiveType: Software
        Category: ASEP
        KeyPath: Mozilla\*\Components
        Recursive: true
        Comment:
    -
        Description: Mozilla Extensions Components
        HiveType: Software
        Category: ASEP
        KeyPath: Mozilla\*\Extensions\Components
        Recursive: true
        Comment:
    -
        Description: Mozilla Extensions Plugins
        HiveType: Software
        Category: ASEP
        KeyPath: Mozilla\*\Extensions\Plugins
        Recursive: true
        Comment:
    -
        Description: Mozilla Plugins
        HiveType: Software
        Category: ASEP
        KeyPath: Mozilla\*\Plugins
        Recursive: true
        Comment:
    -
        Description: Mozilla Install Directory
        HiveType: Software
        Category: ASEP
        KeyPath: Mozilla\Mozilla Firefox\*\Main\Install Directory
        Recursive: true
        Comment:
    -
        Description: MozillaPlugins Path
        HiveType: Software
        Category: ASEP
        KeyPath: MozillaPlugins\*\Path
        Recursive: true
        Comment:
    -
        Description: Policies\Microsoft\Windows\System\Scripts\Logoff
        HiveType: Software
        Category: ASEP
        KeyPath: Policies\Microsoft\Windows\System\Scripts\Logoff
        ValueName: Script
        Recursive: true
        Comment:
    -
        Description: Policies\Microsoft\Windows\System\Scripts\Logon
        HiveType: Software
        Category: ASEP
        KeyPath: Policies\Microsoft\Windows\System\Scripts\Logon
        ValueName: Script
        Recursive: true
        Comment:
    -
        Description: Policies\Microsoft\Windows\System\Scripts\Shutdown
        HiveType: Software
        Category: ASEP
        KeyPath: Policies\Microsoft\Windows\System\Scripts\Shutdown
        ValueName: Script
        Recursive: true
        Comment:
    -
        Description: Policies\Microsoft\Windows\System\Scripts\Startup
        HiveType: Software
        Category: ASEP
        KeyPath: Policies\Microsoft\Windows\System\Scripts\Startup
        ValueName: Script
        Recursive: true
        Comment:
    -
        Description: SilentProcessExit
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\SilentProcessExit\*
        ValueName: ReportingMode
        Recursive: true
        Comment:
    -
        Description: SilentProcessExit
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\SilentProcessExit\*
        ValueName: MonitorProcess
        Recursive: true
        Comment:
    -
        Description: Microsoft\Windows NT\CurrentVersion\Windows
        HiveType: Software
        Category: ASEP
        KeyPath: Microsoft\Windows NT\CurrentVersion\Windows
        ValueName: IconServiceLib
        Recursive: false
        Comment:
