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
* **Same schema** as the EF Core Identity provider (`AspNetUsers`, `AspNetRoles`, …) — compatible with databases
  EF Core created, no migration required.
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

linq2db DDL creates no foreign keys, so `DeleteAsync` removes a deleted user's claims, logins, tokens and role
memberships (and a deleted role's memberships and claims) itself, where EF Core relies on `ON DELETE CASCADE`.

### Identity options

The stores read **only** `IdentityOptions.Stores.SchemaVersion` (injected as `IOptions<IdentityOptions>`; pass it to
the store constructor when creating a store by hand). It decides whether `DeleteAsync` also removes the user's
passkeys: only at `IdentitySchemaVersions.Version3`, the first schema version with the `AspNetUserPasskeys` table —
the same rule EF Core applies. Without options the stores behave as `Version1`.

No other option is applied — `Stores.MaxLengthForKeys` included. If you need one, please open a
[feature request](https://github.com/linq2db/linq2db/issues/new/choose).

`Stores.ProtectPersonalData` is **not supported**: the stores do not encrypt the `[ProtectedPersonalData]` columns,
so they do not implement `IProtectedUserStore<TUser>`, and `UserManager` refuses the option at construction rather
than storing that data in plaintext.

### `UserManager` operations are not atomic

linq2db has no change tracker, so every store call is written when it runs. A `UserManager` method that makes several
store calls is therefore not one unit of work, as it is under EF Core's single `SaveChanges`: `AddClaimsAsync`
inserts the claims and then updates the user under the optimistic concurrency check, and when that update returns
`ConcurrencyFailure` the claims are already stored. Retrying `AddClaimsAsync` then duplicates them (nothing in
`AspNetUserClaims` prevents it). Retrying `AddLoginAsync` or `AddToRoleAsync` returns a failed result instead
(`LoginAlreadyAssociated` / `UserAlreadyInRole`), because `UserManager` finds the row the first attempt stored. Token
writes are safe to repeat.

To make such an operation atomic, run it in a transaction on the same scoped context the stores resolve, and commit
only on success:

```csharp
await using var tx = await db.BeginTransactionAsync();   // db: the scoped IdentityDataConnection

var result = await userManager.AddClaimsAsync(user, claims);

if (result.Succeeded)
    await tx.CommitAsync();                              // otherwise disposing rolls the claims back
```

## The identity context

`IdentityDataConnection` / `IdentityDataContext` (and their generic `<TUser, TRole, TKey, …>` forms) apply the default
`AspNet*` fluent mappings for you:

* `IdentityDataConnection` (based on `DataConnection`) — keeps the connection open for its lifetime.
* `IdentityDataContext` (based on `DataContext`) — opens a connection per query and closes it afterwards (EF-like).

Deriving from one of those contexts is how you get the default mappings — they are applied by the context
constructor and are not exposed separately.

To customize, override `ConfigureMappings` (it is `protected virtual`): call `base.ConfigureMappings(ms)` first,
then run a second `FluentMappingBuilder` over the same schema. The later pass wins and inherits what the defaults
already set, so a rename keeps the default length and nullability:

```csharp
protected override void ConfigureMappings(MappingSchema ms)
{
    base.ConfigureMappings(ms);
    new FluentMappingBuilder(ms)
        .Entity<IdentityUser>()
            .Property(e => e.Email).HasColumnName("EmailAddress")
        .Build();
}
```

`context.AddMappingSchema(ms)` after construction also outranks the defaults, but a schema built on its own starts
from blank attributes: a rename there replaces the default column mapping instead of inheriting its length and
nullability. `DataOptions.UseMappingSchema(...)` does **not** work at all — the contexts add their own schema in the
constructor, after the options have been applied, so the defaults outrank anything supplied that way.

### Customization limits

* **A column the defaults pin for a specific provider cannot be renamed on that provider by an unscoped mapping.**
  `AspNetUserTokens.UserId` / `LoginProvider` / `Name` on Firebird 2.5, and `AspNetUserPasskeys.CredentialId` on
  Access, Informix and Firebird 2.5, carry provider-scoped mappings that drop a primary key those engines cannot
  index (Informix also stores the id as hex text, as it cannot compare a binary value that wide);
  `AspNetUserPasskeys.Data` on MySQL, Oracle, Firebird, Access, Informix and SQL CE carries one that widens it to a
  text/CLOB column. A provider-scoped mapping outranks an unscoped one, so the rename is dropped with no error. Scope
  your own mapping to the same provider to override it.
* **Some columns carry a provider-scoped database type that outranks an unscoped `HasDataType` / `HasLength`.**
  `ClaimValue` on both claim tables and `AspNetUserTokens.Value` on MySQL, Oracle, Firebird, Access, DB2, Informix,
  SAP HANA, Sybase and SQL CE, and the 256-character name/email columns on Access and Informix, are typed for those
  providers by the defaults. An unscoped `HasDataType` or `HasLength` on them is ignored by `CreateTable` there; set
  `HasDbType` to override.
* **Extra properties on a subclass become columns.** `class AppUser : IdentityUser<string> { public string Tenant { get; set; } }`
  maps `Tenant` as a column, so `CreateTable` emits it and inserts include it — the same as EF Core, but the table is
  then no longer interchangeable with a stock `AspNetUsers` unless you `Ignore` the property.
* **A custom passkey type needs the matching store.** On .NET 10 the contexts and stores take an optional
  `TUserPasskey` (`IdentityDataConnection<TUser, TRole, TKey, …, TUserToken, TUserPasskey>`,
  `UserStore<…, TRoleClaim, TUserPasskey>`, `UserOnlyStore<…, TUserToken, TUserPasskey>`), as EF Core does.
  `AddLinqToDBStores` picks the passkey-aware store for such a context; a store built by hand without the
  `TUserPasskey` argument queries the stock `IdentityUserPasskey<TKey>`, which that context does not map.
* **On .NET 10 the role-aware store and contexts no longer derive from their user-only counterparts.** They derive
  from the passkey-aware forms instead, as in EF Core, so `UserStore<…, TRoleClaim>` is not a
  `UserOnlyStore<…, TUserToken>` there. Code that upcasts between them should target the `IUser*Store<TUser>`
  interfaces or `UserStoreBase<TUser, TKey, TUserClaim, TUserLogin, TUserToken>` instead, which are the same on every
  target framework.
* **Mappings are built once per context type, for the process lifetime.** `ConfigureMappings` runs on the first
  instantiation of a given context class and the result is cached against that class, so mappings cannot vary per
  instance — per-tenant table names, for example.
* **`AddLinqToDBStores<TContext>` applies no mappings when `TContext` is not one of the identity contexts.** The
  stores are still registered and will run, but against tables named after the CLR types.

### Remote (LinqService) clients

A remote context cannot derive from the identity contexts, so give **both ends** the identity mapping schema. Take
it from an identity context built for the provider - constructing one opens no connection:

```csharp
var identitySchema = new IdentityDataConnection(new DataOptions().UseSqlServer(connectionString)).MappingSchema;

// client: the remote context the stores run on
services.AddScoped<IDataContext>(_ => new MyRemoteContext(remoteOptions.UseMappingSchema(identitySchema)));

// server: the DataConnection the LinqService executes on
dataConnection.AddMappingSchema(identitySchema);
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
* The queryable user/role stores.
* Roleless **`UserOnlyStore`**.
* **Passkeys** (`IUserPasskeyStore`) on .NET 10.

### Provider support

Every linq2db provider runs the stores, with one exception. **ClickHouse** reports neither updated rows nor an
affected-rows count for an `UPDATE`, so the optimistic-concurrency check behind `UpdateAsync` cannot be decided
there: user and role updates throw, and every `UserManager` operation that saves the user throws with them. It has no
identity columns either, so int / long keys must be assigned by the caller. Reads, claims, logins, tokens, roles and
deletes work.

On **SAP HANA** a passkey's `Data` column holds at most 5000 characters, the widest `NVARCHAR` it binds: enough for
the default `none` attestation, possibly not for a direct-attestation certificate chain.

## Navigations

Association helpers for the standard Identity entity types are provided as **extension methods** (extension
*properties* can't be used in expression trees), usable inside any linq2db query:

| Receiver | Method | Related rows |
|---|---|---|
| `IdentityUser<TKey>` | `.Roles()`  | `AspNetRoles` the user belongs to (through `AspNetUserRoles`) |
| `IdentityUser<TKey>` | `.Claims()` | `AspNetUserClaims` |
| `IdentityUser<TKey>` | `.Logins()` | `AspNetUserLogins` |
| `IdentityRole<TKey>` | `.Users()`  | `AspNetUsers` in the role (through `AspNetUserRoles`) |
| `IdentityRole<TKey>` | `.Claims()` | `AspNetRoleClaims` |

```csharp
using LinqToDB;
using LinqToDB.Identity;

var roleNames = db.GetTable<IdentityUser>()
    .Where(u => u.NormalizedUserName == "ALICE")
    .SelectMany(u => u.Roles())
    .Select(r => r.Name)
    .ToList();
```

They are query-only markers — linq2db expands them into the corresponding join; calling one outside a query throws.

They return the stock types (`IdentityRole<TKey>`, `IdentityUserClaim<TKey>`, …) even when the context uses your own:
the generic contexts map the stock types to the same `AspNet*` tables for that reason, so your types' extra members
are not visible through a navigation — join your own table on `Id` for those. If your `ConfigureMappings` override
renames a table, rename the stock type's table as well, or query `GetTable<TRole>()` / `GetTable<TUserRole>()`
directly.

## Supported frameworks

`net462`, `netstandard2.0`, `net8.0`, `net9.0`, `net10.0`. (Passkeys are .NET 10 only.)

## Migrating from the standalone `linq2db.Identity`

The package id stays **`linq2db.Identity`** and now follows linq2db's own version line — a *minor* step from the
standalone 6.0.0, but the API is **breaking**: don't let a `6.*` range or an automated minor update pick it up
unreviewed. Existing `AspNet*` tables keep working with **no DB migration**; tables the standalone package created
need their names mapped or renamed — see *Database* below. Tables created by this version's `CreateTable` differ on some providers: wider claim, token and passkey
data columns, and a `DATETIME` `LockoutEnd` on MySQL. Widening an existing table to match is optional; values that
did not fit the old columns never could be stored.

| Area | Before (standalone) | After (monorepo) |
|---|---|---|
| Entity model | custom `IIdentityUser<TKey>` / `IIdentityRole<TKey>` / `IIdentity*` interface family | standard `Microsoft.AspNetCore.Identity.IdentityUser<TKey>` / `IdentityRole<TKey>` base types |
| Context wiring | `IConnectionFactory` / `DefaultConnectionFactory` | a linq2db `IDataContext` registered in DI (e.g. `IdentityDataConnection`) |
| DI registration | `AddLinqToDBStores(new DefaultConnectionFactory())` (+ entity-type generic args) | `AddLinqToDBStores<TContext>()` |
| Concurrency stamp | manual | automatic (linq2db `OptimisticLockPropertyAttribute` / `VersionBehavior`) |
| Table names | none mapped — `CreateTable` used the CLR type names | `AspNet*`, as EF Core |
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

**Database.** The default mapping produces EF Core's `AspNetUsers` / `AspNetRoles` / `AspNetUserClaims` /
`AspNetUserRoles` / `AspNetUserLogins` / `AspNetUserTokens` / `AspNetRoleClaims` layout (plus `AspNetUserPasskeys` on
.NET 10), so a database EF Core created, or one you had already mapped to those names, is compatible as-is.

The standalone package mapped no table names, so a schema it created with `CreateTable<T>()` — as its sample did —
has tables named after the CLR types: `ApplicationUser`, `IdentityRole`, ``IdentityUserClaim`1``, and so on. Either
rename those tables to the `AspNet*` names, or map the old names in a `ConfigureMappings` override:

```csharp
public class AppDataConnection(DataOptions options) : IdentityDataConnection<ApplicationUser>(options)
{
    protected override void ConfigureMappings(MappingSchema ms)
    {
        base.ConfigureMappings(ms);
        new FluentMappingBuilder(ms)
            .Entity<ApplicationUser>()          .HasTableName("ApplicationUser")
            .Entity<IdentityRole>()             .HasTableName("IdentityRole")
            .Entity<IdentityUserClaim<string>>().HasTableName("IdentityUserClaim`1")
            .Entity<IdentityUserRole<string>>() .HasTableName("IdentityUserRole`1")
            .Entity<IdentityUserLogin<string>>().HasTableName("IdentityUserLogin`1")
            .Entity<IdentityUserToken<string>>().HasTableName("IdentityUserToken`1")
            .Entity<IdentityRoleClaim<string>>().HasTableName("IdentityRoleClaim`1")
            .Build();
    }
}
```

The navigation helpers query the stock types, so rename those as well if you use them (see [Navigations](#navigations)).
