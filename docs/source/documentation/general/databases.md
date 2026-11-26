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
|ClickHouse| ClickHouse<br/>ClickHouse.Driver - using ClickHouse.Driver<br/>ClickHouse.Octonica - using Octonica.ClickHouseClient<br/>ClickHouse.MySql - using MySqlConnector |
|DB2 (LUW, z/OS)| DB2<br/>DB2.LUW<br/>DB2.z/OS |
|DuckDB |DuckDB |
|Firebird |Firebird<br/>Firebird.2.5<br/>Firebird.3<br/>Firebird.4<br/>Firebird.5 |
|Informix |Informix<br/>Informix.DB2 |
|MariaDB |MariaDB.10<br/>MariaDB.10.MySql.Data<br/>MariaDB.10.MySqlConnector |
|Microsoft Access |Access - OleDb, JET or ACE detected<br/>Access.Odbc - ODBC, JET or ACE detected<br/>Access.Jet.OleDb<br/>Access.Jet.Odbc<br/>Access.Ace.OleDb<br/>Access.Ace.Odbc<br/>Access.LibRed - managed provider, reads and writes MDB and ACCDB files |
|Microsoft Sql Azure | |
|Microsoft Sql Server |SqlServer - default compatibility level SQL Server 2008<br/>SqlServer.2005<br/>SqlServer.2008<br/>SqlServer.2012<br/>SqlServer.2014<br/>SqlServer.2016<br/>SqlServer.2017<br/>SqlServer.2019<br/>SqlServer.2022<br/>SqlServer.2025 |
|Microsoft SqlCe |SqlCe |
|MySql |MySql<br/>MySql.5.7<br/>MySql.8.0<br/>MySql.5.7.MySql.Data<br/>MySql.5.7.MySqlConnector<br/>MySql.8.0.MySql.Data<br/>MySql.8.0.MySqlConnector |
|Oracle |Oracle<br/>Oracle.11.Managed<br/>Oracle.11.Native<br/>Oracle.11.Devart<br/>Oracle.Managed<br/>Oracle.Native<br/>Oracle.Devart |
|PostgreSQL |PostgreSQL<br/>PostgreSQL.9.2<br/>PostgreSQL.9.3<br/>PostgreSQL.9.5<br/>PostgreSQL.11<br/>PostgreSQL.12<br/>PostgreSQL.13<br/>PostgreSQL.15<br/>PostgreSQL.18<br/>PostgreSQL.19 |
|SQLite |SQLite, SQLite.Classic - using System.Data.Sqlite<br>SQLite.MS - using Microsoft.Data.Sqlite |
|SAP HANA |SapHana<br/>SapHana.Native<br/>SapHana.Odbc |
|Sybase ASE |Sybase - using Native SAP/Sybase ASE provider<br/>Sybase.Managed - using Managed Sybase/SAP ASE provider from [DataAction](https://github.com/DataAction/AdoNetCore.AseClient) |
|YDB |YDB |
|[DB2 iSeries](https://github.com/LinqToDB4iSeries/Linq2DB4iSeries) |iSeriesProvider |
