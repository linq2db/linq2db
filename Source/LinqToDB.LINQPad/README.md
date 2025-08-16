# LINQ to DB LINQPad Driver

linq2db.LINQPad is a driver for [LINQPad 5 (.NET Framework 4.8)](http://www.linqpad.net) and [LINQPad 8 (.NET 8+)](http://www.linqpad.net).

Following databases supported (by all LINQPad versions if other not specified):

- **ClickHouse**: using Binary (LINQPad 8), HTTP and MySQL interfaces
- **DB2** (LUW, z/OS): LINQPad 8 x64 and LINQPad 5)
- **DB2 iSeries**: LINQPad 8 and LINQPad 5 (check release notes to see which version supports this database)
- **Firebird**
- **Informix**: LINQPad 8 x64 and LINQPad 5
- **Microsoft Access**: both OLE DB and ODBC drivers
- **Microsoft SQL Server** 2005+ *(including **Microsoft SQL Azure**)*
- **Microsoft SQL Server Compact (SQL CE)**
- **MariaDB**
- **MySql**
- **Oracle**
- **PostgreSQL**
- **SAP HANA** *(client software must be installed, supports both Native and ODBC providers)*
- **SAP/Sybase ASE**
- **SQLite**

## Download

Releases are hosted on [Github](https://github.com/linq2db/linq2db/releases) and on [nuget.org](https://www.nuget.org/packages/linq2db.LINQPad) for LINQPad 8 driver.

Latest build is hosted on [Azure Artifacts](https://dev.azure.com/linq2db/linq2db/_packaging?_a=package&feed=linq2db%40Local&package=linq2db.LINQPad&protocolType=NuGet). Feed [URL](https://pkgs.dev.azure.com/linq2db/linq2db/_packaging/linq2db/nuget/v3/index.json) ([how to use](https://docs.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio#package-sources)).

## Installation

### LINQPad 8 (NuGet)

- Click "Add connection" in LINQPad.
- In the "Choose Data Context" dialog, press the "View more drivers..." button.
- In the "LINQPad NuGet Manager" dialog, find LINQ To DB driver in list of drivers and click the "Install" button.
- Close "LINQPad NuGet Manager" dialog
- In the "Choose Data Context" dialog, select the "LINQ to DB" driver and click the "Next" button.
- In the "LINQ to DB connection" dialog, supply your connection information.
- You're done.

### LINQPad 8 (Manual)

- Download latest **.lpx6** file from the link provided above.
- Click "Add connection" in LINQPad.
- In the "Choose Data Context" dialog, press the "View more drivers..." button.
- In the "LINQPad NuGet Manager" dialog, press the "Install driver from .LPX6 file..." button.
- Select the downloaded file and click the "Open" button.
- In the "Choose Data Context" dialog, select the "LINQ to DB" driver and click the "Next" button.
- In the "LINQ to DB connection" dialog, supply your connection information.
- You're done.

### LINQPad 5 (Choose a driver)

- Click "Add connection" in LINQPad.
- In the "Choose Data Context" dialog, press the "View more drivers..." button.
- In the "Choose a Driver" dialog, search for "LINQ to DB Driver".
- Click "Download & Enable Driver" link to install/update to latest driver release
- In the "Choose Data Context" dialog, select the "LINQ to DB" driver and click the "Next" button.
- In the "LINQ to DB connection" dialog, supply your connection information.
- You're done.

### LINQPad 5 (Manual)

- Download latest **.lpx** file from the link provided above.
- Click "Add connection" in LINQPad.
- In the "Choose Data Context" dialog, press the "View more drivers..." button.
- In the "Choose a Driver" dialog, press the "Browse..." button.
- Select the downloaded file and click the "Open" button.
- In the "Choose Data Context" dialog, select the "LINQ to DB" driver and click the "Next" button.
- In the "LINQ to DB connection" dialog, supply your connection information.
- You're done.
