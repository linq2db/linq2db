Move-Item -Path "win_netfx_job2.json" -Destination "DataProviders.json" -Force


$sqlInstance = "(local)\SQL2014"

# create databases
$dbName = "TestDatanet45"
sqlcmd -S "$sqlInstance" -Q "Use [master]; CREATE DATABASE [$dbName]"
sqlcmd -U sa -P Password12! -S "$sqlInstance" -i ".\Data\Create Scripts\Northwind.sql" > nul
