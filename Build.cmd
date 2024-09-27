@echo off

cd /d "%~dp0"

FOR %%c IN (Release Debug Azure) DO (
    dotnet build linq2db.sln -c %%c --no-incremental -v m
)
