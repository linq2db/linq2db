pwsh -NoProfile -ExecutionPolicy Bypass -File "%~dp0mssql-container.ps1" -Container "mssql|1433|linq2db/linq2db:win-mssql-2019"
if %errorlevel% NEQ 0 exit /b 1

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT @@Version"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataSA;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSSA;"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "sp_configure 'contained database authentication', 1;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "RECONFIGURE;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL;"

REM test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite (SQL 2019 image)
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataSA SET RECOVERY SIMPLE; ALTER DATABASE TestDataSA SET DELAYED_DURABILITY = FORCED;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMSSA SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSSA SET DELAYED_DURABILITY = FORCED;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataContained SET DELAYED_DURABILITY = FORCED;"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "ALTER DATABASE TestDataMSContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSContained SET DELAYED_DURABILITY = FORCED;"
