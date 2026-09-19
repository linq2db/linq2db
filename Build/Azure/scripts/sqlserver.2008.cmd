rem SQL Server 2008 (host port 1408).
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql2008|1408|linq2db/linq2db:win-mssql-2008"
if %errorlevel% NEQ 0 exit /b 1

docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM test-DB perf: SIMPLE recovery cuts transaction-log overhead (DELAYED_DURABILITY needs SQL 2014+, N/A here)
docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE;"
docker exec mssql2008 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE;"
