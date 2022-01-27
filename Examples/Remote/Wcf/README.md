# How to run

First of all, you need to install [Northwind DB](https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/downloading-sample-databases) from Microsoft to your SQL Server instance.

Host
---------------

1. Modify 'connectionString' section into your App.config.

Client
------

1. Modify 'connectionString' section in the beginning of your Northwind.tt file.
2. Save the Northwind.tt file, so Northwind.generated.cs file will arise.


After all, rebuild your solution, run Host first, then run Client.
