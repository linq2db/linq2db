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
		// Both engines can host the schema but not the store's write model, so the gates are per-test rather
		// than a blanket provider exclusion - the read-only and claim tests pass on both.
		protected const string ClickHouseGate = "ClickHouse reports no affected-rows count and has no UPDATE ... RETURNING, so the store's optimistic update and delete cannot tell a conflict from a success; InsertOrUpdate is unsupported outright.";
		protected const string YdbGate        = "linq2db's YDB provider implements no InsertOrUpdate, which the role and token upserts need, and reports no affected-rows count for the optimistic delete.";

		// The table itself creates (without its key - see DefaultMappings); it is the lookups that cannot run.
		protected const string InformixPasskeyGate = "A 1024-byte credential id fits only an Informix BYTE column, and BYTE values cannot appear in a comparison - so every passkey lookup filters on a column Informix refuses to filter on.";

		// ASP.NET Core Identity entities carry non-deterministic values - GUID Ids (IdentityUser/IdentityRole default
		// ctors), the security stamp, and the optimistic-concurrency stamp are all freshly generated per run. The SQL
		// is structurally identical between the direct and remote (LinqService) paths; only those random parameter
		// VALUES differ run-to-run, so the direct-vs-remote SQL baseline comparison can never match. These fixtures
		// verify store behaviour, not SQL text - opt out of baseline capture (matches the DisableBaseline precedents).
		public override void OnBeforeTest()
		{
			base.OnBeforeTest();
			CustomTestContext.Get().Set(CustomTestContext.BASELINE_DISABLED, true);
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
		// the eight type parameters of IdentityDataConnection<...> don't include one, and the fixtures that need
		// passkeys are string-keyed and use Schema above.
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
