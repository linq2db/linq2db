List of existing Entity Framework providers: https://learn.microsoft.com/en-us/ef/core/providers/?tabs=dotnet-core-cli

# Tested providers

| Tests Folder | Provider Nuget | Database |
|-|-|-|
| `Npgsql` | [Npgsql.EntityFrameworkCore.PostgreSQL](https://www.nuget.org/packages/Npgsql.EntityFrameworkCore.PostgreSQL) | PostgreSQL |
| `Pomelo` | [Pomelo.EntityFrameworkCore.MySql](https://www.nuget.org/packages/Pomelo.EntityFrameworkCore.MySql) | MySQL and MariaDB |
| `SQLite` | [Microsoft.EntityFrameworkCore.Sqlite](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Sqlite) | SQLite |
| `SQLServer` | [Microsoft.EntityFrameworkCore.SqlServer](https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer) | SQL Server incl. Azure SQL |

# How to add new EF provider to tests

- ensure `Linq To DB` supports provider's database
- add folder for provider and document it in this file
- add references to provider nuget(s)
- TBD
