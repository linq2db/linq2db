rmdir ..\sources\mssql-2012 /q /s

sql2012\SQLEXPRADV_x64_ENU.exe /q /x:..\sources\mssql-2012

mkdir ..\sources\mssql-2012\cu
copy sql2012\sqlserver2012-kb4583465-x64_c6e5ea14425fed26b885ab6b70aba8622817fd8c.exe ..\sources\mssql-2012\cu\ /Y

mkdir ..\sources\mssql-2012\gac\Microsoft.NetEnterpriseServers.ExceptionMessageBox\10.0.0.0__89845dcd8080cc91
mkdir ..\sources\mssql-2012\gac\Microsoft.SqlServer.CustomControls\10.0.0.0__89845dcd8080cc91
mkdir ..\sources\mssql-2012\gac\Microsoft.SqlServer.WizardFrameworkLite\10.0.0.0__89845dcd8080cc91
copy ..\sources\mssql-2012\1033_ENU_LP\x64\Setup\sql2008support\windows\gac\Q2BDSRKB.DLL ..\sources\mssql-2012\gac\Microsoft.NetEnterpriseServers.ExceptionMessageBox\10.0.0.0__89845dcd8080cc91\Microsoft.NetEnterpriseServers.ExceptionMessageBox.dll
copy ..\sources\mssql-2012\1033_ENU_LP\x64\Setup\sql2008support\windows\gac\-KKU9GG-.DLL ..\sources\mssql-2012\gac\Microsoft.SqlServer.CustomControls\10.0.0.0__89845dcd8080cc91\MICROSOFT.SQLSERVER.CUSTOMCONTROLS.DLL
copy ..\sources\mssql-2012\1033_ENU_LP\x64\Setup\sql2008support\windows\gac\YO4JF9KP.DLL ..\sources\mssql-2012\gac\Microsoft.SqlServer.WizardFrameworkLite\10.0.0.0__89845dcd8080cc91\Microsoft.SqlServer.WizardFrameworkLite.dll