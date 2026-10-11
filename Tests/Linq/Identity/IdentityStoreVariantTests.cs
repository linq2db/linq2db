using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

using Shouldly;

namespace Tests.Identity
{
	[TestFixture]
	public class IdentityStoreVariantTests : IdentityTestData
	{
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task UserOnlyStore_NoRoles([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserOnlyStore<IdentityUser>(ctx);
			var user  = NewUser("ulf");

			(await store.CreateAsync(user)).Succeeded.ShouldBeTrue();

			await store.AddClaimsAsync(user, new[] { new Claim("x", "1") });
			(await store.GetClaimsAsync(user)).Count.ShouldBe(1);

			await store.AddLoginAsync(user, new UserLoginInfo("p", "k", "P"));
			(await store.FindByLoginAsync("p", "k")).ShouldNotBeNull();

			(await store.DeleteAsync(user)).Succeeded.ShouldBeTrue();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task IdentityDataContext_StoreOps([DataSources(false)] string context)
		{
			// DDL via a connection; store operations over the DataContext-based identity context (covers its
			// AddMappingSchema ctor path). DataContext is a direct context, so this case is direct-only.
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var dc     = new IdentityDataContext(new DataOptions().UseConfiguration(context.StripRemote()));

			var store = new UserStore<IdentityUser>(dc);
			var user  = NewUser("dctx");

			(await store.CreateAsync(user)).Succeeded.ShouldBeTrue();
			(await store.FindByIdAsync(user.Id)).ShouldNotBeNull();
			(await store.DeleteAsync(user)).Succeeded.ShouldBeTrue();
		}

		// Regression for LinqToDB.Identity#18: non-string key types (Guid here, int below) are supported.
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task GuidKeys([DataSources] string context)
		{
			using var setup  = new IdentityDataConnection<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<IdentityUser<Guid>, IdentityRole<Guid>, Guid>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser<Guid>, IdentityRole<Guid>, IDataContext, Guid>(ctx);
			var roles = new RoleStore<IdentityRole<Guid>, IDataContext, Guid>(ctx);

			var user = new IdentityUser<Guid> { Id = Guid.NewGuid(), UserName = "gail", NormalizedUserName = "GAIL" };
			var role = new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = "gadmin", NormalizedName = "GADMIN" };
			(await users.CreateAsync(user)).Succeeded.ShouldBeTrue();
			(await roles.CreateAsync(role)).Succeeded.ShouldBeTrue();

			(await users.FindByIdAsync(user.Id.ToString())).ShouldNotBeNull().UserName.ShouldBe("gail");

			await users.AddToRoleAsync(user, "GADMIN");
			(await users.IsInRoleAsync(user, "GADMIN")).ShouldBeTrue();

			(await users.DeleteAsync(user)).Succeeded.ShouldBeTrue();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task IntKeys_IdentityGenerated([DataSources] string context)
		{
			using var setup  = new IdentityDataConnection<IdentityUser<int>, IdentityRole<int>, int>(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<IdentityUser<int>, IdentityRole<int>, int>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser<int>, IdentityRole<int>, IDataContext, int>(ctx);
			var roles = new RoleStore<IdentityRole<int>, IDataContext, int>(ctx);

			var user = new IdentityUser<int> { UserName = "ivan", NormalizedUserName = "IVAN" };
			(await users.CreateAsync(user)).Succeeded.ShouldBeTrue();
			user.Id.ShouldBeGreaterThan(0); // DB-generated identity key read back

			(await users.FindByIdAsync(user.Id.ToString())).ShouldNotBeNull().UserName.ShouldBe("ivan");

			var role = new IdentityRole<int> { Name = "iadmin", NormalizedName = "IADMIN" };
			await roles.CreateAsync(role);
			role.Id.ShouldBeGreaterThan(0);

			await users.AddToRoleAsync(user, "IADMIN");
			(await users.IsInRoleAsync(user, "IADMIN")).ShouldBeTrue();

			(await users.DeleteAsync(user)).Succeeded.ShouldBeTrue();
		}

		// Regression for LinqToDB.Identity#12: the stores resolve through DI (AddLinqToDBStores) and drive a UserManager.
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
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
			userStore.GetType().FullName!.ShouldStartWith("LinqToDB.Identity.UserStore");
			roleStore.GetType().FullName!.ShouldStartWith("LinqToDB.Identity.RoleStore");

			var users = sp.GetRequiredService<UserManager<IdentityUser>>();
			var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

			(await roles.CreateAsync(new IdentityRole("manager"))).Succeeded.ShouldBeTrue();

			var created = await users.CreateAsync(new IdentityUser("di-user"), "P@ssw0rd1!");
			created.Succeeded.ShouldBeTrue();

			var found = (await users.FindByNameAsync("di-user")).ShouldNotBeNull();

			(await users.AddToRoleAsync(found, "manager")).Succeeded.ShouldBeTrue();
			(await users.IsInRoleAsync(found, "manager")).ShouldBeTrue();
		}

		// Regression for LinqToDB.Identity#21: a UserManager over an int-keyed user works end to end, and the
		// framework's string-typed FindByIdAsync resolves through the store's string->int key conversion.
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
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

			(await users.CreateAsync(new IdentityUser<int> { UserName = "int-di" }, "P@ssw0rd1!")).Succeeded.ShouldBeTrue();

			var byName = (await users.FindByNameAsync("int-di")).ShouldNotBeNull();
			byName.Id.ShouldBeGreaterThan(0);

			// UserManager.FindByIdAsync takes a string; the store converts it to the int key (issue #21).
			(await users.FindByIdAsync(byName.Id.ToString())).ShouldNotBeNull().Id.ShouldBe(byName.Id);
		}

		// AddLinqToDBStores without an identity context: the stores fall back to the stock satellite types, and the
		// mappings come from wherever the consumer's context gets them.
		[Test]
		public async Task DependencyInjection_PlainContext([DataSources(false)] string context, [Values] bool withRoles)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);

