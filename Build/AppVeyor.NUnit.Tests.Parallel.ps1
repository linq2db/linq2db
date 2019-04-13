$wc = New-Object System.Net.WebClient

$net46Tests = {
	param($logFileName)
	$null = nunit3-console Tests\Linq\bin\AppVeyor\net46\linq2db.Tests.dll --result=$logFileName --where "cat != Ignored & cat != SkipCI"
	return $LastExitCode
}

$netcore2Tests = {
	param($logFileName)
	$null = dotnet test Tests\Linq\ -f netcoreapp2.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	return $LastExitCode
}

$netcore1Tests = {
	param($logFileName)
	$null = dotnet test Tests\Linq\ -f netcoreapp1.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	return $LastExitCode
}

$logFileNameNet45 = "$env:APPVEYOR_BUILD_FOLDER\nunit_net46_results.xml"
$logFileNameCore2 = "$env:APPVEYOR_BUILD_FOLDER\nunit_core2_results.xml"
$logFileNameCore1 = "$env:APPVEYOR_BUILD_FOLDER\nunit_core1_results.xml"

Start-Job $net46Tests  -ArgumentList $logFileNameNet45
Start-Job $netcore2Tests  -ArgumentList $logFileNameCore2
Start-Job $netcore1Tests  -ArgumentList $logFileNameCore1

While (Get-Job -State "Running")
{
  Start-Sleep 1
}

$url = "https://ci.appveyor.com/api/testresults/nunit3/$env:APPVEYOR_JOB_ID"
#$wc.UploadFile($url, $logFileNameNet45)
$wc.UploadFile($url, $logFileNameCore2)
$wc.UploadFile($url, $logFileNameCore1)

$exit = (Get-Job | Receive-Job | Measure-Object -Sum).Sum

$host.SetShouldExit($exit)
