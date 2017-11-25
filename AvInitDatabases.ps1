param ([string]$conf="")

if ($conf -eq "mono")
{
	Rename-Item Tests\Linq\MonoAppveyorDataProviders.txt UserDataProviders.txt
	Rename-Item Tests\Linq\AppveyorDataProviders.Core.txt UserDataProviders.Core.txt
}
else
{
	Rename-Item Tests\Linq\AppveyorDataProviders.txt UserDataProviders.txt
	Rename-Item Tests\Linq\AppveyorDataProviders.Core.txt UserDataProviders.Core.txt
}

$startPath = "$($env:appveyor_build_folder)\Data"
$sqlInstance = "(local)\SQL2012SP1"
$dbName = "TestData"

# create database
sqlcmd -S "$sqlInstance" -Q "Use [master]; CREATE DATABASE [$dbName]"

#$mdfFile = join-path $startPath "northwnd.mdf"
#$ldfFile = join-path $startPath "northwnd.ldf"
#Write-Host "mdfFile : $mdfFile "
#Write-Host "ldfFile : $ldfFile "
#sqlcmd -S "$sqlInstance" -Q "Use [master]; CREATE DATABASE [Northwind] ON (FILENAME = '$mdfFile'),(FILENAME = '$ldfFile') for ATTACH"

# MySql 
$env:MYSQL_PWD="Password12!"
$cmd = '"C:\Program Files\MySql\MySQL Server 5.7\bin\mysql" -e "create database $dbName;" --user=root'
iex "& $cmd"

# PgSql
$env:PGUSER="postgres"
$env:PGPASSWORD="Password12!"
$cmd = '"C:\Program Files\PostgreSQL\9.3\bin\createdb" $dbName'
iex "& $cmd"



