$StartDate = (Get-Date).ToString("yyyy-MM-dd")

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

    # Clean up old tracking file so farewell popup shows fresh
    $PopupTrackPath = Join-Path $TargetFolder "LastPopupDate.txt"
    if (Test-Path -Path $PopupTrackPath) {
        Remove-Item -Path $PopupTrackPath -Force -ErrorAction SilentlyContinue
    }

    icacls $TargetFolder /grant "Users:(OI)(CI)F" /T | Out-Null

    # --- Scheduled Tasks ---

    # 1. Peekaboo Toast at 17:50 Mon-Fri
    $ToastAction = New-ScheduledTaskAction -Execute $ExePath -WorkingDirectory $TargetFolder
    $TriggerToast = New-ScheduledTaskTrigger -Weekly -DaysOfWeek Monday,Tuesday,Wednesday,Thursday,Friday -At "17:50"
    $ToastPrincipal = New-ScheduledTaskPrincipal -GroupId "S-1-5-32-545" -RunLevel Limited
    $ToastSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries
    Register-ScheduledTask -TaskName "good bye TATA (Toast)" -Action $ToastAction -Trigger $TriggerToast -Principal $ToastPrincipal -Settings $ToastSettings -Force

    # 2. Farewell Popup at Logon (one-time, tracked by app itself)
    $PopupAction = New-ScheduledTaskAction -Execute $ExePath -Argument "--startup" -WorkingDirectory $TargetFolder
    $TriggerLogon = New-ScheduledTaskTrigger -AtLogOn
    $PopupPrincipal = New-ScheduledTaskPrincipal -GroupId "S-1-5-32-545" -RunLevel Limited
    $PopupPrincipal.LogonType = "Interactive"
    $PopupSettings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    Register-ScheduledTask -TaskName "good bye TATA" -Action $PopupAction -Trigger $TriggerLogon -Principal $PopupPrincipal -Settings $PopupSettings -Force

    # 3. Clean up old tasks if they exist
    Unregister-ScheduledTask -TaskName "EnergySavingAlert" -Confirm:$false -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName "EnergySavingPopup_Logon" -Confirm:$false -ErrorAction SilentlyContinue
    Unregister-ScheduledTask -TaskName "EnergySavingPopup_Daily" -Confirm:$false -ErrorAction SilentlyContinue

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
