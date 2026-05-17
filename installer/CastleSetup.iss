#define MyAppName "Castle"
#define MyAppVersion "1.0.1"
#define MyAppPublisher "ozzydotjs"
#define MyAppURL "https://github.com/ozzydotjs/Castle"
#define MyAppExeName "Castle.exe"
#define MyAppIconName "skull-logo.ico"

[Setup]
AppId={{9F2523C1-5D65-4F85-9A58-CA57E0000001}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}

DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes

OutputDir=Output
OutputBaseFilename=CastleSetup-{#MyAppVersion}

SetupIconFile=..\Castle\wwwroot\skull-logo.ico
UninstallDisplayIcon={app}\skull-logo.ico

Compression=lzma
SolidCompression=yes
WizardStyle=modern
WizardSizePercent=110

ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
PrivilegesRequired=lowest

CloseApplications=yes
RestartApplications=no
ShowLanguageDialog=no

VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription=Castle Music Player Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Messages]
WelcomeLabel1=Welcome to Castle Setup
WelcomeLabel2=This will install Castle, a desktop music player for local playback, playlists, lyrics, downloads, and library management.%n%nClick Next to continue.
FinishedHeadingLabel=Castle has been installed
FinishedLabel=Castle is ready. Launch the app and start building your library.

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "..\publish\windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\publish\windows\Castle.Updater.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Castle\wwwroot\skull-logo.ico"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\skull-logo.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\skull-logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Castle"; Flags: nowait postinstall skipifsilent