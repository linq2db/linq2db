Invoke-WebRequest -Uri https://download.microsoft.com/download/F/F/D/FFDF76E3-9E55-41DA-A750-1798B971936C/ENU/SSCERuntime_x64-ENU.exe -OutFile SSCERuntime_x64-ENU.exe
.\SSCERuntime_x64-ENU.exe \x:sqlce
$process = Start-Process -FilePath msiexec.exe -ArgumentList ('/i', '.\sqlce\SSCERuntime_x64-ENU.msi', '/qn', '/lxv', 'install.log') -Wait -PassThru
$exitCode = $process.ExitCode

Get-Content 'install.log'
        if ($exitCode -eq 0 -or $exitCode -eq 3010)
        {
            Write-Host -Object 'Installation successful'
            return $exitCode
        }
        else
        {
            Write-Host -Object "Non zero exit code returned by the installation process : $exitCode."
            exit $exitCode
        }
