# How to run

First of all, you need to install [Northwind DB](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/downloading-sample-databases) from Microsoft to your SQL Server instance.

1. Server: Modify connection string in Startup.cs file.
2. Client: Modify connection string parameter in LoadSqlServerMetadata method call in Northwind.tt file.
3. Client: Save the Northwind.tt file to generate database model.
4. Build
5. Start Server
6. Start Client
