@echo off

cd /d "%~dp0"

dotnet build linq2db.sln -c Release --no-incremental -v m
dotnet build linq2db.sln -c Debug --no-incremental -v m
dotnet build linq2db.sln -c Azure --no-incremental -v m
