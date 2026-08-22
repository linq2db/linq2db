using System;

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

		// Creates the default string-key AspNet* schema and drops it on dispose. Drop-then-create is idempotent,
		// covering a prior (possibly crashed) run that left tables in the reused database file.
		protected sealed class Schema : IDisposable
		{
			readonly IdentityDataConnection _db;

			public Schema(IdentityDataConnection db)
			{
				_db = db;
				Drop();

				_db.CreateTable<IdentityUser>();
				_db.CreateTable<IdentityRole>();
				_db.CreateTable<IdentityUserClaim <string>>();
				_db.CreateTable<IdentityUserRole  <string>>();
				_db.CreateTable<IdentityUserLogin <string>>();
				_db.CreateTable<IdentityUserToken <string>>();
				_db.CreateTable<IdentityRoleClaim <string>>();
#if NET10_0_OR_GREATER
				_db.CreateTable<IdentityUserPasskey<string>>();
#endif
			}

			public void Dispose() => Drop();

			void Drop()
			{
#if NET10_0_OR_GREATER
				_db.DropTable<IdentityUserPasskey<string>>(throwExceptionIfNotExists: false);
#endif
				_db.DropTable<IdentityRoleClaim <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserToken <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserLogin <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserRole  <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserClaim <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityRole>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUser>(throwExceptionIfNotExists: false);
			}
		}

		protected static IdentityDataConnection GetSetup(string context)
			=> new (new DataOptions().UseConfiguration(context.StripRemote()));

		protected static IdentityUser NewUser(string name)
			=> new (name) { NormalizedUserName = name.ToUpperInvariant(), Email = name + "@test.com", NormalizedEmail = (name + "@test.com").ToUpperInvariant() };

		protected static IdentityRole NewRole(string name)
			=> new (name) { NormalizedName = name.ToUpperInvariant() };
	}
}
