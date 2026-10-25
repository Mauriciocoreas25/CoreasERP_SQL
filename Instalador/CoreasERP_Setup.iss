; Script de Inno Setup Profesional para Coreas ERP
; Genera un instalador formal para Windows (.exe) con desinstalador registrado en el Panel de Control

#define MyAppName "Coreas ERP"
#define MyAppVersion "1.1.0"
#define MyAppPublisher "Coreas ERP"
#define MyAppExeName "CoreasERP.exe"
#define MySourceDir "..\CoreasERP\bin\Release\net9.0-windows10.0.19041.0\win10-x64\publish"

[Setup]
AppId={{D35F32B9-72E0-4BF8-809F-11FE7326B22A}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputBaseFilename=CoreasERP_Instalador_Oficial
OutputDir=..\..\
SetupIconFile={#MySourceDir}\appicon.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=commandline dialog
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
; Acceso directo en el Menú Inicio (automático)
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\appicon.ico"; WorkingDir: "{app}"
; Acceso directo en el Escritorio (se crea SIEMPRE automáticamente)
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\appicon.ico"; WorkingDir: "{app}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Ejecutar {#MyAppName} ahora"; Flags: nowait postinstall skipifsilent
