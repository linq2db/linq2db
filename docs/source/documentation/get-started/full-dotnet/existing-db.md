---
title: Getting Started on .NET Framework - Existing Database - LINQ To DB
author: sdanyliv
---

> [!WARNING]
> T4 templates are obsoleted and replaced with new [`dotnet tool`](https://www.nuget.org/packages/linq2db.cli). Tool documentation could be found [here](xref:cli).

# Getting started with LINQ To DB with an Existing Database

In this walkthrough, you will build a console application that performs basic data access against a Microsoft SQL Server database using LINQ To DB. You will use existing database to create your model.

> [!TIP]  
> You can view this article's [sample](https://github.com/linq2db/examples/tree/master/SqlServer/GetStarted) on GitHub.

## Prerequisites

The following prerequisites are needed to complete this walkthrough:

* [Visual Studio](https://visualstudio.microsoft.com/downloads/)

* [Northwind Database](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/downloading-sample-databases)

> [!TIP]  
> [Northwind Database script file](https://github.com/linq2db/linq2db/blob/master/Data/northwind.sql.zip), which could be executed using Microsoft SQL Server Management Studio

## Create new project

* Open Visual Studio

* File > New > Project...

* From the left menu select Templates > Visual C# > Windows Classic Desktop

* Select the **Console App (.NET Framework)** project template

* Ensure you are targeting **.NET Framework 4.5.1** or later

* Give the project a name and click **OK**

## Install LINQ To DB

To use LINQ To DB, install the package for the database provider(s) you want to target. This walkthrough uses SQL Server. For a list of available providers see [Database Providers](xref:databases).

* Tools > NuGet Package Manager > Package Manager Console

* Run `Install-Package linq2db.SqlServer`

## Generate model from database

Now it's time to generate your model from database.

* Project > New Folder...

* Enter *DataModels* as the name and click **OK**

* DataModels > Add Item...  and select **Text Template**

* Enter *Northwind.tt* as the name and click **OK**

* Copy the content from the file Project > *LinqToDB.Templates\CopyMe.SqlServer.tt.txt*

* Modify host, database name and credentials for your SQL Server in `LoadSqlServerMetadata` function call

* Save *Northwind.tt* - it should invoke model generation

> [!TIP]  
> There are many ways to customize generation process. Follow this [link](xref:scaffold-t4) for details.

## Use your model

You can now use your model to perform data access.

* Open *App.config*
* Replace the contents of the file with the following XML (correct connection string based on your server location and credentials)

<!-- [!code-xml[Config](https://github.com/linq2db/examples/blob/master/SqlServer/GetStarted/App.config)] -->
```xml
<?xml version="1.0" encoding="utf-8"?>

<configuration>
  <connectionStrings>
    <add name="MyDatabase" providerName="System.Data.SqlClient"
         connectionString="Data Source=.;Database=Northwind;Integrated Security=SSPI;" />
  </connectionStrings>
</configuration>
```

* Open *Program.cs*

* Replace the contents of the file with the following code

<!-- [!code-csharp[Main](https://github.com/linq2db/examples/blob/master/SqlServer/GetStarted/Program.cs)] -->
```csharp
using System;
using System.Linq;

namespace GetStarted
{
	class Program
	{
		static void Main(string[] args)
		{
			using (var db = new DataModel.NorthwindDB())
			{
				var q =
					from c in db.Customers
					select c;

				foreach (var c in q)
					Console.WriteLine(c.ContactName);
			}
		}
	}
}
```

* Debug > Start Without Debugging

You will see list of Contact names.

![image](static/output-existing-db.png)
