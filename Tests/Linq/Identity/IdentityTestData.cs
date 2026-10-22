using System;
using System.Collections.Generic;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;

using Microsoft.AspNetCore.Identity;

namespace Tests.Identity
{
	// Shared helpers for the identity store fixtures: schema setup (the default string-key AspNet* model),
	// the direct setup connection (also the source of the mapping schema fed to direct + LinqService contexts),
	// and entity factories. DDL always runs on a direct connection - it can't go over the remote path.
	public abstract class IdentityTestData : TestBase
	{
		// ClickHouse hosts the schema and runs everything but the optimistic update and identity keys, so the gates
		// are per-test rather than a blanket provider exclusion. Each gate pins the failure its mechanism produces,
		// so a case failing for any other reason still reports.
		protected const string RefreshGate   = "ClickHouse has neither UPDATE ... RETURNING nor an affected-rows count, so UpdateOptimisticWithRefresh refuses the store's optimistic update.";
		protected const string RefreshError  = "UpdateOptimisticWithRefresh requires the provider";
		protected const string IdentityGate  = "ClickHouse has no identity columns, so a database-generated int key is never read back.";
		protected const string IdentityError = "should be greater than{0}0";

		// ASP.NET Core Identity entities carry non-deterministic values - GUID Ids (IdentityUser/IdentityRole default
		// ctors), the security stamp, and the optimistic-concurrency stamp are all freshly generated per run. The SQL
		// is structurally identical between the direct and remote (LinqService) paths; only those random parameter
		// VALUES differ run-to-run, so the direct-vs-remote SQL baseline comparison can never match. These fixtures
		// verify store behaviour, not SQL text - opt out of baseline capture (matches the DisableBaseline precedents).
		public override void OnBeforeTest()
		{
			base.OnBeforeTest();
			CustomTestContext
				.Get()
				.Set(CustomTestContext.BASELINE_DISABLED, true);
		}

		// Creates the default string-key AspNet* schema and drops it on dispose. CreateLocalTable is
		// drop-then-create, so it also covers a prior (possibly crashed) run that left tables in the reused
		// database file - and it carries the Firebird pool eviction that provider's DDL needs between tests,
		// which a bare CreateTable/DropTable pair fails without ("object TABLE ... is in use").
		protected sealed class Schema : IDisposable
		{
			readonly List<IDisposable> _tables = [];

			public Schema(IdentityDataConnection db)
			{
				_tables.Add(db.CreateLocalTable<IdentityUser>());
				_tables.Add(db.CreateLocalTable<IdentityRole>());
				_tables.Add(db.CreateLocalTable<IdentityUserClaim <string>>());
				_tables.Add(db.CreateLocalTable<IdentityUserRole  <string>>());
				_tables.Add(db.CreateLocalTable<IdentityUserLogin <string>>());
				_tables.Add(db.CreateLocalTable<IdentityUserToken <string>>());
				_tables.Add(db.CreateLocalTable<IdentityRoleClaim <string>>());
#if NET10_0_OR_GREATER
				_tables.Add(db.CreateLocalTable<IdentityUserPasskey<string>>());
#endif
			}

			public void Dispose()
			{
				for (var i = _tables.Count - 1; i >= 0; i--)
					_tables[i].Dispose();
			}
		}

		// The same thing for an arbitrary key type and satellite set, via the generic contexts. No passkey table:
		// the eight type parameters of IdentityDataConnection<...> don't include one - a fixture that needs passkeys
		// uses Schema above, or creates its custom passkey table itself.
		protected sealed class KeyedSchema<TUser, TRole, TKey, TUserClaim, TUserRole, TUserLogin, TRoleClaim, TUserToken> : IDisposable
			where TKey       : IEquatable<TKey>
			where TUser      : IdentityUser     <TKey>
			where TRole      : IdentityRole     <TKey>
			where TUserClaim : IdentityUserClaim<TKey>
			where TUserRole  : IdentityUserRole <TKey>
			where TUserLogin : IdentityUserLogin<TKey>
			where TRoleClaim : IdentityRoleClaim<TKey>
			where TUserToken : IdentityUserToken<TKey>
		{
			readonly List<IDisposable> _tables = [];

			public KeyedSchema(IDataContext db)
			{
				_tables.Add(db.CreateLocalTable<TUser>());
				_tables.Add(db.CreateLocalTable<TRole>());
				_tables.Add(db.CreateLocalTable<TUserClaim>());
				_tables.Add(db.CreateLocalTable<TUserRole >());
				_tables.Add(db.CreateLocalTable<TUserLogin>());
				_tables.Add(db.CreateLocalTable<TUserToken>());
				_tables.Add(db.CreateLocalTable<TRoleClaim>());
			}

			public void Dispose()
			{
				for (var i = _tables.Count - 1; i >= 0; i--)
					_tables[i].Dispose();
			}
		}

		protected sealed class KeyedSchema<TUser, TRole, TKey> : IDisposable
			where TKey  : IEquatable<TKey>
			where TUser : IdentityUser<TKey>
			where TRole : IdentityRole<TKey>
		{
			readonly KeyedSchema<TUser, TRole, TKey, IdentityUserClaim<TKey>, IdentityUserRole<TKey>, IdentityUserLogin<TKey>, IdentityRoleClaim<TKey>, IdentityUserToken<TKey>> _schema;

			public KeyedSchema(IDataContext db) => _schema = new (db);

			public void Dispose() => _schema.Dispose();
		}

		protected static IdentityDataConnection GetSetup(string context)
			=> new (new DataOptions().UseConfiguration(context.StripRemote()));

		protected static IdentityUser NewUser(string name)
			=> new (name) { NormalizedUserName = name.ToUpperInvariant(), Email = name + "@test.com", NormalizedEmail = (name + "@test.com").ToUpperInvariant() };

		protected static IdentityRole NewRole(string name)
			=> new (name) { NormalizedName = name.ToUpperInvariant() };
	}
}
