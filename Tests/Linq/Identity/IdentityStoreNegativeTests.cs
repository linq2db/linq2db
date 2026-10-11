using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Identity;

using Microsoft.AspNetCore.Identity;

using NUnit.Framework;

using Shouldly;

namespace Tests.Identity
{
	[TestFixture]
	public class IdentityStoreNegativeTests : IdentityTestData
	{
		[Test]
		public void NullArguments([DataSources] string context)
		{
			// no schema needed: every assertion throws during argument validation before touching the database.
			using var setup = GetSetup(context);
			using var ctx   = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);
			var user  = NewUser("nullarg");

			Assert.ThrowsAsync<ArgumentNullException>(() => users.CreateAsync(null!));
			Assert.ThrowsAsync<ArgumentNullException>(() => users.UpdateAsync(null!));
			Assert.ThrowsAsync<ArgumentNullException>(() => users.DeleteAsync(null!));
			Assert.ThrowsAsync<ArgumentNullException>(() => users.AddClaimsAsync(user, null!));
			Assert.ThrowsAsync<ArgumentNullException>(() => users.AddLoginAsync(user, null!));
			Assert.ThrowsAsync<ArgumentNullException>(() => users.GetUsersForClaimAsync(null!));
			Assert.ThrowsAsync<ArgumentNullException>(() => roles.CreateAsync(null!));
			Assert.ThrowsAsync<ArgumentNullException>(() => roles.AddClaimAsync(NewRole("r"), null!));
		}

		[Test]
		public async Task RoleNameValidation([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("rolename");

			// empty/whitespace role name
			Assert.ThrowsAsync<ArgumentException>(() => users.AddToRoleAsync(user, " "));
			Assert.ThrowsAsync<ArgumentException>(() => users.RemoveFromRoleAsync(user, " "));
			Assert.ThrowsAsync<ArgumentException>(() => users.IsInRoleAsync(user, " "));

			// role does not exist
			await users.CreateAsync(user);
			Assert.ThrowsAsync<InvalidOperationException>(() => users.AddToRoleAsync(user, "DOES-NOT-EXIST"));
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		public async Task ConcurrencyFailureOnUpdate([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var fresh = NewUser("conc-fresh");
			var user  = NewUser("conc-user");
			var role  = NewRole("conc-role");
			await users.CreateAsync(fresh);
			await users.CreateAsync(user);
			await roles.CreateAsync(role);

			(await users.UpdateAsync(fresh)).Succeeded.ShouldBeTrue();

			// Corrupt the stored ConcurrencyStamp out-of-band so the optimistic filter no longer matches. Through
			// ctx, not the setup connection: Access does not show one connection the rows another just inserted,
			// so the same statement there matches 0 rows and the test's premise silently evaporates.
			await ctx.GetTable<IdentityUser>().Where(u => u.Id == user.Id).Set(u => u.ConcurrencyStamp, "stale").UpdateAsync();
			await ctx.GetTable<IdentityRole>().Where(r => r.Id == role.Id).Set(r => r.ConcurrencyStamp, "stale").UpdateAsync();

			(await users.UpdateAsync(user)).Succeeded.ShouldBeFalse();
			(await roles.UpdateAsync(role)).Succeeded.ShouldBeFalse();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task ConcurrencyFailureOnDelete([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var fresh = NewUser("conc-fresh");
			var user  = NewUser("conc-user");
			var role  = NewRole("conc-role");
			await users.CreateAsync(fresh);
			await users.CreateAsync(user);
			await roles.CreateAsync(role);

			(await users.DeleteAsync(fresh)).Succeeded.ShouldBeTrue();

			// see ConcurrencyFailureOnUpdate for why this goes through ctx
			await ctx.GetTable<IdentityUser>().Where(u => u.Id == user.Id).Set(u => u.ConcurrencyStamp, "stale").UpdateAsync();
			await ctx.GetTable<IdentityRole>().Where(r => r.Id == role.Id).Set(r => r.ConcurrencyStamp, "stale").UpdateAsync();

			(await users.DeleteAsync(user)).Succeeded.ShouldBeFalse();
			(await roles.DeleteAsync(role)).Succeeded.ShouldBeFalse();
		}

		[Test]
		public void DisposedStore([DataSources] string context)
		{
			// no schema needed: the disposed-guard throws before any database access.
			using var setup = GetSetup(context);
			using var ctx   = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			store.Dispose();

			Assert.ThrowsAsync<ObjectDisposedException>(() => store.CreateAsync(NewUser("zed")));
			Assert.ThrowsAsync<ObjectDisposedException>(() => store.FindByIdAsync("any"));
		}

		[Test]
		public async Task NotFoundReturnsNull([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			Assert.That(await users.FindByIdAsync(Guid.NewGuid().ToString()), Is.Null);
			Assert.That(await users.FindByNameAsync("NOSUCH"),                Is.Null);
			Assert.That(await users.FindByEmailAsync("NOSUCH@TEST.COM"),      Is.Null);
			Assert.That(await users.FindByLoginAsync("p", "k"),              Is.Null);
			Assert.That(await roles.FindByIdAsync(Guid.NewGuid().ToString()), Is.Null);
			Assert.That(await roles.FindByNameAsync("NOSUCH"),               Is.Null);
		}

		[Test]
		public async Task IdempotentRemovals([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("nora");
			await store.CreateAsync(user);

			// removing things that don't exist is a no-op, not an error
			Assert.DoesNotThrowAsync(() => store.RemoveLoginAsync(user, "missing", "missing"));
			Assert.DoesNotThrowAsync(() => store.RemoveTokenAsync(user, "missing", "missing", default));
			Assert.DoesNotThrowAsync(() => store.RemoveClaimsAsync(user, new[] { new Claim("none", "none") }));
		}
	}
}
