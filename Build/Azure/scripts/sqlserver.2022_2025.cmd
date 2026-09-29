rem SQL Server 2022 (host port 1422) and 2025 (host port 1425) run as concurrent lanes in one job.
rem Each SQL Server listens on 1433 inside its container; the host port differentiates them.
rem The 2025 image ships mssql-tools18 sqlcmd, which defaults to encrypted connections and rejects
rem the self-signed cert without -C (trust server certificate) - hence the trailing spec field.
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql2022|1422|linq2db/linq2db:win-mssql-2022,mssql2025|1425|linq2db/linq2db:win-mssql-2025|-C"
if %errorlevel% NEQ 0 exit /b 1

docker exec mssql2022 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2022 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
docker exec mssql2025 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;" -C
docker exec mssql2025 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;" -C
REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql2022 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2022 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2025 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;" -C
docker exec mssql2025 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;" -C
