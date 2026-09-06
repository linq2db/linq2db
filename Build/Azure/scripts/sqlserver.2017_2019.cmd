rem SQL Server 2017 (host port 1417) and 2019 (host port 1419) run as concurrent lanes in one job.
rem Each SQL Server listens on 1433 inside its container; the host port differentiates them.
pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql2017|1417|linq2db/linq2db:win-mssql-2017,mssql2019|1419|linq2db/linq2db:win-mssql-2019"
if %errorlevel% NEQ 0 exit /b 1

docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS;"
REM The 2019 databases are deliberately case-sensitive - IsCaseSensitiveComparison in TestBase.Utils
REM expects it for AllSqlServerCS, and on Windows both database and catalog are case-sensitive.
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;"
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMS COLLATE Latin1_General_CS_AS WITH CATALOG_COLLATION = SQL_Latin1_General_CP1_CI_AS;"
REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2017 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestData SET RECOVERY SIMPLE; ALTER DATABASE TestData SET DELAYED_DURABILITY = FORCED;"
docker exec mssql2019 sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMS SET DELAYED_DURABILITY = FORCED;"
