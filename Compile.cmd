@echo off

cd /d "%~dp0"

FOR %%c IN (Release Debug) DO (
    dotnet clean Source\LinqToDB\LinqToDB.csproj -c %%c -v m
)
FOR %%c IN (Release Debug) DO (
    dotnet build Source\LinqToDB\LinqToDB.csproj -c %%c -v m
)

