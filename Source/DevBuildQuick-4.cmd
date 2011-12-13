set MSBuild="%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
set NoPause=true
%MSBuild% linq2db.nproj /p:Configuration=Debug /verbosity:n 
pause