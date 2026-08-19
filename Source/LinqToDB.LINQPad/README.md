# LINQ to DB LINQPad Driver

This nuget package is a driver for [LINQPad](http://www.linqpad.net) 8 and newer, on Windows and macOS (LINQPad 9 recommended). LINQPad 5 is supported by the `.lpx` plugin published with each [release](https://github.com/linq2db/linq2db/releases). Support for older LINQPad versions is available in older driver versions.

Following databases supported:

* **ClickHouse**: using Binary, HTTP and MySQL interfaces
* **DB2** (LUW, z/OS, iSeries): 64-bit LINQPad only
* **DB2 iSeries**: check release notes to see which version supports this database
* **DuckDB**
* **Firebird**
* **Informix**: 64-bit LINQPad only
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

This nuget package doesn't bundle database client libraries. LINQPad downloads the client of a database when a connection to it is first used, so using a database type for the first time needs an internet connection. The LINQPad 5 `.lpx` plugin is unaffected — it ships every client in the bundle.

## macOS

Databases that need Windows-only components — Microsoft Access and SQL Server Compact — are not offered on macOS. Everything else uses the macOS build of its client, including IBM DB2 and Informix. SQL Server spatial values (`geometry`, `geography`, `hierarchyid`) are handled by the managed [dotMorten.Microsoft.SqlServer.Types](https://www.nuget.org/packages/dotMorten.Microsoft.SqlServer.Types) implementation, as Microsoft's package ships its native spatial library for Windows only.

## Troubleshooting

Driver errors are written to `linq2db.LINQPad.log`, in the `Logs.LINQPad<version>` folder of LINQPad's application data directory:

* Windows: `%localappdata%\LINQPad`
* macOS: `~/Library/Application Support/LINQPad`

Errors raised outside of the connection dialog are reported in that log only, without a message box. LINQPad renders a driver's dialogs through Avalonia XPF on macOS, and that is available to a driver just while the dialog is open, so anything reported from elsewhere has nowhere to draw.
