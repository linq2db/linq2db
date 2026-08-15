# LINQ to DB LINQPad Driver

This nuget package is a driver for [LINQPad](http://www.linqpad.net) 8 and newer, on Windows and macOS (LINQPad 9 recommended). LINQPad 5 is supported by the `.lpx` plugin published with each [release](https://github.com/linq2db/linq2db/releases). Support for older versions of LINQPad is available via older versions drivers.

Following databases supported:

* **ClickHouse**: using Binary, HTTP and MySQL interfaces
* **DB2** (LUW, z/OS, iSeries): x64-bit version of LINQPad only
* **DB2 iSeries**: check release notes to see which version supports this database
* **DuckDB**
* **Firebird**
* **Informix**: x64-bit version of LINQPad only
* **Microsoft Access**: both OLE DB and ODBC drivers *(Windows only)*
* **Microsoft SQL Server** 2005+ *(including **Microsoft SQL Azure**)*
* **Microsoft SQL Server Compact (SQL CE)** *(Windows only)*
* **MariaDB**
* **MySql**
* **Oracle**
* **PostgreSQL**
* **SAP HANA** *(client software must be installed, supports both Native and ODBC providers)*
* **SAP/Sybase ASE**
* **SQLite**
* **YDB**

## Installation

* Click "Add connection" in LINQPad.
* In the "Choose Data Context" dialog, press the "View more drivers..." button.
* In the "LINQPad NuGet Manager" dialog, find LINQ To DB driver in list of drivers and click the "Install" button.
* Close "LINQPad NuGet Manager" dialog
* In the "Choose Data Context" dialog, select the "LINQ to DB" driver and click the "Next" button.
* In the "LINQ to DB connection" dialog, supply your connection information.
* You're done.

## Database clients

The driver doesn't bundle database client libraries. LINQPad downloads the client of a database when a connection to it is first used, so using a database type for the first time needs an internet connection.

## macOS

Databases that need Windows-only components — Microsoft Access and SQL Server Compact — are not offered on macOS. For the rest:

* **DB2 / Informix**: the macOS build of the IBM client is used, but its native CLI driver is not part of the client package on any platform and has to be installed separately.
* **Microsoft SQL Server**: connections work, but `Microsoft.SqlServer.Types` values (`geometry`, `geography`, `hierarchyid`) need Windows-only native libraries and cannot be rendered.
* **SAP HANA**: needs the macOS HANA client installed, same as on Windows.

## Troubleshooting

Driver errors are written to `linq2db.LINQPad.log` in LINQPad's log folder (`%localappdata%\LINQPad\Logs.LINQPad<version>` on Windows). On macOS the connection dialog is rendered by LINQPad through Avalonia XPF; errors raised outside of it are written to the log only.
