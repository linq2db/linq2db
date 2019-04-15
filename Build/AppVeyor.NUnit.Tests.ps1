$net46Tests = {
	param($dir, $logFileName)
	Set-Location $dir
	$output = nunit3-console Tests\Linq\bin\AppVeyor\net46\linq2db.Tests.dll --result=$logFileName --where "cat != Ignored && cat != SkipCI"
	$result = "" | Select-Object -Property output,status,name
	$result.output = $output
	$result.status = $LastExitCode
	$result.name = "netfx"
	return $result
}

$netcore2Tests = {
	param($dir, $logFileName)
	Set-Location $dir
	$output = dotnet test Tests\Linq\ -f netcoreapp2.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	$result = "" | Select-Object -Property output,status,name
	$result.output = $output
	$result.status = $LastExitCode
	$result.name = "core2"
	return $result
}

$netcore1Tests = {
	param($dir, $logFileName)
	Set-Location $dir
	$output = dotnet test Tests\Linq\ -f netcoreapp1.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	$result = "" | Select-Object -Property output,status,name
	$result.output = $output
	$result.status = $LastExitCode
	$result.name = "core1"
	return $result
}

$logFileNameNet45 = "$env:APPVEYOR_BUILD_FOLDER\net46_test_results.xml"
$logFileNameCore2 = "$env:APPVEYOR_BUILD_FOLDER\core2_test_results.trx"
$logFileNameCore1 = "$env:APPVEYOR_BUILD_FOLDER\core1_test_results.trx"

$dir = Get-Location
Start-Job -Name "netfx_tests" $net46Tests -ArgumentList $dir,$logFileNameNet45
#Start-Job -Name "netcore_2_tests" $netcore2Tests -ArgumentList $dir,$logFileNameCore2
#Start-Job -Name "netcore_1_tests" $netcore1Tests -ArgumentList $dir,$logFileNameCore1

While (Get-Job -State "Running")
{
  Start-Sleep 1
}

$results = Get-Job | Receive-Job

#Write-Host "Uploading test results"
#$url = "https://ci.appveyor.com/api/testresults/nunit3/$env:APPVEYOR_JOB_ID"
#$wc = New-Object System.Net.WebClient
#$wc.UploadFile($url, $logFileNameNet45)
#$wc.UploadFile($url, $logFileNameCore2)
#$wc.UploadFile($url, $logFileNameCore1)

# push outputs to artifacts always, not only on success
Write-Host "Publish test outputs to artifacts..."
$results | %{ Out-File -FilePath "$env:APPVEYOR_BUILD_FOLDER\$($_.name)_test_outputs.log" -InputObject $_.output -Append; Push-AppveyorArtifact "$($_.name)_test_outputs.log" -FileName "$env:APPVEYOR_BUILD_FOLDER\$($_.name)_test_outputs.log" }
Write-Host "Done."

# set error status on any test runner failed
%{ $results | %{ if ($_.status -ne 0) { $host.SetShouldExit(-1); exit }} }
