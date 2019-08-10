docker pull microsoft/mssql-server-windows-express:2017-latest
docker run -d -e 'ACCEPT_EULA=Y' -e 'SA_PASSWORD=Password12!' -p 1433:1433 -h mssql --name=mssql microsoft/mssql-server-windows-express:2017-latest
docker ps -a
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData;'
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q 'CREATE DATABASE TestData2017;'
docker cp scripts/sql/northwind.sql mssql:/northwind.sql
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -i /northwind.sql
