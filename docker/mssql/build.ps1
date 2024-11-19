function Build-MSSQL-Windows
{
    param
    (
        [string]$Dockefile,
        [string]$Version,
        [string]$Version2,
        [string]$Share,
        [string]$ShareUser,
        [string]$SharePass,
        [string]$GAC,
        [string]$From
    )

    docker build -f $Dockefile --memory 4g --tag linq2db/linq2db:win-mssql-$Version --build-arg VERSION="$Version" --build-arg VERSION2="$Version2" --build-arg SHARE_USER="$ShareUser" --build-arg SHARE_PASS="$SharePass" --build-arg SHARE_PATH="$Share" --build-arg GAC="$GAC" --build-arg FROM="$From" .\context
    }

$User = 'smb-user'
$Pass = '1qaz2ws!QAZ@WS'

Remove-LocalUser -Name "$User"
$verySecurePassword = ConvertTo-SecureString $Pass -AsPlainText -Force
$_ = New-LocalUser -Name $User -Password $verySecurePassword

Remove-SmbShare -Name Installs -Force

$_ = New-SmbShare -Name Installs -Description "SQL Server Installers" -Path c:\docker-images\Sources -FullAccess "Everyone"

$IP = (Get-NetIPAddress -PrefixOrigin Dhcp).IPAddress | Select-Object -First 1
$Share = "\\$IP\Installs"

Build-MSSQL-Windows 'win-mssql-2005' '2005' '90' $Share $User $Pass '' 'dotnet/framework/runtime:3.5-windowsservercore-ltsc2022'
Build-MSSQL-Windows 'win-mssql-2008' '2008' '100' $Share $User $Pass '' 'dotnet/framework/runtime:3.5-windowsservercore-ltsc2022'
Build-MSSQL-Windows 'win-mssql' '2012' '110' $Share $User $Pass 'C:\Windows\assembly\GAC_MSIL\' 'windows/servercore:ltsc2022-amd64'
Build-MSSQL-Windows 'win-mssql' '2014' '120' $Share $User $Pass 'C:\Windows\assembly\GAC_MSIL\' 'dotnet/framework/runtime:3.5-windowsservercore-ltsc2022'
Build-MSSQL-Windows 'win-mssql' '2016' '130' $Share $User $Pass 'C:\Windows\assembly\GAC_MSIL\' 'windows/servercore:ltsc2022-amd64'
Build-MSSQL-Windows 'win-mssql' '2017' '140' $Share $User $Pass 'C:\Windows\Microsoft.Net\assembly\GAC_MSIL\' 'windows/servercore:ltsc2022-amd64'
Build-MSSQL-Windows 'win-mssql' '2019' '150' $Share $User $Pass 'C:\Windows\Microsoft.Net\assembly\GAC_MSIL\' 'windows/servercore:ltsc2022-amd64'
Build-MSSQL-Windows 'win-mssql' '2022' '160' $Share $User $Pass 'C:\Windows\Microsoft.Net\assembly\GAC_MSIL\' 'windows/servercore:ltsc2022-amd64'

Remove-SmbShare -Name Installs -Force

Remove-LocalUser -Name $User
