# --- 32/64 bit elevation ---
if ($PSVersionTable.Platform -eq "Win32NT" -and [Environment]::Is64BitOperatingSystem -and -not [Environment]::Is64BitProcess) {
    $sysnative = Join-Path $env:WINDIR "sysnative\WindowsPowerShell\v1.0\powershell.exe"
    if (Test-Path $sysnative) {
        $process = Start-Process $sysnative -ArgumentList "-STA -ExecutionPolicy Bypass -NoProfile -File `"$PSCommandPath`"" -Wait -PassThru
        exit $process.ExitCode
    }
}

Add-Type -AssemblyName System.Windows.Forms
Set-Location -Path $PSScriptRoot

$logPath = "C:\FootballSchool-install-log.txt"
Start-Transcript -Path $logPath -Append

function Show-Info($text,$title="FootballSchool Setup") {[System.Windows.Forms.MessageBox]::Show($text,$title,"OK","Information") | Out-Null}
function Show-Warning($text,$title="FootballSchool Setup") {[System.Windows.Forms.MessageBox]::Show($text,$title,"OK","Warning") | Out-Null}
function Show-Error($text,$title="FootballSchool Setup") {[System.Windows.Forms.MessageBox]::Show($text,$title,"OK","Error") | Out-Null}
function Ask-Restart { if ([System.Windows.Forms.MessageBox]::Show("Для корректного продолжения установки требуется перезагрузка.`nНажмите ДА для перезагрузки.","Требуется перезагрузка",[System.Windows.Forms.MessageBoxButtons]::YesNo,[System.Windows.Forms.MessageBoxIcon]::Warning) -eq [System.Windows.Forms.DialogResult]::Yes) { Restart-Computer -Force } else { exit 3010 } }

function Execute-SqlFile($server,$database,$filePath){
    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection "Server=$server;Database=$database;Integrated Security=True;TrustServerCertificate=True;"
    $connection.Open()
    try{
        $sql = Get-Content $filePath -Raw
        $batches = [regex]::Split($sql,"^\s*GO\s*$",[System.Text.RegularExpressions.RegexOptions]::Multiline -bor [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        foreach ($batch in $batches){
            if ($batch.Trim().Length -gt 0){
                $cmd = $connection.CreateCommand()
                $cmd.CommandText = $batch
                $cmd.CommandTimeout = 300
                $cmd.ExecuteNonQuery() | Out-Null
            }
        }
    } finally { $connection.Close() }
}

function Execute-SqlCommand($server,$database,$query){
    Add-Type -AssemblyName System.Data
    $connection = New-Object System.Data.SqlClient.SqlConnection "Server=$server;Database=$database;Integrated Security=True;TrustServerCertificate=True;"
    $connection.Open()
    try{
        $cmd = $connection.CreateCommand()
        $cmd.CommandText = $query
        $cmd.CommandTimeout = 300
        $cmd.ExecuteNonQuery() | Out-Null
    } finally { $connection.Close() }
}

try {
    $siteName="FootballSchool"
    $appPath="C:\inetpub\FootballSchool"
    $port=5000
    $dbName="FootballSchool"
    $sqlInstance="SQLEXPRESS"
    $sqlServer=".\$sqlInstance"
    $sqlInstaller="$PSScriptRoot\Prerequisites\SQL2025-SSEI-Expr.exe"
    $hostingInstaller="$PSScriptRoot\Prerequisites\dotnet-hosting.exe"
    $publishPath="$PSScriptRoot\publish"
    $dbFile="$PSScriptRoot\database.sql"

    Show-Info "Начало установки FootballSchool."

    if (-not (Test-Path $publishPath)) { throw "Папка publish не найдена." }
    if (-not (Test-Path $dbFile)) { throw "Файл database.sql не найден." }

    # --- SQL Server 2025 Express silent install ---
    Show-Info "Установка SQL Server 2025 Express..."
    $sqlConfigJson = @"
{
  "Action":"Install",
  "Features":"SQLENGINE",
  "InstanceName":"$sqlInstance",
  "TCPENABLED":1,
  "NPENABLED":1,
  "SQLSVCACCOUNT":"NT AUTHORITY\NETWORK SERVICE",
  "SQLSYSADMINACCOUNTS":"BUILTIN\ADMINISTRATORS",
  "IAcceptSQLServerLicenseTerms":true,
  "Quiet":true,
  "UpdateEnabled":false
}
"@
    $sqlConfigFile="$PSScriptRoot\SQLConfig.json"
    $sqlConfigJson | Out-File -FilePath $sqlConfigFile -Encoding ASCII
    $process = Start-Process -FilePath $sqlInstaller -ArgumentList "/ConfigurationFile=$sqlConfigFile" -Wait -PassThru
    if ($process.ExitCode -ne 0 -and $process.ExitCode -ne 3010) { throw "Ошибка установки SQL Server: ExitCode $($process.ExitCode)" }
    Start-Sleep -Seconds 15
    Start-Service -Name "MSSQL`$$sqlInstance" -ErrorAction SilentlyContinue

    # --- .NET Hosting Bundle ---
    Show-Info "Установка .NET Hosting Bundle..."
    $hostingArgs="/install /quiet /norestart"
    Start-Process -FilePath $hostingInstaller -ArgumentList $hostingArgs -Wait -PassThru

    # --- Copy files ---
    Show-Info "Копирование файлов..."
    if (-not (Test-Path $appPath)) { New-Item -ItemType Directory -Force -Path $appPath | Out-Null }
    Copy-Item -Path "$publishPath\*" -Destination $appPath -Recurse -Force

    # --- Configure appsettings.json ---
    $configPath="$appPath\appsettings.json"
    $json = Get-Content $configPath -Raw | ConvertFrom-Json
    $json.ConnectionStrings.DefaultConnection="Server=$sqlServer;Database=$dbName;Trusted_Connection=True;TrustServerCertificate=True;"
    $json | ConvertTo-Json -Depth 10 | Set-Content -Path $configPath -Encoding UTF8

    # --- IIS Setup ---
    Show-Info "Настройка IIS..."
    Import-Module WebAdministration
    if (-not (Test-Path IIS:\AppPools\$siteName)) { New-WebAppPool -Name $siteName }
    Set-ItemProperty IIS:\AppPools\$siteName -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty IIS:\AppPools\$siteName -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
    if (-not (Get-Website -Name $siteName -ErrorAction SilentlyContinue)) {
        New-Website -Name $siteName -Port $port -PhysicalPath $appPath -ApplicationPool $siteName
    }

    # --- Create database ---
    Show-Info "Создание базы данных..."
    Execute-SqlFile -server $sqlServer -database "master" -filePath $dbFile

    # --- Permissions ---
    icacls "$appPath" /grant "IIS AppPool\$siteName:(OI)(CI)F" /T /C /Q | Out-Null
    icacls "$appPath" /grant "IIS_IUSRS:(OI)(CI)F" /T /C /Q | Out-Null
    netsh advfirewall firewall add rule name="FootballSchool Web" dir=in action=allow protocol=TCP localport=$port

    iisreset | Out-Null
    Show-Info "Установка FootballSchool завершена. Сайт доступен по адресу: http://localhost:$port"

} catch {
    Show-Error "Произошла ошибка при установке.`nПричина:`n$($_.Exception.Message)"
} finally {
    Stop-Transcript
}