param($installPath, $toolsPath, $package, $project)

$copyFrom = $("$installPath\content\*.*")
$copyTo = [System.IO.Path]::GetDirectoryName($project.FullName) + "\"

Write-Host $copyFrom 
Write-Host $copyTo

xcopy $copyFrom $copyTo  /y /E

