param($installPath, $toolsPath, $package, $project)

# get the active solution
$solution           = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
$solutionPath       = [System.IO.Path]::GetDirectoryName($solution.FullName)
$linq2dbToolsPath   = [System.IO.Path]::Combine($solutionPath, ".tools", "linq2db.t4models")

# tools copy
xcopy $("$toolsPath\*.*") $("$linq2dbToolsPath\") /y /e
