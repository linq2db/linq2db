$wc = New-Object System.Net.WebClient
$exit = 0

$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_net452_results.xml"
nunit3-console Tests\Linq\bin\AppVeyor\net452\linq2db.Tests.dll --result=$logFileName --where "cat != Ignored"
if ($LastExitCode -ne 0) { $exit = $LastExitCode }
#$wc.UploadFile("https://ci.appveyor.com/api/testresults/nunit3/$env:APPVEYOR_JOB_ID", "$logFileName")


$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core2_results.xml"
dotnet test Tests\Linq\ -f netcoreapp2.0 --logger:"trx;LogFileName=$logFileName" --filter:"TestCategory!=Ignored" -c AppVeyor
if ($LastExitCode -ne 0) { $exit = $LastExitCode }
$wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$env:APPVEYOR_JOB_ID", "$logFileName")

$logFileName = "$env:APPVEYOR_BUILD_FOLDER\nunit_core1_results.xml"
dotnet test Tests\Linq\ -f netcoreapp1.0 --logger:"trx;LogFileName=$logFileName" --filter:"TestCategory=Create" -c AppVeyor
if ($LastExitCode -ne 0) { $exit = $LastExitCode }
$wc.UploadFile("https://ci.appveyor.com/api/testresults/mstest/$env:APPVEYOR_JOB_ID", "$logFileName")

$host.SetShouldExit($exit)
