@echo off

cd /d "%~dp0"

dotnet clean linq2db.sln -c Release -v m
dotnet clean linq2db.sln -c Debug -v m
dotnet clean linq2db.sln -c Azure -v m
