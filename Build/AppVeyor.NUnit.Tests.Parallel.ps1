$wc = New-Object System.Net.WebClient

$net46Tests = {
	param($url)
	$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_net46_results.xml"
	$null = nunit3-console Tests\Linq\bin\AppVeyor\net46\linq2db.Tests.dll --result=$logFileName --where "cat != Ignored & cat != SkipCI"
	return $LastExitCode
}

$netcore2Tests = {
	param($url)
	$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core2_results.xml"
	$null = dotnet test Tests\Linq\ -f netcoreapp2.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	return $LastExitCode
}

$netcore1Tests = {
	param($url)
	$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core1_results.xml"
	$null = dotnet test Tests\Linq\ -f netcoreapp1.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	return $LastExitCode
}

$url = "https://ci.appveyor.com/api/testresults/nunit3/$env:APPVEYOR_JOB_ID"
Start-Job $net46Tests -ArgumentList $url
Start-Job $netcore2Tests -ArgumentList $url
Start-Job $netcore1Tests -ArgumentList $url

While (Get-Job -State "Running")
{
  Start-Sleep 1
}

$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_net46_results.xml"
#$wc.UploadFile("$url", "$logFileName")
$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core2_results.xml"
$wc.UploadFile("$url", "$logFileName")
$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core1_results.xml"
$wc.UploadFile("$url", "$logFileName")

$exit = (Get-Job | Receive-Job | Measure-Object -Sum).Sum

$host.SetShouldExit($exit)
