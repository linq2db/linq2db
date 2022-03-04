rem docker pull microsoft/mssql-server-windows-developer:2017-latest
docker run -d -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Password12!" -p 1433:1433 -h mssql --name=mssql cagrin/mssql-server-ltsc2022:2017-latest
docker ps -a

echo "Waiting for SQL Server to accept connections"
set max=100
:repeat
set /a max=max-1
if %max% EQU 0 goto fail
echo pinging sql server
sleep 1
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT 1"
if %errorlevel% NEQ 0 goto repeat
echo "SQL Server is operational"

docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "SELECT @@Version"
echo "create TestData"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData;"
echo "create TestData2017"
docker exec mssql sqlcmd -S localhost -U sa -P Password12! -Q "CREATE DATABASE TestData2017;"
goto:eof

:fail
echo "Fail"
docker logs mssql
