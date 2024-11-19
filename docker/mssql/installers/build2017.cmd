rmdir ..\sources\mssql-2017 /q /s

sql2017\SQLServer2017-SSEI-Expr.exe /Quiet /Action=Download /MediaPath=%cd%\tmp /MediaType=Advanced

tmp\SQLEXPRADV_x64_ENU.exe /q /x:..\sources\mssql-2017
rmdir tmp /q /s

mkdir ..\sources\mssql-2017\cu
copy sql2017\SQLServer2017-KB5016884-x64.exe ..\sources\mssql-2017\cu\ /Y
copy sql2017\sqlserver2017-kb5029376-x64_377595bd4ba0de82256f259bc770df907d935cb8.exe ..\sources\mssql-2017\cu\ /Y

mkdir ..\sources\mssql-2017\gac