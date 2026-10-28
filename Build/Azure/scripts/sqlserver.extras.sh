#!/bin/bash

. "$(dirname "$0")/mssql-wait.sh"

docker run -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1433:1433 -h mssql --name=mssql -d linq2db/linq2db:mssql-2025
docker ps -a

wait_or_recreate mssql 1433 linq2db/linq2db:mssql-2025 || exit 1

docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'SELECT @@Version'

# SqlServer.CS: case-sensitive data and catalog (no CATALOG_COLLATION - catalog follows database collation)
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataCS COLLATE Latin1_General_CS_AS;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSCS COLLATE Latin1_General_CS_AS;'

# SqlServer.Contained: case-sensitive data, contained database catalog is always case-insensitive
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'sp_configure '"'"'contained database authentication'"'"', 1;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'RECONFIGURE;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestDataMSContained CONTAINMENT = PARTIAL COLLATE Latin1_General_CS_AS;'

# test-DB perf: SIMPLE recovery + delayed durability cut transaction-log-flush cost on the write-heavy suite
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataCS SET RECOVERY SIMPLE; ALTER DATABASE TestDataCS SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMSCS SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSCS SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataContained SET DELAYED_DURABILITY = FORCED;'
docker exec mssql /opt/mssql-tools18/bin/sqlcmd -No -S localhost -U sa -P Password12! -Q 'ALTER DATABASE TestDataMSContained SET RECOVERY SIMPLE; ALTER DATABASE TestDataMSContained SET DELAYED_DURABILITY = FORCED;'
