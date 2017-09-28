%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /target:Clean LinqToDB.csproj /property:Configuration=Debug   /p:DefineConstants="NOASYNC"
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /target:Clean LinqToDB.csproj /property:Configuration=Release /p:DefineConstants="NOASYNC"
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe LinqToDB.csproj /property:Configuration=Debug   /property:OutputPath=bin\Debug.4.0\   /property:TargetFrameworkVersion=v4.0 /p:DefineConstants="NOASYNC"
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe LinqToDB.csproj /property:Configuration=Release /property:OutputPath=bin\Release.4.0\ /property:TargetFrameworkVersion=v4.0 /p:DefineConstants="NOASYNC"

%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /target:Clean LinqToDB.csproj /property:Configuration=Debug
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /target:Clean LinqToDB.csproj /property:Configuration=Release
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe LinqToDB.csproj /property:Configuration=Debug   
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe LinqToDB.csproj /property:Configuration=Release 

%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /target:Clean LinqToDB.WindowsStore.csproj /property:Configuration=Debug
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe /target:Clean LinqToDB.WindowsStore.csproj /property:Configuration=Release
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe LinqToDB.WindowsStore.csproj /property:Configuration=Debug
%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe LinqToDB.WindowsStore.csproj /property:Configuration=Release
