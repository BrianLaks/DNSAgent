#define MyAppName "DNS Agent"
#define MyAppVersion "2.4.3"
#define MyAppPublisher "Brian Laks"
#define MyAppURL "https://github.com/BrianLaks/DNSAgent"
#define MyAppExeName "DNSAgent.Service.exe"
#define MyServiceName "DNSAgent"

[Setup]
AppId={{8F4A5B2C-9D3E-4F1A-8B6C-7E9F0A1B2C3D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
LicenseFile=..\LICENSE.txt
OutputDir=..\Release
OutputBaseFilename=DNSAgent-Setup-v{#MyAppVersion}
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
SetupIconFile=..\DNSAgent.Tray\icon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startservice"; Description: "Start DNS Agent service after installation"; GroupDescription: "Service Options:"; Flags: checkedonce

[Files]
; Core application files from the staging dist folder
Source: "..\_BuildStaging\Dist\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
; Specific handling for the database to avoid overwriting user data
Source: "..\_BuildStaging\Dist\dnsagent.db"; DestDir: "{app}"; Flags: uninsneveruninstall onlyifdoesntexist

[Icons]
Name: "{group}\DNS Agent Dashboard"; Filename: "http://localhost:5123"; IconFilename: "{app}\{#MyAppExeName}"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\DNS Agent Dashboard"; Filename: "http://localhost:5123"; IconFilename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; Stop existing service just in case
Filename: "{sys}\sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden
; Install and configure the service
Filename: "{sys}\sc.exe"; Parameters: "create {#MyServiceName} binPath= ""{app}\{#MyAppExeName}"" start= auto DisplayName= ""DNS Agent - Network Ad Blocker"""; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "description {#MyServiceName} ""DNS-based advertisement and tracking blocker with web management interface"""; Flags: runhidden
; Start the service
Filename: "{sys}\sc.exe"; Parameters: "start {#MyServiceName}"; Flags: runhidden; Tasks: startservice
; Open dashboard
Filename: "http://localhost:5123"; Description: "Open DNS Agent Dashboard"; Flags: postinstall shellexec skipifsilent

[UninstallRun]
; Stop and remove the service
Filename: "{sys}\sc.exe"; Parameters: "stop {#MyServiceName}"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "delete {#MyServiceName}"; Flags: runhidden

[Code]
function InitializeSetup(): Boolean;
var
  ResultCode: Integer;
begin
  // Check if .NET 9.0 is installed
  if not RegKeyExists(HKLM, 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedhost\9.0') then
  begin
    if MsgBox('.NET 9.0 Runtime is required but not installed.' + #13#10 + #13#10 + 
              'Would you like to download it now?', mbConfirmation, MB_YESNO) = IDYES then
    begin
      ShellExec('open', 'https://dotnet.microsoft.com/download/dotnet/9.0', '', '', SW_SHOW, ewNoWait, ResultCode);
    end;
    Result := False;
  end
  else
    Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssPostInstall then
  begin
    // Configure Windows Firewall
    Exec('netsh', 'advfirewall firewall add rule name="DNS Agent - DNS" dir=in action=allow protocol=UDP localport=53', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('netsh', 'advfirewall firewall add rule name="DNS Agent - Web UI" dir=in action=allow protocol=TCP localport=5123', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    
    // Set Working Directory for Service (CRITICAL for SQLite and Static Files)
    RegWriteStringValue(HKLM, 'SYSTEM\CurrentControlSet\Services\' + '{#MyServiceName}', 'WorkingDirectory', ExpandConstant('{app}'));
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usPostUninstall then
  begin
    // Remove firewall rules
    Exec('netsh', 'advfirewall firewall delete rule name="DNS Agent - DNS"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Exec('netsh', 'advfirewall firewall delete rule name="DNS Agent - Web UI"', '', SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;
