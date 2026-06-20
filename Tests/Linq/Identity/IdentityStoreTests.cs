using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;

using Microsoft.AspNetCore.Identity;

using NUnit.Framework;

namespace Tests.Identity
{
	[TestFixture]
	public class IdentityStoreTests : TestBase
	{
		// Creates the default AspNet* schema for the identity context and drops it on dispose.
		sealed class Schema : System.IDisposable
		{
			readonly IdentityDataConnection _db;

			public Schema(IdentityDataConnection db)
			{
				_db = db;

				// Drop first in case a prior (possibly crashed) run left tables in the reused database file.
				Drop();

				_db.CreateTable<IdentityUser>();
				_db.CreateTable<IdentityRole>();
				_db.CreateTable<IdentityUserClaim <string>>();
				_db.CreateTable<IdentityUserRole  <string>>();
				_db.CreateTable<IdentityUserLogin <string>>();
				_db.CreateTable<IdentityUserToken <string>>();
				_db.CreateTable<IdentityRoleClaim <string>>();
			}

			public void Dispose() => Drop();

			void Drop()
			{
				_db.DropTable<IdentityRoleClaim <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserToken <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserLogin <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserRole  <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserClaim <string>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityRole>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUser>(throwExceptionIfNotExists: false);
			}
		}

		static IdentityDataConnection GetDb(string context)
			=> new (new DataOptions().UseConfiguration(context));

		static IdentityUser NewUser(string name)
			=> new (name) { NormalizedUserName = name.ToUpperInvariant(), Email = name + "@test.com", NormalizedEmail = (name + "@test.com").ToUpperInvariant() };

		static IdentityRole NewRole(string name)
			=> new (name) { NormalizedName = name.ToUpperInvariant() };

		[Test]
		public async Task UserCrud([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDb(context);
			using var schema = new Schema(db);

			var store = new UserStore<IdentityUser>(db);
			var user  = NewUser("alice");

			Assert.That((await store.CreateAsync(user)).Succeeded, Is.True);

			var byId = await store.FindByIdAsync(user.Id);
			Assert.That(byId,             Is.Not.Null);
			Assert.That(byId!.UserName,   Is.EqualTo("alice"));

			var byName = await store.FindByNameAsync("ALICE");
			Assert.That(byName,     Is.Not.Null);
			Assert.That(byName!.Id, Is.EqualTo(user.Id));

			await store.SetEmailAsync(byName, "new@test.com");
			await store.SetNormalizedEmailAsync(byName, "NEW@TEST.COM");
			Assert.That((await store.UpdateAsync(byName)).Succeeded, Is.True);

			var byEmail = await store.FindByEmailAsync("NEW@TEST.COM");
			Assert.That(byEmail,     Is.Not.Null);
			Assert.That(byEmail!.Id, Is.EqualTo(user.Id));

			Assert.That((await store.DeleteAsync(byEmail)).Succeeded, Is.True);
			Assert.That(await store.FindByIdAsync(user.Id), Is.Null);
		}

		[Test]
		public async Task RoleCrud([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDb(context);
			using var schema = new Schema(db);

			var store = new RoleStore<IdentityRole>(db);
			var role  = NewRole("admin");

			Assert.That((await store.CreateAsync(role)).Succeeded, Is.True);

			var byName = await store.FindByNameAsync("ADMIN");
			Assert.That(byName,       Is.Not.Null);
			Assert.That(byName!.Name, Is.EqualTo("admin"));

			Assert.That((await store.DeleteAsync(byName)).Succeeded, Is.True);
			Assert.That(await store.FindByIdAsync(role.Id), Is.Null);
		}

		[Test]
		public async Task UserRolesAndClaims([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDb(context);
			using var schema = new Schema(db);

			var users = new UserStore<IdentityUser>(db);
			var roles = new RoleStore<IdentityRole>(db);

			var user = NewUser("bob");
			var role = NewRole("editor");
			await users.CreateAsync(user);
			await roles.CreateAsync(role);

			await users.AddToRoleAsync(user, "EDITOR");

			Assert.That(await users.IsInRoleAsync(user, "EDITOR"), Is.True);
			Assert.That(await users.GetRolesAsync(user),          Has.Member("editor"));
			Assert.That((await users.GetUsersInRoleAsync("EDITOR")).Select(u => u.Id), Has.Member(user.Id));

			await users.AddClaimsAsync(user, new[] { new Claim("scope", "read"), new Claim("scope", "write") });
			var claims = await users.GetClaimsAsync(user);
			Assert.That(claims.Select(c => c.Value), Is.EquivalentTo(new[] { "read", "write" }));

			await users.RemoveClaimsAsync(user, new[] { new Claim("scope", "read") });
			Assert.That((await users.GetClaimsAsync(user)).Select(c => c.Value), Is.EquivalentTo(new[] { "write" }));

			await users.RemoveFromRoleAsync(user, "EDITOR");
			Assert.That(await users.IsInRoleAsync(user, "EDITOR"), Is.False);
		}

		[Test]
		public async Task LoginsAndTokens([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDb(context);
			using var schema = new Schema(db);

			var store = new UserStore<IdentityUser>(db);
			var user  = NewUser("carol");
			await store.CreateAsync(user);

			await store.AddLoginAsync(user, new UserLoginInfo("google", "key-123", "Google"));
			var byLogin = await store.FindByLoginAsync("google", "key-123");
			Assert.That(byLogin,     Is.Not.Null);
			Assert.That(byLogin!.Id, Is.EqualTo(user.Id));

			var logins = await store.GetLoginsAsync(user);
			Assert.That(logins.Select(l => l.ProviderKey), Has.Member("key-123"));

			await store.SetTokenAsync(user, "google", "refresh", "tok-value", default);
			Assert.That(await store.GetTokenAsync(user, "google", "refresh", default), Is.EqualTo("tok-value"));

			await store.RemoveLoginAsync(user, "google", "key-123");
			Assert.That(await store.FindByLoginAsync("google", "key-123"), Is.Null);
		}
	}
}
