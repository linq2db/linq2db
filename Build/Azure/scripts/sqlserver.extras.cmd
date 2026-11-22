pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql|1433|linq2db/linq2db:win-mssql-2025|-C"
if %errorlevel% NEQ 0 exit /b 1

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT @@Version" -C

REM SqlServer.CS: case-sensitive data and catalog (no CATALOG_COLLATION - catalog follows database collation)
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataCS COLLATE Latin1_General_CS_AS;" -C
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSCS COLLATE Latin1_General_CS_AS;" -C

REM SqlServer.Contained: case-sensitive data, contained database catalog is always case-insensitive
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "sp_configure 'contained database authentication', 1;" -C
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "RECONFIGURE;" -C
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;" -C
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;" -C

REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataCS SET RECOVERY SIMPLE; ALTER DATABASE TestDataCS SET DELAYED_DURABILITY = FORCED;" -C
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMSCS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSCS SET DELAYED_DURABILITY = FORCED;" -C
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataContained SET DELAYED_DURABILITY = FORCED;" -C
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMSContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSContained SET DELAYED_DURABILITY = FORCED;" -C
