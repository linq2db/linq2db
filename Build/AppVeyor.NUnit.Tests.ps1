$wc = New-Object System.Net.WebClient

$net46Tests = {
	param($dir, $logFileName)
	Set-Location $dir
	$output = nunit3-console Tests\Linq\bin\AppVeyor\net46\linq2db.Tests.dll --result=$logFileName --where "cat != Ignored & cat != SkipCI"
	$result = "" | Select-Object -Property output,status
	$result.output = $output
	$result.status = $LastExitCode
	return $result
}

$netcore2Tests = {
	param($dir, $logFileName)
	Set-Location $dir
	$output = dotnet test Tests\Linq\ -f netcoreapp2.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	$result = "" | Select-Object -Property output,status
	$result.output = $output
	$result.status = $LastExitCode
	return $result
}

$netcore1Tests = {
	param($dir, $logFileName)
	Set-Location $dir
	$output = dotnet test Tests\Linq\ -f netcoreapp1.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	$result = "" | Select-Object -Property output,status
	$result.output = $output
	$result.status = $LastExitCode
	return $result
}

$logFileNameNet45 = "$env:APPVEYOR_BUILD_FOLDER\nunit_net46_results.xml"
$logFileNameCore2 = "$env:APPVEYOR_BUILD_FOLDER\nunit_core2_results.xml"
$logFileNameCore1 = "$env:APPVEYOR_BUILD_FOLDER\nunit_core1_results.xml"

$dir = Get-Location
Start-Job -Name "netfx_tests" $net46Tests -ArgumentList $dir,$logFileNameNet45
Start-Job -Name "netcore_2_tests" $netcore2Tests -ArgumentList $dir,$logFileNameCore2
Start-Job -Name "netcore_1_tests" $netcore1Tests -ArgumentList $dir,$logFileNameCore1

While (Get-Job -State "Running")
{
  Start-Sleep 1
}

$results = Get-Job | Receive-Job

# actually takes some time, so disabling it also will speed-up testrun
Write-Host "Writing tests output"
$results | Foreach {$_.output} | Write-Host

Write-Host "Uploading test results"
$url = "https://ci.appveyor.com/api/testresults/nunit3/$env:APPVEYOR_JOB_ID"
#$wc.UploadFile($url, $logFileNameNet45)
$wc.UploadFile($url, $logFileNameCore2)
$wc.UploadFile($url, $logFileNameCore1)

$exit = ($results | Foreach {$_.status} | Measure-Object -Sum).Sum
Write-Host "Exit code (sum): $exit"
$host.SetShouldExit($exit)
