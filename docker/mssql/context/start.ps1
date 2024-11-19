param
(
    [Parameter(Mandatory=$false)] [string]$ACCEPT_EULA,
    [Parameter(Mandatory=$false)] [string]$MSSQL_SA_PASSWORD
)

if($ACCEPT_EULA -ne "Y" -And $ACCEPT_EULA -ne "y")
{
	Write-Verbose "ERROR: You must accept the End User License Agreement before this container can start."
	Write-Verbose "Set the environment variable ACCEPT_EULA to 'Y' if you accept the agreement."
    exit 1
}

Write-Verbose "Starting SQL Server"
Start-Service MSSQLSERVER
Start-Service MSSQLFDLauncher

if($MSSQL_SA_PASSWORD -ne "_")
{
    Write-Verbose "Changing SA login credentials"
    $sqlcmd = "ALTER LOGIN sa with password=" +"'" + $MSSQL_SA_PASSWORD + "'" + ";ALTER LOGIN sa ENABLE;"
    & sqlcmd -U "sa" -P "qGH6RFvq" -Q $sqlcmd
}

Write-Verbose "Started SQL Server."

$lastCheck = (Get-Date).AddSeconds(-2) 
while ($true) 
{ 
    Get-EventLog -LogName Application -Source "MSSQL*" -After $lastCheck | Select-Object TimeGenerated, EntryType, Message	 
    $lastCheck = Get-Date 
    Start-Sleep -Seconds 2 
}