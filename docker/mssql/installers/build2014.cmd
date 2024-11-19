rmdir ..\sources\mssql-2014 /q /s

sql2014\SQLEXPRADV_x64_ENU.exe /q /x:..\sources\mssql-2014

mkdir ..\sources\mssql-2014\cu
copy sql2014\sqlserver2014-kb5029185-x64_23ea4aee8aaac8b3683b21ab94ee0ebcf32cd760.exe ..\sources\mssql-2014\cu\ /Y

mkdir ..\sources\mssql-2014\gac\Microsoft.NetEnterpriseServers.ExceptionMessageBox\10.0.0.0__89845dcd8080cc91
mkdir ..\sources\mssql-2014\gac\Microsoft.SqlServer.CustomControls\10.0.0.0__89845dcd8080cc91
mkdir ..\sources\mssql-2014\gac\Microsoft.SqlServer.WizardFrameworkLite\10.0.0.0__89845dcd8080cc91
copy ..\sources\mssql-2014\1033_ENU_LP\x64\Setup\sql2008support\windows\gac\Q2BDSRKB.DLL ..\sources\mssql-2014\gac\Microsoft.NetEnterpriseServers.ExceptionMessageBox\10.0.0.0__89845dcd8080cc91\Microsoft.NetEnterpriseServers.ExceptionMessageBox.dll
copy ..\sources\mssql-2014\1033_ENU_LP\x64\Setup\sql2008support\windows\gac\-KKU9GG-.DLL ..\sources\mssql-2014\gac\Microsoft.SqlServer.CustomControls\10.0.0.0__89845dcd8080cc91\MICROSOFT.SQLSERVER.CUSTOMCONTROLS.DLL
copy ..\sources\mssql-2014\1033_ENU_LP\x64\Setup\sql2008support\windows\gac\YO4JF9KP.DLL ..\sources\mssql-2014\gac\Microsoft.SqlServer.WizardFrameworkLite\10.0.0.0__89845dcd8080cc91\Microsoft.SqlServer.WizardFrameworkLite.dll