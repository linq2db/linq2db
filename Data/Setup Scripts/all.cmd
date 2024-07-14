echo off

echo ========== ClickHouse ==========

call clickhouse.cmd

echo ========== DB2 ==========

call db2.cmd

echo ========== Firebird 2.5 ==========

call firebird25.cmd

echo ========== Firebird 3.x ==========

call firebird30.cmd

echo ========== Firebird 4.x ==========

call firebird40.cmd

echo ========== Firebird 5.x ==========

call firebird50.cmd

echo ========== Informix ==========

call informix.cmd

echo ========== MariaDB ==========

call mariadb.cmd

echo ========== MySql 5.7 ==========

call mysql57.cmd

echo ========== MySql ==========

call mysql.cmd

echo ========== Oracle 11 ==========

call oracle11.cmd

echo ========== Oracle 12 ==========

rem ERROR: DATABASE SETUP WAS NOT SUCCESSFUL!
rem call oracle12.cmd

echo ========== Oracle 19 ==========

call oracle19.cmd

echo ========== PostgreSQL 9.2 ==========

call pgsql92.cmd

echo ========== PostgreSQL 9.3 ==========

call pgsql93.cmd

echo ========== PostgreSQL 9.5 ==========

call pgsql95.cmd

echo ========== PostgreSQL 15 ==========

call pgsql15.cmd

echo ========== PostgreSQL 16 ==========

call pgsql16.cmd

echo ========== SAP Hana ==========

call saphana2.cmd

echo ========== SQL Server 2017 ==========

call sqlserver2017.cmd

echo ========== SQL Server 2019 ==========

call sqlserver2019.cmd

echo ========== SQL Server 2022 ==========

call sqlserver2022.cmd

echo ========== Sybase ==========

call sybase-ase.cmd


echo ========== Container list ==========

docker update --restart unless-stopped clickhouse
docker update --restart unless-stopped db2
docker update --restart unless-stopped firebird25
docker update --restart unless-stopped firebird30
docker update --restart unless-stopped firebird40
docker update --restart unless-stopped informix
docker update --restart unless-stopped mariadb
docker update --restart unless-stopped mysql57
docker update --restart unless-stopped mysql
docker update --restart unless-stopped oracle11
docker update --restart unless-stopped oracle19
docker update --restart unless-stopped pgsql92
docker update --restart unless-stopped pgsql93
docker update --restart unless-stopped pgsql95
docker update --restart unless-stopped pgsql15
docker update --restart unless-stopped pgsql16
docker update --restart unless-stopped hana2
docker update --restart unless-stopped sql2017
docker update --restart unless-stopped sql2019
docker update --restart unless-stopped sql2022
docker update --restart unless-stopped sybase

docker ps -a
