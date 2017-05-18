param($installPath, $toolsPath, $package)

# get the active solution
$solution           = Get-Interface $dte.Solution ([EnvDTE80.Solution2])
$solutionPath       = [System.IO.Path]::GetDirectoryName($solution.FullName)
$linq2dbToolsPath   = [System.IO.Path]::Combine($solutionPath, ".tools", "linq2db.t4models")
$linq2dbContentPath = [System.IO.Path]::Combine($installPath, "content")

# tools copy
xcopy $("$toolsPath\*.*")            $("$linq2dbToolsPath\") /y /e

# content copy for .Net Core & similar projects
xcopy $("$linq2dbContentPath\*.*")   $("$linq2dbToolsPath\") /y /e

