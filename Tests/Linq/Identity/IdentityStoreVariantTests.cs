using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace Tests.Identity
{
	[TestFixture]
	public class IdentityStoreVariantTests : IdentityTestData
	{
		// Generic AspNet* schema for an arbitrary key type, via the generic IdentityDataConnection<TUser, TRole, TKey>.
		sealed class KeyedSchema<TUser, TRole, TKey> : IDisposable
			where TKey  : IEquatable<TKey>
			where TUser : IdentityUser<TKey>
			where TRole : IdentityRole<TKey>
		{
			readonly IDataContext _db;

			public KeyedSchema(IDataContext db)
			{
				_db = db;
				Drop();

				_db.CreateTable<TUser>();
				_db.CreateTable<TRole>();
				_db.CreateTable<IdentityUserClaim<TKey>>();
				_db.CreateTable<IdentityUserRole <TKey>>();
				_db.CreateTable<IdentityUserLogin<TKey>>();
				_db.CreateTable<IdentityUserToken<TKey>>();
				_db.CreateTable<IdentityRoleClaim<TKey>>();
			}

			public void Dispose() => Drop();

			void Drop()
			{
				_db.DropTable<IdentityRoleClaim<TKey>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserToken<TKey>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserLogin<TKey>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserRole <TKey>>(throwExceptionIfNotExists: false);
				_db.DropTable<IdentityUserClaim<TKey>>(throwExceptionIfNotExists: false);
				_db.DropTable<TRole>(throwExceptionIfNotExists: false);
				_db.DropTable<TUser>(throwExceptionIfNotExists: false);
			}
		}

		[Test]
		public async Task UserOnlyStore_NoRoles([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserOnlyStore<IdentityUser>(ctx);
			var user  = NewUser("ulf");

			Assert.That((await store.CreateAsync(user)).Succeeded, Is.True);

			await store.AddClaimsAsync(user, new[] { new Claim("x", "1") });
			Assert.That((await store.GetClaimsAsync(user)).Count, Is.EqualTo(1));

			await store.AddLoginAsync(user, new UserLoginInfo("p", "k", "P"));
			Assert.That(await store.FindByLoginAsync("p", "k"), Is.Not.Null);

			Assert.That((await store.DeleteAsync(user)).Succeeded, Is.True);
		}

		[Test]
		public async Task IdentityDataContext_StoreOps([DataSources(false)] string context)
		{
			// DDL via a connection; store operations over the DataContext-based identity context (covers its
			// AddMappingSchema ctor path). DataContext is a direct context, so this case is direct-only.
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var dc     = new IdentityDataContext(new DataOptions().UseConfiguration(context.StripRemote()));

			var store = new UserStore<IdentityUser>(dc);
			var user  = NewUser("dctx");

			Assert.That((await store.CreateAsync(user)).Succeeded, Is.True);
			Assert.That(await store.FindByIdAsync(user.Id), Is.Not.Null);
			Assert.That((await store.DeleteAsync(user)).Succeeded, Is.True);
		}

		// Regression for LinqToDB.Identity#18: non-string key types (Guid here, int below) are supported.
		[Test]
		public async Task GuidKeys([DataSources] string context)
		{
			using var setup  = new IdentityDataConnection<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser<Guid>, IdentityRole<Guid>, IDataContext, Guid>(ctx);
			var roles = new RoleStore<IdentityRole<Guid>, IDataContext, Guid>(ctx);

			var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "gail", NormalizedUserName = "GAIL" };
			var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "gadmin", NormalizedName = "GADMIN" };
			Assert.That((await users.CreateAsync(user)).Succeeded, Is.True);
			Assert.That((await roles.CreateAsync(role)).Succeeded, Is.True);

			Assert.That((await users.FindByIdAsync(user.Id.ToString()))?.UserName, Is.EqualTo("gail"));

			await users.AddToRoleAsync(user, "GADMIN");
			Assert.That(await users.IsInRoleAsync(user, "GADMIN"), Is.True);

			Assert.That((await users.DeleteAsync(user)).Succeeded, Is.True);
		}

		[Test]
		public async Task IntKeys_IdentityGenerated([DataSources] string context)
		{
			using var setup  = new IdentityDataConnection<IdentityUser<int>, IdentityRole<int>, int>(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<IdentityUser<int>, IdentityRole<int>, int>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser<int>, IdentityRole<int>, IDataContext, int>(ctx);
			var roles = new RoleStore<IdentityRole<int>, IDataContext, int>(ctx);

			var user = new IdentityUser<int> { UserName = "ivan", NormalizedUserName = "IVAN" };
			Assert.That((await users.CreateAsync(user)).Succeeded, Is.True);
			Assert.That(user.Id, Is.GreaterThan(0)); // DB-generated identity key read back

			Assert.That((await users.FindByIdAsync(user.Id.ToString()))?.UserName, Is.EqualTo("ivan"));

			var role = new IdentityRole<int> { Name = "iadmin", NormalizedName = "IADMIN" };
			await roles.CreateAsync(role);
			Assert.That(role.Id, Is.GreaterThan(0));

			await users.AddToRoleAsync(user, "IADMIN");
			Assert.That(await users.IsInRoleAsync(user, "IADMIN"), Is.True);

			Assert.That((await users.DeleteAsync(user)).Succeeded, Is.True);
		}

		// Regression for LinqToDB.Identity#12: the stores resolve through DI (AddLinqToDBStores) and drive a UserManager.
		[Test]
		public async Task DependencyInjection_ResolvesStores_AndUserManagerRoundTrips([DataSources(false)] string context)
		{
			// DI / UserManager is provider-independent — direct-only.
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);

			var services = new ServiceCollection();
			services.AddLogging();
			services.AddScoped(_ => new IdentityDataConnection(new DataOptions().UseConfiguration(context.StripRemote())));
			services
				.AddIdentityCore<IdentityUser>()
				.AddRoles<IdentityRole>()
				.AddLinqToDBStores<IdentityDataConnection>();

			using var provider = services.BuildServiceProvider();
			using var scope    = provider.CreateScope();
			var sp = scope.ServiceProvider;

			var userStore = sp.GetRequiredService<IUserStore<IdentityUser>>();
			var roleStore = sp.GetRequiredService<IRoleStore<IdentityRole>>();
			Assert.That(userStore.GetType().FullName!, Does.StartWith("LinqToDB.Identity.UserStore"));
			Assert.That(roleStore.GetType().FullName!, Does.StartWith("LinqToDB.Identity.RoleStore"));

			var users = sp.GetRequiredService<UserManager<IdentityUser>>();
			var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

			Assert.That((await roles.CreateAsync(new IdentityRole("manager"))).Succeeded, Is.True);

			var created = await users.CreateAsync(new IdentityUser("di-user"), "P@ssw0rd1!");
			Assert.That(created.Succeeded, Is.True);

			var found = await users.FindByNameAsync("di-user");
			Assert.That(found, Is.Not.Null);

			Assert.That((await users.AddToRoleAsync(found!, "manager")).Succeeded, Is.True);
			Assert.That(await users.IsInRoleAsync(found!, "manager"), Is.True);
		}

		// Regression for LinqToDB.Identity#21: a UserManager over an int-keyed user works end to end, and the
		// framework's string-typed FindByIdAsync resolves through the store's string->int key conversion.
		[Test]
		public async Task DependencyInjection_IntKeys_UserManagerRoundTrips([DataSources(false)] string context)
		{
			using var setup  = new IdentityDataConnection<IdentityUser<int>, IdentityRole<int>, int>(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<IdentityUser<int>, IdentityRole<int>, int>(setup);

			var services = new ServiceCollection();
			services.AddLogging();
			services.AddScoped(_ => new IdentityDataConnection<IdentityUser<int>, IdentityRole<int>, int>(new DataOptions().UseConfiguration(context.StripRemote())));
			services
				.AddIdentityCore<IdentityUser<int>>()
				.AddRoles<IdentityRole<int>>()
				.AddLinqToDBStores<IdentityDataConnection<IdentityUser<int>, IdentityRole<int>, int>>();

			using var provider = services.BuildServiceProvider();
			using var scope    = provider.CreateScope();
			var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser<int>>>();

			Assert.That((await users.CreateAsync(new IdentityUser<int> { UserName = "int-di" }, "P@ssw0rd1!")).Succeeded, Is.True);

			var byName = await users.FindByNameAsync("int-di");
			Assert.That(byName,     Is.Not.Null);
			Assert.That(byName!.Id, Is.GreaterThan(0));

			// UserManager.FindByIdAsync takes a string; the store converts it to the int key (issue #21).
			var byId = await users.FindByIdAsync(byName.Id.ToString());
			Assert.That(byId?.Id, Is.EqualTo(byName.Id));
		}
	}
}
