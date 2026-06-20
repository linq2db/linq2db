# LINQ to DB ASP.NET Core Identity provider

`linq2db.Identity` provides [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
user and role stores implemented over [LINQ to DB](https://github.com/linq2db/linq2db).

It builds on the standard `Microsoft.Extensions.Identity.Stores` abstractions
(`IdentityUser<TKey>`, `IdentityRole<TKey>`, `UserStoreBase`, `RoleStoreBase`), so the entity
model and default `AspNet*` database schema match the Entity Framework Core implementation —
existing Identity databases keep working unchanged.

## Usage

```csharp
services
    .AddIdentity<IdentityUser, IdentityRole>()
    .AddLinqToDBStores<IdentityDataConnection>()
    .AddDefaultTokenProviders();
```

Register the linq2db context (`IDataContext` / `DataConnection` / `DataContext`) with your DI
container as usual; the stores resolve it from there. Both a roles-aware `UserStore` and a
roleless `UserOnlyStore` are available, with `string`, `int`, `Guid`, and custom key types.

> This package supersedes the standalone `linq2db.Identity` package. The model now derives from
> the `Microsoft.AspNetCore.Identity` base types instead of the previous custom interface family,
> and `IConnectionFactory` has been removed in favour of standard DI. See the migration notes in
> the linq2db wiki.