			var services = new ServiceCollection();
			services.AddLogging();
			services.AddScoped(_ => new DataConnection(new DataOptions().UseConfiguration(context.StripRemote()).UseMappingSchema(setup.MappingSchema)));

			var builder = services.AddIdentityCore<IdentityUser>();
			if (withRoles)
				builder.AddRoles<IdentityRole>();
			builder.AddLinqToDBStores<DataConnection>();

			using var provider = services.BuildServiceProvider();
			using var scope    = provider.CreateScope();
			var sp = scope.ServiceProvider;

			if (withRoles)
			{
				sp.GetRequiredService<IUserStore<IdentityUser>>().ShouldBeOfType<UserStore<IdentityUser, IdentityRole, DataConnection, string>>();
				sp.GetRequiredService<IRoleStore<IdentityRole>>().ShouldBeOfType<RoleStore<IdentityRole, DataConnection, string>>();
			}
			else
			{
				sp.GetRequiredService<IUserStore<IdentityUser>>().ShouldBeOfType<UserOnlyStore<IdentityUser, DataConnection, string>>();
				sp.GetService<IRoleStore<IdentityRole>>().ShouldBeNull();
			}

			var users = sp.GetRequiredService<UserManager<IdentityUser>>();
			(await users.CreateAsync(new IdentityUser("plain-di"), "P@ssw0rd1!")).Succeeded.ShouldBeTrue();
			(await users.FindByNameAsync("plain-di")).ShouldNotBeNull();
		}

		// The documented minimal setup has no AddRoles, so it takes the user-only path - which has to find the
		// five-argument context base, and read the satellite types off it in the right order.
		[Test]
		public async Task DependencyInjection_UserOnly_IdentityContext([DataSources(false)] string context)
		{
			using var setup  = new IdentityCustomizationTests.AppUserOnlyConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var people = setup.CreateLocalTable<IdentityCustomizationTests.AppUser>();
			using var claims = setup.CreateLocalTable<IdentityCustomizationTests.AppUserClaim>();
			using var logins = setup.CreateLocalTable<IdentityCustomizationTests.AppUserLogin>();
			using var tokens = setup.CreateLocalTable<IdentityCustomizationTests.AppUserToken>();

			var services = new ServiceCollection();
			services.AddLogging();
			services.AddScoped(_ => new IdentityCustomizationTests.AppUserOnlyConnection(new DataOptions().UseConfiguration(context.StripRemote())));
			services
				.AddIdentityCore<IdentityCustomizationTests.AppUser>()
				.AddLinqToDBStores<IdentityCustomizationTests.AppUserOnlyConnection>();

			using var provider = services.BuildServiceProvider();
			using var scope    = provider.CreateScope();
			var sp = scope.ServiceProvider;

			sp.GetRequiredService<IUserStore<IdentityCustomizationTests.AppUser>>().ShouldBeOfType<UserOnlyStore<
				IdentityCustomizationTests.AppUser, IdentityCustomizationTests.AppUserOnlyConnection, string,
				IdentityCustomizationTests.AppUserClaim, IdentityCustomizationTests.AppUserLogin, IdentityCustomizationTests.AppUserToken>>();

			var users = sp.GetRequiredService<UserManager<IdentityCustomizationTests.AppUser>>();
			(await users.CreateAsync(new IdentityCustomizationTests.AppUser { Id = Guid.NewGuid().ToString(), UserName = "solo-di", Tenant = "acme" }, "P@ssw0rd1!")).Succeeded.ShouldBeTrue();
			(await users.FindByNameAsync("solo-di")).ShouldNotBeNull().Tenant.ShouldBe("acme");
		}

