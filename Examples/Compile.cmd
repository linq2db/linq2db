@echo off

cd /d "%~dp0"

dotnet build Examples.sln -c Release --no-incremental -v m
dotnet build Examples.sln -c Debug --no-incremental -v m
