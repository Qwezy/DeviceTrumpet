; Inno Setup script for DeviceTrumpet.
;
; This produces a plain Win32 setup.exe from the unpackaged build output
; (EarTrumpet.csproj built directly, NOT through EarTrumpet.Package/MSIX),
; so there is no APPX/MSIX signing certificate to deal with. Inno Setup
; itself does not require the resulting installer to be code-signed either;
; it will just run as an ordinary unsigned Win32 program (Windows SmartScreen
; may warn once on first run for an unrecognized publisher, which is
; unrelated to package signing and can be dismissed via "More info" > "Run
; anyway").
;
; Prerequisites:
;   1. Install Inno Setup (free): https://jrsoftware.org/isinfo.php
;   2. Build the app in Release|x86 configuration first, e.g.:
;        msbuild EarTrumpet.vs15.sln /p:Configuration=Release /p:Platform=x86 /t:EarTrumpet
;      This produces Build\Release\EarTrumpet.exe (and its dependencies)
;      at the repo root.
;
; To build the installer:
;   - Open this file in the Inno Setup Compiler and click Compile, or
;   - Run from a command prompt: iscc Packaging\InnoSetup\DeviceTrumpet.iss
;   The output setup.exe is written to Packaging\InnoSetup\Output\.

#define MyAppName "DeviceTrumpet"
#define MyAppVersion "1.0.0"
#define MyAppExeName "EarTrumpet.exe"
#define MyBuildOutputDir "..\..\Build\Release"

[Setup]
AppId={{6E6E6E9E-6F7C-4C8E-9C4A-6A5C4E6B6E6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir=Output
OutputBaseFilename={#MyAppName}Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
SetupIconFile=..\..\EarTrumpet\Assets\Icon-Light.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked
Name: "startupicon"; Description: "Start {#MyAppName} automatically when you sign in"; GroupDescription: "Startup:"; Flags: unchecked

[Files]
; Copies the whole Release build output (EarTrumpet.exe, its DLLs, and the
; per-language satellite resource subfolders) except debug/build artifacts.
Source: "{#MyBuildOutputDir}\*"; DestDir: "{app}"; Excludes: "*.pdb,*.xml,*.vshost.*"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Registry]
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueType: string; ValueName: "{#MyAppName}"; ValueData: """{app}\{#MyAppExeName}"""; Flags: uninsdeletevalue; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
