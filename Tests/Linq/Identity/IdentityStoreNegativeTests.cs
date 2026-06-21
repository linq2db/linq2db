using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Identity;

using Microsoft.AspNetCore.Identity;

using NUnit.Framework;

namespace Tests.Identity
{
	[TestFixture]
	public class IdentityStoreNegativeTests : IdentityTestData
	{
		[Test]
		public void NullArguments([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

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
		public async Task RoleNameValidation([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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
		public async Task ConcurrencyFailure([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var u1 = NewUser("conc-u1");
			var u2 = NewUser("conc-u2");
			await users.CreateAsync(u1);
			await users.CreateAsync(u2);

			// corrupt the stored ConcurrencyStamp out-of-band so the optimistic filter no longer matches
			await setup.GetTable<IdentityUser>().Where(u => u.Id == u1.Id).Set(u => u.ConcurrencyStamp, "stale").UpdateAsync();
			await setup.GetTable<IdentityUser>().Where(u => u.Id == u2.Id).Set(u => u.ConcurrencyStamp, "stale").UpdateAsync();

			Assert.That((await users.UpdateAsync(u1)).Succeeded, Is.False);
			Assert.That((await users.DeleteAsync(u2)).Succeeded, Is.False);

			var role = NewRole("conc-role");
			await roles.CreateAsync(role);
			await setup.GetTable<IdentityRole>().Where(r => r.Id == role.Id).Set(r => r.ConcurrencyStamp, "stale").UpdateAsync();
			Assert.That((await roles.UpdateAsync(role)).Succeeded, Is.False);
			Assert.That((await roles.DeleteAsync(role)).Succeeded, Is.False);
		}

		[Test]
		public void DisposedStore([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			store.Dispose();

			Assert.ThrowsAsync<ObjectDisposedException>(() => store.CreateAsync(NewUser("zed")));
			Assert.ThrowsAsync<ObjectDisposedException>(() => store.FindByIdAsync("any"));
		}

		[Test]
		public async Task NotFoundReturnsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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
		public async Task IdempotentRemovals([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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
