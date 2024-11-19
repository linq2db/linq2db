@echo off

cd /d "%~dp0"

FOR %%c IN (Release Debug Azure) DO (
    dotnet clean linq2db.sln -c %%c -v m
)
