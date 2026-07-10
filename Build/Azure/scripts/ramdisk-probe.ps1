# RAM disk viability probe (diagnostic only) -- see actions/runner-images#7193.
#
# Question: on the Azure hosted Windows agent, can we create a RAM-backed, SqlCe-usable,
# standard-volume disk using ONLY built-in Windows features (iSCSI Target Server 'ramdisk:'
# provider + inbox loopback initiator), with NO third-party kernel driver / cert-trust?
#
# This is the one property set the shelved attempts could not jointly satisfy:
#   - AIM  gave a real volume but needs a blocked kernel-driver install.
#   - ImDisk installs silently but presents a raw \Device\ImDiskN\ reparse with no volume
#     GUID -> SqlCe rejects it (NativeError 28611).
# iSCSI surfaces a real SCSI disk (volume GUID) using only MS-signed inbox components.
#
# Diagnostic ONLY: wrapped in try/catch + always `exit 0`, and gated `continueOnError` in
# YAML, so it can never fail the SqlCE leg. Read the "RAMDISK PROBE SUMMARY" block in the log.

$ErrorActionPreference = 'Continue'
$results = [ordered]@{}
function Note($k, $v) { $script:results[$k] = $v; Write-Host ("[probe] {0}: {1}" -f $k, $v) }

$sw          = [System.Diagnostics.Stopwatch]::StartNew()
$vdiskPath   = 'ramdisk:l2dbprobe.vhdx'
$targetName  = 'l2dbramprobe'
$sizeBytes   = 256MB
$tgtNode     = $null
$connected   = $false
$driveLetter = $null

# 1. Environment (correlate viability with the agent's VM config -- issue reports
#    New-IscsiVirtualDisk fails on 10.* + dual-drive configs, works on 192.* + single-VHD)
try {
    $os = Get-CimInstance Win32_OperatingSystem
    Note 'os_caption'   $os.Caption
    Note 'os_version'   $os.Version
    Note 'free_ram_mb'  ([int]($os.FreePhysicalMemory / 1024))
    $ipv4 = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
             Where-Object { $_.IPAddress -ne '127.0.0.1' } |
             Select-Object -ExpandProperty IPAddress) -join ','
    Note 'ipv4'         $ipv4
    Note 'disk_count'   ((Get-Disk -ErrorAction SilentlyContinue | Measure-Object).Count)
}
catch { Note 'env_error' $_.Exception.Message }

