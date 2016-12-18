mv Tests/Linq/TravisDataPtoviders.txt Tests/Linq/UserDataProviders.txt 

 mysql -e 'CREATE DATABASE TestData;'

 psql -c 'create database TestData;' -U postgres
