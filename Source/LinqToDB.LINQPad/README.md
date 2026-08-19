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

This nuget package doesn't bundle database client libraries. LINQPad downloads the client of a database when a connection to it is first used, so using a database type for the first time needs an internet connection. A static data context selects its provider itself, so the driver cannot detect which client it needs: such a connection downloads all of them unless you pick its database in the "Database" field of the connection dialog. Note that a context assembly which references a client library must be able to load it from its own folder — a build output folder has it, a hand-assembled one may not — because the connection dialog inspects that assembly before any client has been downloaded. The "Context" field is editable if the list cannot be built.

The LINQPad 5 `.lpx` plugin is unaffected by any of this — it ships every client in the bundle.

## macOS

Databases that need Windows-only components — Microsoft Access and SQL Server Compact — are not offered on macOS. Everything else uses the macOS build of its client, including IBM DB2 and Informix. SQL Server spatial values (`geometry`, `geography`, `hierarchyid`) are handled by the managed [dotMorten.Microsoft.SqlServer.Types](https://www.nuget.org/packages/dotMorten.Microsoft.SqlServer.Types) implementation, as Microsoft's package ships its native spatial library for Windows only.

## Troubleshooting

Driver errors are written to `linq2db.LINQPad.log`, in the `Logs.LINQPad<version>` folder of LINQPad's application data directory:

* Windows: `%localappdata%\LINQPad`
* macOS: `~/Library/Application Support/LINQPad`

On macOS, errors raised outside of the connection dialog go to that log only, without a message box: LINQPad renders a driver's dialogs through Avalonia XPF there, and XPF is available to a driver just while its own dialog is open, so anything reported from elsewhere has nowhere to draw. On Windows they are shown in a message box as well.

Failures the driver recovers from go to the log on either system, without a message box. Reading the age of a connection's schema is one: LINQPad asks for it before it has downloaded that connection's database client, so it cannot be answered yet, and the schema is built and queried normally straight afterwards.
