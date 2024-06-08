#choco install msaccess2010-redist-x64 --allow-empty-checksums
Invoke-WebRequest -Uri https://raw.githubusercontent.com/linq2db/linq2db.ci/access/providers/access/AccessDatabaseEngine_X64.exe -OutFile AccessDatabaseEngine_X64.exe
$process = Start-Process -FilePath AccessDatabaseEngine_X64.exe -ArgumentList ('/Passive', '/Quiet', '/NoRestart', '/Log:$($env:temp)\MSAccess210-redist.log') -Wait -PassThru
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


