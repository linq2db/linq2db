@echo off

cd /d "%~dp0"

FOR %%c IN (Release Debug Azure) DO (
    dotnet build linq2db.slnx -c %%c --no-incremental -v m
)
