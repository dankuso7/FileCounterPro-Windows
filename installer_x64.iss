[Setup]
AppName=FileCounterPro
AppVersion=1.0
DefaultDirName={autopf}\FileCounterPro
DefaultGroupName=FileCounterPro
OutputDir=.\Installer
OutputBaseFilename=FileCounterPro_Setup_x64
Compression=lzma
SolidCompression=yes
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64
DisableProgramGroupPage=yes

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Files]
Source: ".\publish-x64\FileCounterPro.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\FileCounterPro"; Filename: "{app}\FileCounterPro.exe"
Name: "{autodesktop}\FileCounterPro"; Filename: "{app}\FileCounterPro.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\FileCounterPro.exe"; Description: "Launch FileCounterPro"; Flags: nowait postinstall skipifsilent
