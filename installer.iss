[Setup]
AppName=FileCounter Pro
AppVersion=2.0
DefaultDirName={autopf}\FileCounterPro
DefaultGroupName=FileCounter Pro
OutputDir=Output
OutputBaseFilename=FileCounterPro_Setup
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "out\FileCounterPro-Windows-x64.exe"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\FileCounter Pro"; Filename: "{app}\FileCounterPro-Windows-x64.exe"
Name: "{group}\Uninstall FileCounter Pro"; Filename: "{uninstallexe}"
Name: "{autodesktop}\FileCounter Pro"; Filename: "{app}\FileCounterPro-Windows-x64.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\FileCounterPro-Windows-x64.exe"; Description: "Launch FileCounter Pro"; Flags: nowait postinstall skipifsilent
