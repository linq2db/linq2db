Invoke-WebRequest -Uri https://download.microsoft.com/download/F/F/D/FFDF76E3-9E55-41DA-A750-1798B971936C/ENU/SSCERuntime_x64-ENU.exe -OutFile SSCERuntime_x64-ENU.exe
.\SSCERuntime_x64-ENU.exe /quiet /qn
Copy-Item "c:\Program Files (x86)\Microsoft SQL Server Compact Edition\v4.0\Private\*" .\ -Recurse -Include *
