rmdir ..\sources\mssql-2022 /q /s

sql2022\SQL2022-SSEI-Expr.exe /Quiet /Action=Download /MediaPath=%cd%\tmp /MediaType=Advanced

tmp\SQLEXPRADV_x64_ENU.exe /q /x:..\sources\mssql-2022
rmdir tmp /q /s

mkdir ..\sources\mssql-2022\cu
copy sql2022\SQLServer2022-KB5031778-x64.exe ..\sources\mssql-2022\cu\ /Y

mkdir ..\sources\mssql-2022\gac
xcopy sql2022-gac\ ..\sources\mssql-2022\gac\ /s /e /h
