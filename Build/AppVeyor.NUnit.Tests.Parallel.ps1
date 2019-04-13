$net46Tests = {
	param($url)
	#$wc = New-Object System.Net.WebClient
	$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_net46_results.xml"
	$null = nunit3-console Tests\Linq\bin\AppVeyor\net46\linq2db.Tests.dll --result=$logFileName --where "cat != Ignored & cat != SkipCI"
	if ($LastExitCode -ne 0) { $exit = $LastExitCode }
	$null = #$wc.UploadFile($url, "$logFileName")
	return $exit
}

$netcore2Tests = {
	param($url)
	$wc = New-Object System.Net.WebClient
	$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core2_results.xml"
	$null = dotnet test Tests\Linq\ -f netcoreapp2.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	if ($LastExitCode -ne 0) { $exit = $LastExitCode }
	$null = $wc.UploadFile($url, "$logFileName")
	return $exit
}

$netcore1Tests = {
	param($url)
	$wc = New-Object System.Net.WebClient
	$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core1_results.xml"
	$null = dotnet test Tests\Linq\ -f netcoreapp1.0 --logger "trx;LogFileName=$logFileName" --filter "TestCategory != Ignored & TestCategory != ActiveIssue & TestCategory != SkipCI" -c AppVeyor
	if ($LastExitCode -ne 0) { $exit = $LastExitCode }
	$null = $wc.UploadFile($url, "$logFileName")
	return $exit
}

$url = "https://ci.appveyor.com/api/testresults/nunit3/$env:APPVEYOR_JOB_ID"
Start-Job $net46Tests -ArgumentList $url
Start-Job $netcore2Tests -ArgumentList $url
Start-Job $netcore1Tests -ArgumentList $url

While (Get-Job -State "Running")
{
  Start-Sleep 1
}

$exit = (Get-Job | Receive-Job | Measure-Object -Sum).Sum

$host.SetShouldExit($exit)
