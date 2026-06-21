# linq2db.Identity<!-- omit in toc -->

`linq2db.Identity` is an [ASP.NET Core Identity](https://learn.microsoft.com/aspnet/core/security/authentication/identity)
persistence provider built on [LINQ to DB](https://github.com/linq2db/linq2db). It implements the Identity store
contracts (`IUserStore`, `IRoleStore`, and the full family of optional interfaces) on top of a linq2db `IDataContext`,
so you get Identity's `UserManager` / `RoleManager` / `SignInManager` backed by linq2db's fast, type-safe SQL instead
of Entity Framework Core.

It uses the **standard `Microsoft.AspNetCore.Identity` entity types** (`IdentityUser<TKey>`, `IdentityRole<TKey>`, …)
and produces the **same `AspNet*` database schema** as the EF Core provider — so it is a drop-in storage backend for an
existing Identity database.

* [Why use it](#why-use-it)
* [Install](#install)
* [Quick start (DI)](#quick-start-di)
* [The identity context](#the-identity-context)
* [Custom key types (int / Guid)](#custom-key-types-int--guid)
* [What's supported](#whats-supported)
* [Navigations](#navigations)
* [Supported frameworks](#supported-frameworks)
* [Migrating from the standalone `linq2db.Identity`](#migrating-from-the-standalone-linq2dbidentity)

## Why use it

* **No EF Core dependency** — Identity storage on linq2db alone.
* **Same schema** as the EF Core Identity provider (`AspNetUsers`, `AspNetRoles`, …) — compatible with existing
  databases, no migration required.
* **Fast, type-safe** SQL via linq2db; automatic optimistic concurrency (the `ConcurrencyStamp` is a linq2db
  optimistic-lock column).
* Works over the **direct** connection and the **remote [LinqService](https://linq2db.github.io/articles/general/Client-Server.html)** path.
* Arbitrary key types (`string`, `Guid`, `int`, `long`, …).

## Install

```sh
dotnet add package linq2db.Identity
```

The matching `Microsoft.Extensions.Identity.Stores` version is pulled in automatically.

## Quick start (DI)

Register a linq2db identity context, then point the Identity stores at it with `AddLinqToDBStores<TContext>()`
(an `IdentityBuilder` extension in the `Microsoft.Extensions.DependencyInjection` namespace):

```csharp
using LinqToDB;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;

// 1. register the linq2db context (any IDataContext works: DataConnection / DataContext / a remote context)
services.AddScoped(_ => new IdentityDataConnection(
    new DataOptions().UseSqlServer(connectionString)));

// 2. wire Identity to the linq2db stores
services.AddIdentity<IdentityUser, IdentityRole>()
    .AddLinqToDBStores<IdentityDataConnection>()
    .AddDefaultTokenProviders();
```

`AddLinqToDBStores<TContext>` infers the user / role / key / entity types from the `IdentityBuilder` and (when
`TContext` derives from `IdentityDataConnection<…>` / `IdentityDataContext<…>`) from the context, registering the
matching `UserStore` / `RoleStore` — or a `UserOnlyStore` when no role type is configured.

Creating the schema (e.g. in tests or first-run setup) is ordinary linq2db DDL — `CreateTable<IdentityUser>()`,
`CreateTable<IdentityRole>()`, and so on for the seven (eight on .NET 10 with passkeys) `AspNet*` tables.

## The identity context

`IdentityDataConnection` / `IdentityDataContext` (and their generic `<TUser, TRole, TKey, …>` forms) apply the default
`AspNet*` fluent mappings for you:

* `IdentityDataConnection` (based on `DataConnection`) — keeps the connection open for its lifetime.
* `IdentityDataContext` (based on `DataContext`) — opens a connection per query and closes it afterwards (EF-like).

You can also use **any** `IDataContext` and configure the mappings yourself — the default mappings live in
`LinqToDB.Identity.Mapping.DefaultMappings` and are public.

To customize a column for a specific provider, override `ConfigureMappings` (it is `protected virtual`) or layer an
additional `MappingSchema` via `DataOptions.UseMappingSchema(...)`. For example, the .NET 10 passkey `Data` column
defaults to an unbounded `NVarChar` (`nvarchar(max)` on SQL Server); on Oracle, where `NVARCHAR2` is capped at 4000
bytes, map it to `DataType.NText` (`NCLOB`):

```csharp
protected override void ConfigureMappings(MappingSchema ms)
{
    base.ConfigureMappings(ms);
    new FluentMappingBuilder(ms)
        .Entity<IdentityUserPasskey<string>>()
            .Property(e => e.Data).HasDataType(DataType.NText)
        .Build();
}
```

## Custom key types (int / Guid)

Arbitrary `TKey` is supported via the generic context and stores:

```csharp
public class AppUser : IdentityUser<int> { }
public class AppRole : IdentityRole<int> { }

services.AddScoped(_ => new IdentityDataConnection<AppUser, AppRole, int>(options));
services.AddIdentityCore<AppUser>().AddRoles<AppRole>()
    .AddLinqToDBStores<IdentityDataConnection<AppUser, AppRole, int>>();
```

* **int / long** keys are mapped as store-generated **identity** columns (assigned on insert), matching EF Core.
* **string / Guid** keys are client-assigned (set `Id` yourself, as Identity's `IdentityUser` does for strings).
* `UserManager.FindByIdAsync` takes a **string** — that is the ASP.NET Core framework signature; the store converts it
  to your key type, so pass `id.ToString()`.

## What's supported

The full modern store contract:

* User CRUD, claims, logins, external tokens.
* Roles, role claims, user-role membership.
* Password hash, security stamp, email/phone confirmation, two-factor, lockout.
* Authenticator keys and two-factor **recovery codes**.
* `IProtectedUserStore` and the queryable user/role stores.
* Roleless **`UserOnlyStore`**.
* **Passkeys** (`IUserPasskeyStore`) on .NET 10.

## Navigations

Association helpers for the standard Identity entity types are provided as **extension methods** (extension
*properties* can't be used in expression trees), usable inside any linq2db query:

| Receiver | Method | Related rows |
|---|---|---|
| `IdentityUser<TKey>` | `.Roles()`  | `AspNetUserRoles`  for the user |
| `IdentityUser<TKey>` | `.Claims()` | `AspNetUserClaims` |
| `IdentityUser<TKey>` | `.Logins()` | `AspNetUserLogins` |
| `IdentityRole<TKey>` | `.Users()`  | `AspNetUserRoles`  for the role |
| `IdentityRole<TKey>` | `.Claims()` | `AspNetRoleClaims` |

```csharp
using LinqToDB;
using LinqToDB.Identity;

var roleIds = db.GetTable<IdentityUser>()
    .Where(u => u.NormalizedUserName == "ALICE")
    .SelectMany(u => u.Roles())
    .Select(r => r.RoleId)
    .ToList();
```

They are query-only markers — linq2db expands them into the corresponding join; calling one outside a query throws.

## Supported frameworks

`net462`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. (Passkeys are .NET 10 only.)

## Migrating from the standalone `linq2db.Identity`

The package id stays **`linq2db.Identity`**; the monorepo build is a new **major** version. The default database schema
is **unchanged** — existing `AspNet*` tables keep working with no DB migration. The breaking changes are all at the API
level.

| Area | Before (standalone) | After (monorepo) |
|---|---|---|
| Entity model | custom `IIdentityUser<TKey>` / `IIdentityRole<TKey>` / `IIdentity*` interface family | standard `Microsoft.AspNetCore.Identity.IdentityUser<TKey>` / `IdentityRole<TKey>` base types |
| Context wiring | `IConnectionFactory` / `DefaultConnectionFactory` | a linq2db `IDataContext` registered in DI (e.g. `IdentityDataConnection`) |
| DI registration | `AddLinqToDBStores(new DefaultConnectionFactory())` (+ entity-type generic args) | `AddLinqToDBStores<TContext>()` |
| Concurrency stamp | manual | automatic (linq2db `OptimisticLockPropertyAttribute` / `VersionBehavior`) |
| DB schema | `AspNet*` | **identical** `AspNet*` |
| TFMs | `netstandard2.0` | `net462; netstandard2.0; net8.0; net9.0; net10.0` |

**Entity model.** Replace types implementing the old custom interfaces with subclasses of the ASP.NET Core base types
(or use the defaults). The custom `IIdentityUser` / `IIdentityRole` / `IIdentityUserClaim` / … interfaces and
`IConcurrency` / `IClameConverter` are gone:

```diff
- public class ApplicationUser : IIdentityUser<string> { … }
+ public class ApplicationUser : Microsoft.AspNetCore.Identity.IdentityUser<string> { … }
```

**DI wiring.** `IConnectionFactory` is removed. Register your linq2db context with DI and point the stores at it with
`AddLinqToDBStores<TContext>()`:

```diff
- services.AddIdentity<ApplicationUser, IdentityRole>()
-     .AddLinqToDBStores(new DefaultConnectionFactory())
-     .AddDefaultTokenProviders();
+ services.AddScoped(_ => new IdentityDataConnection(
+     new DataOptions().UseConnectionString(providerName, connectionString)));
+
+ services.AddIdentity<IdentityUser, IdentityRole>()
+     .AddLinqToDBStores<IdentityDataConnection>()
+     .AddDefaultTokenProviders();
```

**Database.** No schema change — the default mapping produces the same `AspNetUsers` / `AspNetRoles` /
`AspNetUserClaims` / `AspNetUserRoles` / `AspNetUserLogins` / `AspNetUserTokens` / `AspNetRoleClaims` layout (plus
`AspNetUserPasskeys` on .NET 10). Existing databases are compatible as-is.
