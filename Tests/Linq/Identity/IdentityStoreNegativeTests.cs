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
		public async Task NullArguments([DataSources] string context)
		{
			// no schema needed: every assertion throws during argument validation before touching the database.
			using var setup = GetSetup(context);
			using var ctx   = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);
			var user  = NewUser("nullarg");

			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => users.CreateAsync(null!));
			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => users.UpdateAsync(null!));
			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => users.DeleteAsync(null!));
			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => users.AddClaimsAsync(user, null!));
			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => users.AddLoginAsync(user, null!));
			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => users.GetUsersForClaimAsync(null!));
			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => roles.CreateAsync(null!));
			await Shouldly.Should.ThrowAsync<ArgumentNullException>(() => roles.AddClaimAsync(NewRole("r"), null!));
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
			await Shouldly.Should.ThrowAsync<ArgumentException>(() => users.AddToRoleAsync(user, " "));
			await Shouldly.Should.ThrowAsync<ArgumentException>(() => users.RemoveFromRoleAsync(user, " "));
			await Shouldly.Should.ThrowAsync<ArgumentException>(() => users.IsInRoleAsync(user, " "));

			// role does not exist
			await users.CreateAsync(user);
			await Shouldly.Should.ThrowAsync<InvalidOperationException>(() => users.AddToRoleAsync(user, "DOES-NOT-EXIST"));
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = RefreshGate,  ErrorType = typeof(LinqToDBException),     ErrorMessage = RefreshError)]
		public async Task ConcurrencyFailureOnUpdate([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var fresh     = NewUser("conc-fresh");
			var user      = NewUser("conc-user");
			var freshRole = NewRole("conc-fresh-role");
			var role      = NewRole("conc-role");
			await users.CreateAsync(fresh);
			await users.CreateAsync(user);
			await roles.CreateAsync(freshRole);
			await roles.CreateAsync(role);

			(await users.UpdateAsync(fresh))    .Succeeded.ShouldBeTrue();
			(await roles.UpdateAsync(freshRole)).Succeeded.ShouldBeTrue();

			// Corrupt the stored ConcurrencyStamp out-of-band so the optimistic filter no longer matches. Through
			// ctx, not the setup connection: Access does not show one connection the rows another just inserted,
			// so the same statement there matches 0 rows and the test's premise silently evaporates.
			await ctx.GetTable<IdentityUser>()
				.Where(u => u.Id == user.Id)
				.Set(u => u.ConcurrencyStamp, "stale")
				.UpdateAsync();
			await ctx.GetTable<IdentityRole>()
				.Where(r => r.Id == role.Id)
				.Set(r => r.ConcurrencyStamp, "stale")
				.UpdateAsync();

			(await users.UpdateAsync(user)).Succeeded.ShouldBeFalse();
			(await roles.UpdateAsync(role)).Succeeded.ShouldBeFalse();
		}

		[Test]
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
			await ctx.GetTable<IdentityUser>()
				.Where(u => u.Id == user.Id)
				.Set(u => u.ConcurrencyStamp, "stale")
				.UpdateAsync();
			await ctx.GetTable<IdentityRole>()
				.Where(r => r.Id == role.Id)
				.Set(r => r.ConcurrencyStamp, "stale")
				.UpdateAsync();

			(await users.DeleteAsync(user)).Succeeded.ShouldBeFalse();
			(await roles.DeleteAsync(role)).Succeeded.ShouldBeFalse();
		}

		[Test]
		public async Task DisposedStore([DataSources] string context)
		{
			// no schema needed: the disposed-guard throws before any database access.
			using var setup = GetSetup(context);
			using var ctx   = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			store.Dispose();

			await Shouldly.Should.ThrowAsync<ObjectDisposedException>(() => store.CreateAsync(NewUser("zed")));
			await Shouldly.Should.ThrowAsync<ObjectDisposedException>(() => store.FindByIdAsync("any"));
		}

		[Test]
		public async Task NotFoundReturnsNull([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var userById    = await users.FindByIdAsync(Guid.NewGuid().ToString());
			var userByName  = await users.FindByNameAsync("NOSUCH");
			var userByEmail = await users.FindByEmailAsync("NOSUCH@TEST.COM");
			var userByLogin = await users.FindByLoginAsync("p", "k");
			var roleById    = await roles.FindByIdAsync(Guid.NewGuid().ToString());
			var roleByName  = await roles.FindByNameAsync("NOSUCH");

			userById   .ShouldBeNull();
			userByName .ShouldBeNull();
			userByEmail.ShouldBeNull();
			userByLogin.ShouldBeNull();
			roleById   .ShouldBeNull();
			roleByName .ShouldBeNull();
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
			await Shouldly.Should.NotThrowAsync(() => store.RemoveLoginAsync(user, "missing", "missing"));
			await Shouldly.Should.NotThrowAsync(() => store.RemoveTokenAsync(user, "missing", "missing", default));
			await Shouldly.Should.NotThrowAsync(() => store.RemoveClaimsAsync(user, new[] { new Claim("none", "none") }));
		}
	}
}
