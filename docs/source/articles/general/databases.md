---
uid: databases
---

# Supported Databases

[Class with name constants](xref:linq2db::LinqToDB.ProviderName).

One database may have several providers because of:

* using different ADO .Net implementations (as for SQLite)
* SQL compatibility level, that allows using new SQL features of the database engine (as for MS SQL Server)

| Database| Provider name |
|--|--|
|ClickHouse| ClickHouse<br/>ClickHouse.MySql<br/>ClickHouse.Client<br/>ClickHouse.Octonica |
|DB2 (LUW, z/OS)| DB2<br/>DB2.LUW<br/>DB2.z/OS |
|Firebird |Firebird |
|Informix |Informix<br/>Informix.DB2 |
|Microsoft Access |Access<br/>Access.ODBC |
|Microsoft Sql Azure | |
|Microsoft Sql Server |SqlServer - default compatibility level SQL Server 2008<br/>SqlServer.2000 (removed in v4)<br/>SqlServer.2005<br/>SqlServer.2008<br/>SqlServer.2012<br/>SqlServer.2014<br/>SqlServer.2016<br/>SqlServer.2017<br/>SqlServer.2019<br/>SqlServer.2022 |
|Microsoft SqlCe |SqlCe |
|MySql |MySql<br/>MySqlConnector<br/>MySql.Official |
|Oracle |Oracle<br/>Oracle.11.Managed</br>Oracle.11.Native<br/>Oracle.11.Devart<br/>Oracle.Managed</br>Oracle.Native<br/>Oracle.Devart |
|PostgreSQL |PostgreSQL<br/>PostgreSQL.9.2<br/>PostgreSQL.9.3<br/>PostgreSQL.9.5<br/>PostgreSQL.15 |
|SQLite |SQLite, SQLite.Classic - using System.Data.Sqlite<br>SQLite.MS - using Microsoft.Data.Sqlite |
|SAP HANA |SapHana<br/>SapHana.Native<br/>SapHana.Odbc |
|Sybase ASE |Sybase - using Native SAP/Sybase ASE provider<br/>Sybase.Managed - using Managed Sybase/SAP ASE provider from [DataAction](https://github.com/DataAction/AdoNetCore.AseClient) |
|[DB2 iSeries](https://github.com/LinqToDB4iSeries/Linq2DB4iSeries) |iSeriesProvider |
