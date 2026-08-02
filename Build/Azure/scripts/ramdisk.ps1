<#
.SYNOPSIS
Mount a RAM-backed, standard-volume NTFS disk on a Windows agent using only built-in Windows
features -- the iSCSI Target Server 'ramdisk:' provider plus the inbox loopback initiator --
with no third-party kernel driver or cert-trust. Emits the assigned drive letter.

.DESCRIPTION
Verified viable on the Azure hosted Windows agent (Server 2025): the resulting volume exposes a
\\?\Volume{GUID} and SqlCe reads/writes on it (see actions/runner-images#7193). Two non-obvious
requirements were needed to make the local iSCSI session connect:

  * HKLM:\Software\Microsoft\iSCSI Target\AllowLoopBack=1 -- read at WinTarget start, so it is set
    before the service is (re)started.
  * The initiator connects over the agent's PRIVATE interface IP (10.x), NOT 127.0.0.1: the target
    server refuses the loopback address even with AllowLoopBack set. The private-IP path traverses
    the real NIC, so the inbox 'iSCSI Service' firewall group is enabled too.

NOT enabled by default. The ~4 min FS-iSCSITarget-Server install cost outweighs the I/O gain for
the SqlCe test suite (measured: no net speedup), so the CI step that calls this is dormant -- gated
behind the `use_ramdisk` pipeline variable (undefined by default). Kept as ready-to-use
functionality for a future I/O-bound use case.

No teardown: hosted CI agents are ephemeral and discarded after the run.

.PARAMETER SizeBytes
Size of the RAM disk. Default 256MB.

.PARAMETER TargetName
iSCSI target / virtual-disk name. Default 'l2dbramdisk'.

.OUTPUTS
The assigned drive letter (e.g. 'E') on success. Throws on any failure.
#>
param(
    [long]  $SizeBytes  = 256MB,
    [string]$TargetName = 'l2dbramdisk'
)

$ErrorActionPreference = 'Stop'

$vdiskPath = "ramdisk:$TargetName.vhdx"

# iSCSI Target Server role (hosted agents are Server SKUs). Installing is the slow part (~4 min).
$feat = Get-WindowsFeature -Name FS-iSCSITarget-Server
if (-not $feat.Installed) {
    Install-WindowsFeature -Name FS-iSCSITarget-Server | Out-Null
}

# MS iSCSI Target refuses loopback sessions to its own targets by default; AllowLoopBack overrides
# it and is read at service start, so set it before (re)starting WinTarget.
New-Item -Path 'HKLM:\Software\Microsoft\iSCSI Target' -Force | Out-Null
Set-ItemProperty -Path 'HKLM:\Software\Microsoft\iSCSI Target' -Name 'AllowLoopBack' -Value 1 -Type DWord

Restart-Service -Name WinTarget -Force -ErrorAction SilentlyContinue   # re-read AllowLoopBack if already up
Start-Service   -Name WinTarget                                        # iSCSI Target Server
Set-Service     -Name MSiSCSI -StartupType Automatic
Start-Service   -Name MSiSCSI                                          # initiator
Import-Module IscsiTarget
Enable-NetFirewallRule -DisplayGroup 'iSCSI Service' -ErrorAction SilentlyContinue   # inbound 3260 for the private-IP path

# RAM-backed virtual disk + target mapping for the local initiator.
New-IscsiVirtualDisk -Path $vdiskPath -Size $SizeBytes | Out-Null
$iqn = Get-InitiatorPort | Select-Object -First 1 -ExpandProperty NodeAddress
New-IscsiServerTarget -TargetName $TargetName -InitiatorIds @("Iqn:$iqn") | Out-Null
Add-IscsiVirtualDiskTargetMapping -TargetName $TargetName -Path $vdiskPath

# Connect over the private interface IP (10.x) -- the target refuses 127.0.0.1.
$privIp = Get-NetIPAddress -AddressFamily IPv4 |
          Where-Object { $_.IPAddress -like '10.*' } |
          Select-Object -First 1 -ExpandProperty IPAddress
if (-not $privIp) { throw 'no private 10.x interface IP found to connect the iSCSI initiator over' }

New-IscsiTargetPortal -TargetPortalAddress $privIp -TargetPortalPortNumber 3260 | Out-Null
Update-IscsiTargetPortal -TargetPortalAddress $privIp -ErrorAction SilentlyContinue
$tgt = Get-IscsiTarget | Where-Object { $_.NodeAddress -like "*$TargetName*" } | Select-Object -First 1
if ($null -eq $tgt) { throw 'iSCSI target not discovered on the private-IP portal' }
Connect-IscsiTarget -NodeAddress $tgt.NodeAddress -IsPersistent $false | Out-Null
Start-Sleep -Seconds 3

# Bring the disk online + format NTFS (a standard volume with a GUID -- the property SqlCe requires).
$disk = Get-Disk | Where-Object { $_.BusType -eq 'iSCSI' } | Select-Object -First 1
if ($null -eq $disk) { throw 'no iSCSI disk surfaced after connect' }
if ($disk.IsOffline) { Set-Disk -Number $disk.Number -IsOffline $false }
Set-Disk        -Number $disk.Number -IsReadOnly $false -ErrorAction SilentlyContinue
Initialize-Disk -Number $disk.Number -PartitionStyle MBR
$part = New-Partition -DiskNumber $disk.Number -UseMaximumSize -AssignDriveLetter
Format-Volume -DriveLetter $part.DriveLetter -FileSystem NTFS -Confirm:$false -Force | Out-Null

Write-Host ("RAM disk mounted at {0}: (target '{1}', {2}MB)" -f $part.DriveLetter, $TargetName, [math]::Round($SizeBytes / 1MB))
$part.DriveLetter
