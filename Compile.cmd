cd /d "%~dp0"
"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" /v:m /target:Clean Source\LinqToDB\LinqToDB.csproj /property:Configuration=Debug
"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" /v:m /target:Clean Source\LinqToDB\LinqToDB.csproj /property:Configuration=Release
"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" /v:m Source\LinqToDB\LinqToDB.csproj /property:Configuration=Debug
"%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe" /v:m Source\LinqToDB\LinqToDB.csproj /property:Configuration=Release
