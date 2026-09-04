pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql|1433|linq2db/linq2db:win-mssql-2005"
if %errorlevel% NEQ 0 exit /b 1

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT @@Version"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM test-DB perf: SIMPLE recovery cuts transaction-log overhead (DELAYED_DURABILITY needs SQL 2014+, N/A here)
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE;"
