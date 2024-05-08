#choco install msaccess2010-redist-x86
Invoke-WebRequest -Uri https://raw.githubusercontent.com/linq2db/linq2db.ci/access/providers/access/AccessDatabaseEngine.exe -OutFile AccessDatabaseEngine.exe
$process = Start-Process -FilePath AccessDatabaseEngine.exe -ArgumentList ('/Passive', '/Quiet', '/NoRestart', '/Log:$($env:temp)\MSAccess210-redist.log') -Wait -PassThru
$exitCode = $process.ExitCode
if ($exitCode -eq 0)
{
    Write-Host -Object 'Installation successful'
    return $exitCode
}
else
{
    Write-Host -Object "Non zero exit code returned by the installation process : $exitCode."
    Get-Content '$($env:temp)\MSAccess210-redist.log'
    exit $exitCode
}

