[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
AppId={{CoffeeAwake-8473-A3B9-9023-FCA827D9A201}
AppName=CoffeeAwake
AppVersion=1.0
AppPublisher=Gabriel
AppPublisherURL=https://github.com/gasiso/CoffeeAwake
AppSupportURL=https://github.com/gasiso/CoffeeAwake
AppUpdatesURL=https://github.com/gasiso/CoffeeAwake
DefaultDirName={autopf}\CoffeeAwake
DisableProgramGroupPage=yes
; Remove the following line to run in administrative install mode (install for all users.)
PrivilegesRequired=lowest
OutputBaseFilename=CoffeeAwake_Setup_v1.0
Compression=lzma
SolidCompression=yes
WizardStyle=modern
OutputDir=Output

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Pegando o binário compilado pelo comando dotnet publish recomendado no README
Source: "publish\win-x64\CoffeeAwake.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "publish\win-x64\*.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist
; NOTE: Don't use "Flags: ignoreversion" on any shared system files

[Icons]
Name: "{autoprograms}\CoffeeAwake"; Filename: "{app}\CoffeeAwake.exe"
Name: "{autodesktop}\CoffeeAwake"; Filename: "{app}\CoffeeAwake.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\CoffeeAwake.exe"; Description: "{cm:LaunchProgram,CoffeeAwake}"; Flags: nowait postinstall skipifsilent
