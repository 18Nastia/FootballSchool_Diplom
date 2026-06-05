; FootballSchool Setup - Inno Setup Script

[Setup]
AppName=FootballSchool
AppVersion=1.0
DefaultDirName={pf}\FootballSchool
DefaultGroupName=FootballSchool
OutputBaseFilename=FootballSchoolSetup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64

[Files]
; Копируем все необходимые файлы проекта
Source: "InstallerFiles\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Run]
; Запуск PowerShell скрипта установки с окнами уведомлений
Filename: "powershell.exe"; \
  Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\install.ps1"""; \
  Flags: runascurrentuser waituntilterminated

[Icons]
Name: "{group}\FootballSchool"; Filename: "{app}\FootballSchool.exe"