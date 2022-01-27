# How to run

First of all, you need to install [Northwind DB](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/downloading-sample-databases) from Microsoft to your SQL Server instance.

WebHost
---------------

1. Modify 'connectionString' section into your Web.config.

Client
------

1. Modify 'connectionString' section at the end of your Northwind.tt file.
2. Save the Northwind.tt file, so Northwind.generated.cs file will arise.


After all:
1. Rebuild your solution
2. Run WebHost first, and append to the address "/LinqWcfService.svc". A page about your service information should appear.
3. Then run Client.
