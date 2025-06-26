@echo off

cd /d "%~dp0"

dotnet build Examples.slnx -c Release --no-incremental -v m
dotnet build Examples.slnx -c Debug --no-incremental -v m
