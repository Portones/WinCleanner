; Script de compilación de Inno Setup para WinCleaner
; Descarga Inno Setup desde: https://jrsoftware.org/isdl.php
; Abre este archivo con Inno Setup y pulsa F9 para compilar el instalador único.

[Setup]
AppName=WinCleaner
AppVersion=1.1.0
AppPublisher=Rodrigo Portones
AppPublisherURL=https://github.com/RodrigoPortones/WinCleaner
DefaultDirName={commonpf}\WinCleaner
DefaultGroupName=WinCleaner
DisableProgramGroupPage=yes
; Icono del instalador y de desinstalación
SetupIconFile=Assets\WinCleanerLogo.ico
UninstallDisplayIcon={app}\WinCleanerLogo.ico
Compression=lzma
SolidCompression=yes
WizardStyle=modern
; OutputDir define dónde se guardará el instalador compilado (en la carpeta raíz del proyecto)
OutputDir=.
OutputBaseFilename=WinCleanerSetup
; Solicitar privilegios de administrador para realizar la instalación en archivos de programa
PrivilegesRequired=admin

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Copiar el ejecutable principal
Source: "bin\Release\net9.0-windows10.0.19041.0\win-x64\publish\WinCleaner.exe"; DestDir: "{app}"; Flags: ignoreversion
; Copiar el icono para usarlo en la desinstalación
Source: "Assets\WinCleanerLogo.ico"; DestDir: "{app}"; Flags: ignoreversion
; Copiar todos los archivos y DLLs de soporte del directorio de publicación
Source: "bin\Release\net9.0-windows10.0.19041.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\WinCleaner"; Filename: "{app}\WinCleaner.exe"
Name: "{autodesktop}\WinCleaner"; Filename: "{app}\WinCleaner.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\WinCleaner.exe"; Description: "{cm:LaunchProgram,WinCleaner}"; Flags: nowait postinstall skipifsilent
