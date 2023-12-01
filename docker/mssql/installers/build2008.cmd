REM https://learn.microsoft.com/en-us/troubleshoot/sql/database-engine/install/windows/update-or-slipstream-installation#procedure-2-create-a-merged-drop

rmdir ..\sources\mssql-2008 /q /s

sql2008\SQLEXPRADV_x64_ENU.exe /q /x:..\sources\mssql-2008
sql2008\SQLServer2008SP4-KB2979596-x64-ENU.exe /q /x:..\sources\mssql-2008\pcu
sql2008\sqlserver2008-kb4057114-x64_9ce0b7c5909d8fcc5b9a12d17f29b7864a9df33a.exe /q /x:..\sources\mssql-2008\cu

robocopy ..\sources\mssql-2008\pcu\x64 ..\sources\mssql-2008\x64 /XF Microsoft.SQL.Chainer.PackageData.dll
robocopy ..\sources\mssql-2008\cu\x64 ..\sources\mssql-2008\x64 /XF Microsoft.SQL.Chainer.PackageData.dll

copy ..\sources\mssql-2008\cu\Setup.exe ..\sources\mssql-2008\ /Y
copy ..\sources\mssql-2008\cu\Setup.rll ..\sources\mssql-2008\ /Y