try {
    # 2. iSCSI Target Server role (Windows Server feature; hosted agents are Server SKUs)
    $t0 = $sw.Elapsed
    $feat = Get-WindowsFeature -Name FS-iSCSITarget-Server -ErrorAction SilentlyContinue
    if ($null -eq $feat) {
        Note 'iscsi_target_feature' 'ABSENT (Get-WindowsFeature returned null)'
    }
    else {
        Note 'iscsi_target_installed_before' $feat.Installed
        if (-not $feat.Installed) {
            $inst = Install-WindowsFeature -Name FS-iSCSITarget-Server -ErrorAction Stop
            Note 'iscsi_target_install_success' $inst.Success
            Note 'iscsi_target_restart_needed'  $inst.RestartNeeded
        }
    }
    Note 'iscsi_target_install_secs' ([math]::Round(($sw.Elapsed - $t0).TotalSeconds, 1))

    # MS iSCSI Target Server refuses loopback sessions to its own targets by default
    # (documented limitation -- the "Connection Failed" the last two probes hit at connect).
    # AllowLoopBack overrides it; the flag is read at service start, so set it BEFORE starting
    # WinTarget and restart the service so a fresh-installed/auto-started instance honors it.
    try {
        New-Item -Path 'HKLM:\Software\Microsoft\iSCSI Target' -Force -ErrorAction SilentlyContinue | Out-Null
        Set-ItemProperty -Path 'HKLM:\Software\Microsoft\iSCSI Target' -Name 'AllowLoopBack' -Value 1 -Type DWord -ErrorAction Stop
        Note 'allow_loopback_set' 'OK'
    }
    catch { Note 'allow_loopback_set' "skip: $($_.Exception.Message)" }

    Restart-Service -Name WinTarget -Force -ErrorAction SilentlyContinue   # re-read AllowLoopBack if already up
    Start-Service -Name WinTarget -ErrorAction SilentlyContinue     # iSCSI Target Server
    Set-Service   -Name MSiSCSI -StartupType Automatic -ErrorAction SilentlyContinue
    Start-Service -Name MSiSCSI -ErrorAction SilentlyContinue       # initiator
    Note 'wintarget_status' (Get-Service WinTarget -ErrorAction SilentlyContinue).Status
    Note 'msiscsi_status'   (Get-Service MSiSCSI   -ErrorAction SilentlyContinue).Status
    Import-Module IscsiTarget -ErrorAction SilentlyContinue

    # 3. RAM-backed virtual disk -- the failure point the issue flags on some Azure configs
    $t1 = $sw.Elapsed
    New-IscsiVirtualDisk -Path $vdiskPath -Size $sizeBytes -ErrorAction Stop | Out-Null
    Note 'vdisk_create'      'OK'
    Note 'vdisk_create_secs' ([math]::Round(($sw.Elapsed - $t1).TotalSeconds, 1))

    # 4. target + mapping (allow the local initiator)
    $iqn = Get-InitiatorPort -ErrorAction Stop | Select-Object -First 1 -ExpandProperty NodeAddress
    Note 'initiator_iqn' $iqn
    New-IscsiServerTarget -TargetName $targetName -InitiatorIds @("Iqn:$iqn") -ErrorAction Stop | Out-Null
    Add-IscsiVirtualDiskTargetMapping -TargetName $targetName -Path $vdiskPath -ErrorAction Stop
    Note 'target_map' 'OK'

    # 4b. Firewall prerequisite -- Server 2025 ships the 'iSCSI Service' firewall group disabled,
    #     so the loopback connect to 127.0.0.1:3260 is refused (the "Connection Failed" the first
    #     probe hit at Connect-IscsiTarget). Enable it before connecting.
    try   { Enable-NetFirewallRule -DisplayGroup 'iSCSI Service' -ErrorAction Stop; Note 'fw_iscsi_enabled' 'OK' }
    catch { Note 'fw_iscsi_enabled' "skip: $($_.Exception.Message)" }

    # 5. connect -- try loopback first (now that AllowLoopBack is set), then fall back to the
    #    agent's private 10.x interface IP (the IP is per-run, so discover it, never hardcode).
    $portals = @('127.0.0.1')
    $privIp  = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
               Where-Object { $_.IPAddress -like '10.*' } |
               Select-Object -First 1 -ExpandProperty IPAddress
    if ($privIp) { $portals += $privIp }

    foreach ($portal in $portals) {
        try {
            New-IscsiTargetPortal -TargetPortalAddress $portal -TargetPortalPortNumber 3260 -ErrorAction Stop | Out-Null
            Update-IscsiTargetPortal -TargetPortalAddress $portal -ErrorAction SilentlyContinue
            $tgt = Get-IscsiTarget -ErrorAction Stop | Where-Object { $_.NodeAddress -like "*$targetName*" } | Select-Object -First 1
            if ($null -eq $tgt) { throw 'target not discovered' }
            $tgtNode = $tgt.NodeAddress
            Connect-IscsiTarget -NodeAddress $tgtNode -IsPersistent $false -ErrorAction Stop | Out-Null
            $connected = $true
            Note 'iscsi_connect_via' $portal
            break
        }
        catch {
            Note "connect_fail_$portal" $_.Exception.Message
            Remove-IscsiTargetPortal -TargetPortalAddress $portal -Confirm:$false -ErrorAction SilentlyContinue
        }
    }
    if (-not $connected) { throw 'all portals failed to connect' }
    Start-Sleep -Seconds 3

    # 6. bring disk online + format NTFS
    $disk = Get-Disk -ErrorAction Stop | Where-Object { $_.BusType -eq 'iSCSI' } | Select-Object -First 1
    if ($null -eq $disk) { throw 'no iSCSI disk surfaced' }
    Note 'disk_number' $disk.Number
    if ($disk.IsOffline) { Set-Disk -Number $disk.Number -IsOffline $false }
    Set-Disk       -Number $disk.Number -IsReadOnly $false -ErrorAction SilentlyContinue
    Initialize-Disk -Number $disk.Number -PartitionStyle MBR -ErrorAction Stop
    $part = New-Partition -DiskNumber $disk.Number -UseMaximumSize -AssignDriveLetter -ErrorAction Stop
    $driveLetter = $part.DriveLetter
    Format-Volume -DriveLetter $driveLetter -FileSystem NTFS -Confirm:$false -Force -ErrorAction Stop | Out-Null
    Note 'drive_letter' $driveLetter

    # 7. standard-volume GUID check -- the property ImDisk lacked (SqlCe-critical)
    $vol = Get-Volume -DriveLetter $driveLetter -ErrorAction SilentlyContinue
    Note 'volume_guid_present' ([bool]$vol.UniqueId)
    Note 'volume_unique_id'    $vol.UniqueId

    # 8. SqlCe acceptance test -- the make-or-break (ImDisk failed here w/ NativeError 28611)
    & "$PSScriptRoot/sqlce.ps1" | Out-Null
    $ssce = 'C:\Program Files\Microsoft SQL Server Compact Edition\v4.0\Desktop\System.Data.SqlServerCe.dll'
    if (-not (Test-Path $ssce)) { throw "SqlCe assembly not found at $ssce" }
    Add-Type -Path $ssce
    $sdf = "${driveLetter}:\probe.sdf"
    $cs  = "Data Source=$sdf"
    try {
        $engine = New-Object System.Data.SqlServerCe.SqlCeEngine($cs)
        $engine.CreateDatabase(); $engine.Dispose()
        $conn = New-Object System.Data.SqlServerCe.SqlCeConnection($cs)
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandText = 'CREATE TABLE T (Id INT)';       [void]$cmd.ExecuteNonQuery()
        $cmd.CommandText = 'INSERT INTO T (Id) VALUES (1)'; [void]$cmd.ExecuteNonQuery()
        $cmd.CommandText = 'SELECT COUNT(*) FROM T';        $n = $cmd.ExecuteScalar()
        $conn.Close()
        Note 'sqlce_on_ramdisk' "OK (count=$n)"
    }
    catch [System.Data.SqlServerCe.SqlCeException] {
        $ne = if ($_.Exception.Errors.Count) { $_.Exception.Errors[0].NativeError } else { '?' }
        Note 'sqlce_on_ramdisk' "FAIL NativeError=$ne : $($_.Exception.Message)"
    }
}
catch {
    Note 'FATAL' $_.Exception.Message
}
finally {
    try { if ($connected -and $tgtNode) { Disconnect-IscsiTarget -NodeAddress $tgtNode -Confirm:$false -ErrorAction SilentlyContinue } } catch {}
    try { Remove-IscsiServerTarget -TargetName $targetName -ErrorAction SilentlyContinue } catch {}
    try { Remove-IscsiVirtualDisk  -Path $vdiskPath        -ErrorAction SilentlyContinue } catch {}
}

Note 'total_secs' ([math]::Round($sw.Elapsed.TotalSeconds, 1))
Write-Host ''
Write-Host '==== RAMDISK PROBE SUMMARY ===='
$results.GetEnumerator() | ForEach-Object { Write-Host ("{0,-28} = {1}" -f $_.Key, $_.Value) }
Write-Host '==============================='
exit 0   # diagnostic only -- never fail the leg
