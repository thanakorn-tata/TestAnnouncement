$StartDate = "2026-06-17"

$ScriptPath = $PSScriptRoot
if (!$ScriptPath) { $ScriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path }
if (!$ScriptPath) { $ScriptPath = Get-Location }

$InnerZipName = "Announcement.zip" 
$InnerZipPath = Join-Path $ScriptPath $InnerZipName

$TargetFolder = "C:\Downloadpath\Notification"
$ExePath = Join-Path $TargetFolder "Announcement.exe"

try {
    Stop-Process -Name "Announcement" -Force -ErrorAction SilentlyContinue
    taskkill /F /IM "Announcement.exe" /T 2>$null
    Start-Sleep -Seconds 2

    if (!(Test-Path -Path $TargetFolder)) {
        New-Item -Path $TargetFolder -ItemType Directory -Force | Out-Null
    }

    if (Test-Path -Path $InnerZipPath) {
        Write-Host "Extracting $InnerZipName to $TargetFolder..."
        Expand-Archive -Path $InnerZipPath -DestinationPath $TargetFolder -Force
    }
    else {
        throw "Cannot find internal zip file at $InnerZipPath"
    }

    $StartDatePath = Join-Path $TargetFolder "StartDate.txt"
    if (Test-Path -Path $StartDatePath) {
        Remove-Item -Path $StartDatePath -Force -ErrorAction SilentlyContinue
    }
    Set-Content -Path $StartDatePath -Value $StartDate -Force

    icacls $TargetFolder /grant "Users:(OI)(CI)F" /T | Out-Null

    $TaskAction = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $TargetFolder
    $PopupAction = New-ScheduledTaskAction -Execute $ExePath -Argument "--startup" -WorkingDirectory $TargetFolder

    $TriggerNoon = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday,Tuesday,Wednesday,Thursday,Friday -At "12:00"
    $TriggerEvening = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday,Tuesday,Wednesday,Thursday,Friday -At "18:00"
    $TriggerLogon = New-ScheduledTaskTrigger -AtLogOn
    $TriggerDaily = New-ScheduledTaskTrigger -Daily -At "06:00"

    $AlertPrincipal = New-ScheduledTaskPrincipal -GroupId "S-1-5-32-545" -RunLevel Limited
    
    $PopupPrincipal = New-ScheduledTaskPrincipal -GroupId "S-1-5-32-545" -RunLevel Limited
    $PopupPrincipal.LogonType = "Interactive"

    $AlertSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    $PopupSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    
    Register-ScheduledTask -TaskName "EnergySavingAlert" -Action $TaskAction -Trigger @($TriggerNoon, $TriggerEvening) -Principal $AlertPrincipal -Settings $AlertSettings -Force
    Register-ScheduledTask -TaskName "EnergySavingPopup_Logon" -Action $PopupAction -Trigger $TriggerLogon -Principal $PopupPrincipal -Settings $PopupSettings -Force
    Register-ScheduledTask -TaskName "EnergySavingPopup_Daily" -Action $PopupAction -Trigger $TriggerDaily -Principal $PopupPrincipal -Settings $PopupSettings -Force

    Write-Host "Deployment Complete"
}
catch {
    $errMessage = "[$((Get-Date).ToString('yyyy-MM-dd HH:mm:ss'))] ERROR: $_"
    try {
        $errMessage | Out-File (Join-Path $ScriptPath "DeployError.txt") -Append
        if (Test-Path -Path $TargetFolder) {
            $errMessage | Out-File (Join-Path $TargetFolder "Debug_Error.txt")
        }
    }
    catch { }
    Write-Error $errMessage
    exit 1
}
