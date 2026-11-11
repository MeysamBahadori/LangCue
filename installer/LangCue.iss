; LangCue 0.2.0-preview.2 - Windows x64 per-user installer
#define AppName "LangCue"
#define AppVersion "0.2.0-preview.2"
#define PublishDir "..\artifacts\publish\win-x64"

[Setup]
AppId={{B7F23A39-EB1E-55BF-95CC-DB86AB4C7150}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher=LangCue Contributors
DefaultDirName={localappdata}\Programs\LangCue
DefaultGroupName=LangCue
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\LangCue.exe
SetupIconFile=..\assets\LangCue.ico
OutputDir=..\artifacts\installer
OutputBaseFilename=LangCue-Setup-{#AppVersion}-win-x64
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Tasks]
Name: "autostart"; Description: "Start LangCue when I sign in"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{userprograms}\LangCue"; Filename: "{app}\LangCue.exe"
Name: "{userstartup}\LangCue"; Filename: "{app}\LangCue.exe"; Tasks: autostart

[Run]
Filename: "{app}\LangCue.exe"; Description: "Start LangCue"; Flags: nowait postinstall skipifsilent
