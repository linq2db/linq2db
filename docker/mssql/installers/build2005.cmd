rmdir ..\sources\mssql-2005 /q /s
sql2005\sqlserver2005expressadvancedsp4-kb2463332-x86-enu_b8640fde879a23a2372b27f158d54abb5079033e.exe /x:tmp /q
tmp\hotfixexpressadv\files\sqlexpr_adv.exe /x:..\sources\mssql-2005 /q
rmdir tmp /q /s
