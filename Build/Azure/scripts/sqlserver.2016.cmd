rem SQL Server 2016 (host port 1416).
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql2016|1416|linq2db/linq2db:win-mssql-2016"
if %errorlevel% NEQ 0 exit /b 1

docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2016 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;"
