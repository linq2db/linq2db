rmdir ..\sources\mssql-2019 /q /s

sql2019\SQL2019-SSEI-Expr.exe /Quiet /Action=Download /MediaPath=%cd%\tmp /MediaType=Advanced

tmp\SQLEXPRADV_x64_ENU.exe /q /x:..\sources\mssql-2019
rmdir tmp /q /s

mkdir ..\sources\mssql-2019\cu
copy sql2019\SQLServer2019-KB5030333-x64.exe ..\sources\mssql-2019\cu\ /Y

mkdir ..\sources\mssql-2019\gac