		// A full context registered without AddRoles takes the user-only path, and must still find the
		// context's satellite types through its base chain.
		[Test]
		public void DependencyInjection_FullContextWithoutRoles([DataSources(false)] string context)
		{
			var services = new ServiceCollection();
			services.AddLogging();
			services.AddScoped(_ => new IdentityCustomizationTests.AppIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote())));
			services
				.AddIdentityCore<IdentityCustomizationTests.AppUser>()
				.AddLinqToDBStores<IdentityCustomizationTests.AppIdentityConnection>();

			using var provider = services.BuildServiceProvider();
			using var scope    = provider.CreateScope();

			scope.ServiceProvider.GetRequiredService<IUserStore<IdentityCustomizationTests.AppUser>>().ShouldBeOfType<UserOnlyStore<
				IdentityCustomizationTests.AppUser, IdentityCustomizationTests.AppIdentityConnection, string,
				IdentityCustomizationTests.AppUserClaim, IdentityCustomizationTests.AppUserLogin, IdentityCustomizationTests.AppUserToken>>();
		}

#if NET10_0_OR_GREATER
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllInformix, Details = InformixPasskeyGate)]
		public async Task DependencyInjection_CustomPasskey([DataSources(false)] string context, [Values] bool withRoles)
		{
			using var setup    = new IdentityCustomizationTests.AppPasskeyConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema   = new KeyedSchema<
				IdentityCustomizationTests.AppUser, IdentityCustomizationTests.AppRole, string, IdentityCustomizationTests.AppUserClaim,
				IdentityCustomizationTests.AppUserRole, IdentityCustomizationTests.AppUserLogin, IdentityCustomizationTests.AppRoleClaim,
				IdentityCustomizationTests.AppUserToken>(setup);
			using var passkeys = setup.CreateLocalTable<IdentityCustomizationTests.AppUserPasskey>();

			var services = new ServiceCollection();
			services.AddLogging();

			if (withRoles)
			{
				services.AddScoped(_ => new IdentityCustomizationTests.AppPasskeyConnection(new DataOptions().UseConfiguration(context.StripRemote())));
				services
					.AddIdentityCore<IdentityCustomizationTests.AppUser>()
					.AddRoles<IdentityCustomizationTests.AppRole>()
					.AddLinqToDBStores<IdentityCustomizationTests.AppPasskeyConnection>();
			}
			else
			{
				services.AddScoped(_ => new IdentityCustomizationTests.AppUserOnlyPasskeyConnection(new DataOptions().UseConfiguration(context.StripRemote())));
				services
					.AddIdentityCore<IdentityCustomizationTests.AppUser>()
					.AddLinqToDBStores<IdentityCustomizationTests.AppUserOnlyPasskeyConnection>();
			}

			using var provider = services.BuildServiceProvider();
			using var scope    = provider.CreateScope();
			var sp = scope.ServiceProvider;

			var store = sp.GetRequiredService<IUserStore<IdentityCustomizationTests.AppUser>>();

			if (withRoles)
				store.ShouldBeOfType<UserStore<
					IdentityCustomizationTests.AppUser, IdentityCustomizationTests.AppRole, IdentityCustomizationTests.AppPasskeyConnection, string,
					IdentityCustomizationTests.AppUserClaim, IdentityCustomizationTests.AppUserRole, IdentityCustomizationTests.AppUserLogin,
					IdentityCustomizationTests.AppUserToken, IdentityCustomizationTests.AppRoleClaim, IdentityCustomizationTests.AppUserPasskey>>();
			else
				store.ShouldBeOfType<UserOnlyStore<
					IdentityCustomizationTests.AppUser, IdentityCustomizationTests.AppUserOnlyPasskeyConnection, string,
					IdentityCustomizationTests.AppUserClaim, IdentityCustomizationTests.AppUserLogin, IdentityCustomizationTests.AppUserToken,
					IdentityCustomizationTests.AppUserPasskey>>();

			var users = sp.GetRequiredService<UserManager<IdentityCustomizationTests.AppUser>>();
			var user  = new IdentityCustomizationTests.AppUser { Id = Guid.NewGuid().ToString(), UserName = "pk-di" };
			(await users.CreateAsync(user, "P@ssw0rd1!")).Succeeded.ShouldBeTrue();

			var passkeyStore = (IUserPasskeyStore<IdentityCustomizationTests.AppUser>)store;
			await passkeyStore.AddOrUpdatePasskeyAsync(user, new UserPasskeyInfo(
				[8, 8, 8], [9, 9], DateTimeOffset.UtcNow, signCount: 1,
				transports: ["internal"], isUserVerified: true, isBackupEligible: false,
				isBackedUp: false, attestationObject: [5], clientDataJson: [6]), default);

			(await passkeyStore.GetPasskeysAsync(user, default)).Count.ShouldBe(1);
		}
#endif

		[Test]
		public void DependencyInjection_RejectsNonIdentityTypes()
		{
			Shouldly.Should.Throw<InvalidOperationException>(() => new ServiceCollection()
				.AddIdentityCore<object>()
				.AddLinqToDBStores<IdentityDataConnection>())
				.Message.ShouldContain("IdentityUser<TKey>");

			Shouldly.Should.Throw<InvalidOperationException>(() => new ServiceCollection()
				.AddIdentityCore<IdentityUser>()
				.AddRoles<object>()
				.AddLinqToDBStores<IdentityDataConnection>())
				.Message.ShouldContain("IdentityRole<TKey>");
		}
	}
}
