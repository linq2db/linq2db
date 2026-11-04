using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Data;
using LinqToDB.Identity;
using LinqToDB.Mapping;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using NUnit.Framework;

using Shouldly;

namespace Tests.Identity
{
	// Consumer-side customization, both shapes the package readme describes: substituting your own model types,
	// and keeping the shipped ones but renaming or retyping their columns. The negative cases pin the two limits
	// the readme now states - DataOptions.UseMappingSchema cannot override the defaults, and a column the defaults
	// pin for one provider cannot be renamed there by an unscoped mapping.
	[TestFixture]
	public class IdentityCustomizationTests : IdentityTestData
	{
		#region Scenario A - the consumer's own model

		public class AppUser : IdentityUser<string>
		{
			// A member the ASP.NET Core base type does not have.
			public string? Tenant { get; set; }
		}

		public class AppRole      : IdentityRole     <string>;
		public class AppUserClaim : IdentityUserClaim<string>;
		public class AppUserRole  : IdentityUserRole <string>;
		public class AppUserLogin : IdentityUserLogin<string>;
		public class AppRoleClaim : IdentityRoleClaim<string>;
		public class AppUserToken : IdentityUserToken<string>;

		// The same extension on the stock string-keyed user, for the single-type-argument contexts.
		public class AppStockUser : IdentityUser
		{
			public string? Tenant { get; set; }
		}

		internal sealed class AppIdentityConnection(DataOptions options)
			: IdentityDataConnection<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(options);

		internal sealed class AppUserOnlyConnection(DataOptions options)
			: IdentityDataConnection<AppUser, string, AppUserClaim, AppUserLogin, AppUserToken>(options);

#if NET10_0_OR_GREATER
		public class AppUserPasskey : IdentityUserPasskey<string>
		{
			public string? Device { get; set; }
		}

		internal sealed class AppPasskeyConnection(DataOptions options)
			: IdentityDataConnection<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken, AppUserPasskey>(options);

		internal sealed class AppUserOnlyPasskeyConnection(DataOptions options)
			: IdentityDataConnection<AppUser, string, AppUserClaim, AppUserLogin, AppUserToken, AppUserPasskey>(options);

		// The store writes only the stock passkey members, so the extra one is the consumer's to fill.
		sealed class AppPasskeyStore(IDataContext context, IOptions<IdentityOptions> options)
			: UserStore<AppUser, AppRole, IDataContext, string, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim, AppUserPasskey>(context, options: options)
		{
			protected override AppUserPasskey CreateUserPasskey(AppUser user, UserPasskeyInfo passkey)
			{
				var entity = base.CreateUserPasskey(user, passkey);
				entity.Device = passkey.Name;
				return entity;
			}
		}
#endif

		// Typed navigations the consumer declares on their own subclasses, which the stock extension methods cannot
		// return: their type arguments are not inferable from the receiver.
		public class NavUser : IdentityUser<string>
		{
			[Association(ThisKey = nameof(Id), OtherKey = nameof(NavUserClaim.UserId))]
			public IEnumerable<NavUserClaim> Claims { get; set; } = null!;

			[Association(QueryExpressionMethod = nameof(RolesQuery))]
			public IEnumerable<NavRole> Roles { get; set; } = null!;

			static Expression<Func<NavUser, IDataContext, IQueryable<NavRole>>> RolesQuery()
				=> (user, db) => db.GetTable<NavUserRole>()
					.Where(ur => ur.UserId == user.Id)
					.SelectMany(ur => db.GetTable<NavRole>().Where(r => r.Id == ur.RoleId));
		}

		public class NavRole : IdentityRole<string>
		{
			[Association(QueryExpressionMethod = nameof(UsersQuery))]
			public IEnumerable<NavUser> Users { get; set; } = null!;

			static Expression<Func<NavRole, IDataContext, IQueryable<NavUser>>> UsersQuery()
				=> (role, db) => db.GetTable<NavUserRole>()
					.Where(ur => ur.RoleId == role.Id)
					.SelectMany(ur => db.GetTable<NavUser>().Where(u => u.Id == ur.UserId));
		}

		public class NavUserClaim : IdentityUserClaim<string>
		{
			public string? Issuer { get; set; }
		}

		public class NavUserRole : IdentityUserRole<string>;

		internal sealed class NavIdentityConnection(DataOptions options)
			: IdentityDataConnection<NavUser, NavRole, string, NavUserClaim, NavUserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>(options);

		[Test]
		public async Task OwnNavigationProperties([DataSources] string context)
		{
			using var setup  = new NavIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<NavUser, NavRole, string, NavUserClaim, NavUserRole, IdentityUserLogin<string>, IdentityRoleClaim<string>, IdentityUserToken<string>>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			// association members are not columns
			setup.MappingSchema
				.GetEntityDescriptor(typeof(NavUser))
				.Columns
				.Select(c => c.MemberName)
				.ShouldNotContain(nameof(NavUser.Roles));

			var users = new UserStore<NavUser, NavRole, IDataContext, string, NavUserClaim, NavUserRole, IdentityUserLogin<string>, IdentityUserToken<string>, IdentityRoleClaim<string>>(ctx);
			var roles = new RoleStore<NavRole, IDataContext, string, NavUserRole, IdentityRoleClaim<string>>(ctx);

			var user = new NavUser { Id = Guid.NewGuid().ToString(), UserName = "navy", NormalizedUserName = "NAVY" };
			var role = new NavRole { Id = Guid.NewGuid().ToString(), Name = "deck", NormalizedName = "DECK" };
			await users.CreateAsync(user);
			await roles.CreateAsync(role);
			await users.AddToRoleAsync(user, "DECK");
			await ctx.InsertAsync(new NavUserClaim { UserId = user.Id, ClaimType = "scope", ClaimValue = "read", Issuer = "local" });

			var roleNames = await ctx.GetTable<NavUser>()
				.Where(u => u.Id == user.Id)
				.SelectMany(u => u.Roles)
				.Select(r => r.Name)
				.ToListAsync();

			var issuers = await ctx.GetTable<NavUser>()
				.Where(u => u.Id == user.Id)
				.SelectMany(u => u.Claims)
				.Select(c => c.Issuer)
				.ToListAsync();

			var userNames = await ctx.GetTable<NavRole>()
				.Where(r => r.Id == role.Id)
				.SelectMany(r => r.Users)
				.Select(u => u.UserName)
				.ToListAsync();

			roleNames.ShouldBe(["deck"]);
			issuers  .ShouldBe(["local"]);
			userNames.ShouldBe(["navy"]);
		}

		[Test]
		public void CustomModelTypesKeepDefaultMappings([DataSources(false)] string context)
		{
			using var db = new AppIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));

			var ms = db.MappingSchema;

			ms
				.GetEntityDescriptor(typeof(AppUser))
				.Name.Name
				.ShouldBe("AspNetUsers");
			ms
				.GetEntityDescriptor(typeof(AppRole))
				.Name.Name
				.ShouldBe("AspNetRoles");
			ms
				.GetEntityDescriptor(typeof(AppUserClaim))
				.Name.Name
				.ShouldBe("AspNetUserClaims");
			ms
				.GetEntityDescriptor(typeof(AppUserRole))
				.Name.Name
				.ShouldBe("AspNetUserRoles");
			ms
				.GetEntityDescriptor(typeof(AppUserLogin))
				.Name.Name
				.ShouldBe("AspNetUserLogins");
			ms
				.GetEntityDescriptor(typeof(AppRoleClaim))
				.Name.Name
				.ShouldBe("AspNetRoleClaims");
			ms
				.GetEntityDescriptor(typeof(AppUserToken))
				.Name.Name
				.ShouldBe("AspNetUserTokens");

			var user     = ms.GetEntityDescriptor(typeof(AppUser));
			var userRole = ms.GetEntityDescriptor(typeof(AppUserRole));

			// The defaults are registered against the consumer's type, so they reach its inherited members,
			// and its own members are mapped alongside them.
			user.Columns
				.Single(c => c.MemberName == nameof(AppUser.Email))
				.Length
				.ShouldBe(256);
			user.Columns
				.Select(c => c.MemberName)
				.ShouldContain(nameof(AppUser.Tenant));

			// The string-key width has to reach the generic entry points as well: unpinned, these widen to the
			// provider default, which diverges from the EF Core schema everywhere and overruns Firebird 2.5's
			// index limit on the AspNetUserRoles composite key.
			user.Columns
				.Single(c => c.MemberName == nameof(AppUser.Id))
				.Length
				.ShouldBe(36);
			userRole.Columns
				.Single(c => c.MemberName == nameof(AppUserRole.UserId))
				.Length
				.ShouldBe(36);
			userRole.Columns
				.Single(c => c.MemberName == nameof(AppUserRole.RoleId))
				.Length
				.ShouldBe(36);
		}

		[Test]
		public async Task CustomModelTypesRoundTrip([DataSources] string context)
		{
			using var setup  = new AppIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<AppUser, AppRole, IDataContext, string, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(ctx);
			var roles = new RoleStore<AppRole, IDataContext, string, AppUserRole, AppRoleClaim>(ctx);

			var user = new AppUser { Id = Guid.NewGuid().ToString(), UserName = "ada", NormalizedUserName = "ADA", Tenant = "acme" };
			var role = new AppRole { Id = Guid.NewGuid().ToString(), Name = "custom", NormalizedName = "CUSTOM" };

			(await users.CreateAsync(user))
				.Succeeded
				.ShouldBeTrue();
			(await roles.CreateAsync(role))
				.Succeeded
				.ShouldBeTrue();

			// the consumer's own member survives the round-trip
			var loaded = await users.FindByIdAsync(user.Id);
			loaded.ShouldNotBeNull();
			loaded.Tenant.ShouldBe("acme");

			// every satellite below is the consumer's type, not the stock one
			await users.AddClaimsAsync(user, [new Claim("tenant", "acme")]);
			var userClaims = await users.GetClaimsAsync(user);
			userClaims
				.Single()
				.Type
				.ShouldBe("tenant");

			await users.AddToRoleAsync(user, "CUSTOM");
			var inRole = await users.IsInRoleAsync(user, "CUSTOM");
			inRole.ShouldBeTrue();

			await users.AddLoginAsync(user, new UserLoginInfo("prov", "key", "disp"));
			var byLogin = await users.FindByLoginAsync("prov", "key");
			byLogin.ShouldNotBeNull();
			byLogin.Id.ShouldBe(user.Id);

			await users.SetTokenAsync(user, "prov", "tok", "value", default);
			var token = await users.GetTokenAsync(user, "prov", "tok", default);
			token.ShouldBe("value");

			(await users.DeleteAsync(user))
				.Succeeded
				.ShouldBeTrue();
		}

		[Test]
		public async Task CustomModelNavigations([DataSources] string context)
		{
			using var setup  = new AppIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<AppUser, AppRole, IDataContext, string, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(ctx);
			var roles = new RoleStore<AppRole, IDataContext, string, AppUserRole, AppRoleClaim>(ctx);

			var user = new AppUser { Id = Guid.NewGuid().ToString(), UserName = "nav", NormalizedUserName = "NAV" };
			var role = new AppRole { Id = Guid.NewGuid().ToString(), Name = "nav-role", NormalizedName = "NAV-ROLE" };
			await users.CreateAsync(user);
			await roles.CreateAsync(role);
			await users.AddToRoleAsync(user, "NAV-ROLE");
			await users.AddClaimsAsync(user, [new Claim("scope", "read")]);
			await users.AddLoginAsync(user, new UserLoginInfo("google", "key-nav", "Google"));
			await roles.AddClaimAsync(role, new Claim("perm", "all"));

			// the navigations return the stock types - which the custom-model context maps to the same tables
			var roleNames =
				from u in ctx.GetTable<AppUser>()
				where u.Id == user.Id
				from r in u.Roles()
				select r.Name;

			var userClaimValues =
				from u in ctx.GetTable<AppUser>()
				where u.Id == user.Id
				from c in u.Claims()
				select c.ClaimValue;

			var loginProviders =
				from u in ctx.GetTable<AppUser>()
				where u.Id == user.Id
				from l in u.Logins()
				select l.LoginProvider;

			var userNames =
				from r in ctx.GetTable<AppRole>()
				where r.Id == role.Id
				from u in r.Users()
				select u.UserName;

			var roleClaimValues =
				from r in ctx.GetTable<AppRole>()
				where r.Id == role.Id
				from c in r.Claims()
				select c.ClaimValue;

			var roleNameList       = await roleNames.ToListAsync();
			var userClaimValueList = await userClaimValues.ToListAsync();
			var loginProviderList  = await loginProviders.ToListAsync();
			var userNameList       = await userNames.ToListAsync();
			var roleClaimValueList = await roleClaimValues.ToListAsync();

			roleNameList      .ShouldBe(["nav-role"]);
			userClaimValueList.ShouldBe(["read"]);
			loginProviderList .ShouldBe(["google"]);
			userNameList      .ShouldBe(["nav"]);
			roleClaimValueList.ShouldBe(["all"]);
		}

		[Test]
		public void GenericContextsKeepDefaultMappings([DataSources(false)] string context)
		{
			var options = new DataOptions().UseConfiguration(context.StripRemote());

			using (var db = new IdentityDataContext   <AppStockUser>(options)) ShouldMapDefaults<AppStockUser, IdentityUserClaim<string>, IdentityUserLogin<string>, IdentityUserToken<string>>(db.MappingSchema, typeof(IdentityRole));
			using (var db = new IdentityDataConnection<AppStockUser>(options)) ShouldMapDefaults<AppStockUser, IdentityUserClaim<string>, IdentityUserLogin<string>, IdentityUserToken<string>>(db.MappingSchema, typeof(IdentityRole));

			using (var db = new IdentityDataContext<AppUser, AppRole, string>(options))
				ShouldMapDefaults<AppUser, IdentityUserClaim<string>, IdentityUserLogin<string>, IdentityUserToken<string>>(db.MappingSchema, typeof(AppRole));
			using (var db = new IdentityDataContext<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(options))
				ShouldMapDefaults<AppUser, AppUserClaim, AppUserLogin, AppUserToken>(db.MappingSchema, typeof(AppRole));

			using (var db = new IdentityDataContext<AppUser, string, AppUserClaim, AppUserLogin, AppUserToken>(options))
				ShouldMapDefaults<AppUser, AppUserClaim, AppUserLogin, AppUserToken>(db.MappingSchema, role: null);
			using (var db = new AppUserOnlyConnection(options))
				ShouldMapDefaults<AppUser, AppUserClaim, AppUserLogin, AppUserToken>(db.MappingSchema, role: null);
		}

		static void ShouldMapDefaults<TUser, TUserClaim, TUserLogin, TUserToken>(MappingSchema ms, Type? role)
		{
			var user = ms.GetEntityDescriptor(typeof(TUser));

			user.Name.Name.ShouldBe("AspNetUsers");
			user.Columns
				.Single(c => c.MemberName == nameof(IdentityUser.Id))
				.Length
				.ShouldBe(36);
			user.Columns
				.Select(c => c.MemberName)
				.ShouldContain(nameof(AppUser.Tenant));

			ms
				.GetEntityDescriptor(typeof(TUserClaim))
				.Name.Name
				.ShouldBe("AspNetUserClaims");
			ms
				.GetEntityDescriptor(typeof(TUserLogin))
				.Name.Name
				.ShouldBe("AspNetUserLogins");
			ms
				.GetEntityDescriptor(typeof(TUserToken))
				.Name.Name
				.ShouldBe("AspNetUserTokens");

			// what the user navigations return, whatever the context's own satellite types are
			ms
				.GetEntityDescriptor(typeof(IdentityUserClaim<string>))
				.Name.Name
				.ShouldBe("AspNetUserClaims");
			ms
				.GetEntityDescriptor(typeof(IdentityUserLogin<string>))
				.Name.Name
				.ShouldBe("AspNetUserLogins");

			if (role != null)
				ms
					.GetEntityDescriptor(role)
					.Name.Name
					.ShouldBe("AspNetRoles");
			else
				ms
					.GetEntityDescriptor(typeof(IdentityRole))
					.Name.Name
					.ShouldBe(nameof(IdentityRole));
		}

		[Test]
		public async Task UserOnlyContextRoundTrip([DataSources] string context)
		{
			using var setup  = new AppUserOnlyConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var users  = setup.CreateLocalTable<AppUser>();
			using var claims = setup.CreateLocalTable<AppUserClaim>();
			using var logins = setup.CreateLocalTable<AppUserLogin>();
			using var tokens = setup.CreateLocalTable<AppUserToken>();
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserOnlyStore<AppUser, IDataContext, string, AppUserClaim, AppUserLogin, AppUserToken>(ctx);
			var user  = new AppUser { Id = Guid.NewGuid().ToString(), UserName = "solo", NormalizedUserName = "SOLO", Tenant = "acme" };

			(await store.CreateAsync(user))
				.Succeeded
				.ShouldBeTrue();
			var solo = await store.FindByNameAsync("SOLO");
			solo
				.ShouldNotBeNull()
				.Tenant
				.ShouldBe("acme");

			await store.AddClaimsAsync(user, [new Claim("tenant", "acme")]);
			var userClaims = await store.GetClaimsAsync(user);
			userClaims
				.Single()
				.Type
				.ShouldBe("tenant");

			await store.AddLoginAsync(user, new UserLoginInfo("prov", "key", "disp"));
			var byLogin = await store.FindByLoginAsync("prov", "key");
			byLogin
				.ShouldNotBeNull()
				.Id
				.ShouldBe(user.Id);

			await store.SetTokenAsync(user, "prov", "tok", "value", default);
			var token = await store.GetTokenAsync(user, "prov", "tok", default);
			token.ShouldBe("value");

			(await store.DeleteAsync(user))
				.Succeeded
				.ShouldBeTrue();
			ctx.GetTable<AppUserClaim>()
				.Count(r => r.UserId == user.Id)
				.ShouldBe(0);
			ctx.GetTable<AppUserLogin>()
				.Count(r => r.UserId == user.Id)
				.ShouldBe(0);
			ctx.GetTable<AppUserToken>()
				.Count(r => r.UserId == user.Id)
				.ShouldBe(0);
		}

#if NET10_0_OR_GREATER
		[Test]
		public void CustomPasskeyTypeKeepsDefaultMappings([DataSources(false)] string context)
		{
			var options = new DataOptions().UseConfiguration(context.StripRemote());

			// Informix stores the id as hex text, two characters per byte
			var credentialIdLength = context.IsAnyOf(TestProvName.AllInformix) ? 2048 : 1024;

			using (var db = new AppPasskeyConnection        (options)) ShouldMapPasskey(db.MappingSchema, credentialIdLength);
			using (var db = new AppUserOnlyPasskeyConnection(options)) ShouldMapPasskey(db.MappingSchema, credentialIdLength);
			using (var db = new IdentityDataContext<AppUser, string, AppUserClaim, AppUserLogin, AppUserToken, AppUserPasskey>(options)) ShouldMapPasskey(db.MappingSchema, credentialIdLength);
			using (var db = new IdentityDataContext<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken, AppUserPasskey>(options)) ShouldMapPasskey(db.MappingSchema, credentialIdLength);

			static void ShouldMapPasskey(MappingSchema ms, int credentialIdLength)
			{
				var passkey = ms.GetEntityDescriptor(typeof(AppUserPasskey));

				passkey.Name.Name.ShouldBe("AspNetUserPasskeys");
				passkey.Columns
					.Single(c => c.MemberName == nameof(AppUserPasskey.CredentialId))
					.Length
					.ShouldBe(credentialIdLength);
				passkey.Columns
					.Single(c => c.MemberName == nameof(AppUserPasskey.UserId))
					.Length
					.ShouldBe(36);
				passkey.Columns
					.Select(c => c.MemberName)
					.ShouldContain(nameof(AppUserPasskey.Device));

				// no passkey navigation exists, so the stock type is not mapped alongside the custom one
				ms
					.GetEntityDescriptor(typeof(IdentityUserPasskey<string>))
					.Name.Name
					.ShouldNotBe("AspNetUserPasskeys");
			}
		}

		[Test]
		public async Task CustomPasskeyTypeRoundTrip([DataSources] string context)
		{
			using var setup    = new AppPasskeyConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema   = new KeyedSchema<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(setup);
			using var passkeys = setup.CreateLocalTable<AppUserPasskey>();
			using var ctx      = GetDataContext(context, setup.MappingSchema);

			var store = new AppPasskeyStore(ctx, Options.Create(new IdentityOptions { Stores = { SchemaVersion = IdentitySchemaVersions.Version3 } }));
			var user  = new AppUser { Id = Guid.NewGuid().ToString(), UserName = "pk", NormalizedUserName = "PK" };
			(await store.CreateAsync(user))
				.Succeeded
				.ShouldBeTrue();

			var laptop = NewPasskey([1, 2, 3], "laptop");
			var phone  = NewPasskey([4, 5, 6], "phone");
			await store.AddOrUpdatePasskeyAsync(user, laptop, default);
			await store.AddOrUpdatePasskeyAsync(user, phone,  default);

			ctx.GetTable<AppUserPasskey>()
				.Where(p => p.UserId == user.Id)
				.Select(p => p.Device)
				.ToList()
				.ShouldBe(["laptop", "phone"], ignoreOrder: true);

			var byPasskeyId = await store.FindByPasskeyIdAsync(laptop.CredentialId, default);
			byPasskeyId
				.ShouldNotBeNull()
				.Id
				.ShouldBe(user.Id);

			laptop.SignCount = 7;
			await store.AddOrUpdatePasskeyAsync(user, laptop, default);
			var passkey = await store.FindPasskeyAsync(user, laptop.CredentialId, default);
			passkey
				.ShouldNotBeNull()
				.SignCount
				.ShouldBe(7u);

			await store.RemovePasskeyAsync(user, phone.CredentialId, default);
			(await store.GetPasskeysAsync(user, default))
				.Count
				.ShouldBe(1);

			(await store.DeleteAsync(user))
				.Succeeded
				.ShouldBeTrue();
			ctx.GetTable<AppUserPasskey>()
				.Count(p => p.UserId == user.Id)
				.ShouldBe(0);

			static UserPasskeyInfo NewPasskey(byte[] credentialId, string name)
				=> new(credentialId, [9, 9], DateTimeOffset.UtcNow, signCount: 1, transports: ["internal"], isUserVerified: true,
					isBackupEligible: false, isBackedUp: false, attestationObject: [5], clientDataJson: [6]) { Name = name };
		}
#endif

		#endregion

		#region Scenario B - the shipped model, renamed

		sealed class RenamedIdentityConnection(DataOptions options) : IdentityDataConnection(options)
		{
			public const string UserTable   = "MyUsers";
			public const string EmailColumn = "EmailAddress";

			protected override void ConfigureMappings(MappingSchema mappingSchema)
			{
				base.ConfigureMappings(mappingSchema);

				new FluentMappingBuilder(mappingSchema)
					.Entity<IdentityUser>()
						.HasTableName(UserTable)
						.Property(e => e.Email)       .HasColumnName(EmailColumn)
						.Property(e => e.PasswordHash).HasDataType(DataType.VarChar)
					.Build();
			}
		}

		[Table(RenamedIdentityConnection.UserTable)]
		sealed class RenamedUserRow
		{
			[Column]                                        public string  Id    { get; set; } = null!;
			[Column(RenamedIdentityConnection.EmailColumn)] public string? Email { get; set; }
		}

		[Test]
		public void ConfigureMappingsOverridesDefaults([DataSources(false)] string context)
		{
			using var db = new RenamedIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));

			var ed    = db.MappingSchema.GetEntityDescriptor(typeof(IdentityUser));
			var email = ed.Columns.Single(c => c.MemberName == nameof(IdentityUser.Email));

			ed.Name.Name.ShouldBe(RenamedIdentityConnection.UserTable);
			email.ColumnName.ShouldBe(RenamedIdentityConnection.EmailColumn);
			ed.Columns
				.Single(c => c.MemberName == nameof(IdentityUser.PasswordHash))
				.DataType
				.ShouldBe(DataType.VarChar);

			// The second pass clones what the defaults registered rather than starting from a blank attribute,
			// so a rename keeps the default length.
			email.Length.ShouldBe(256);
		}

		[Test]
		public async Task RenamedSchemaRoundTrips([DataSources] string context)
		{
			using var setup  = new RenamedIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("rena");

			(await store.CreateAsync(user))
				.Succeeded
				.ShouldBeTrue();

			var found = await store.FindByEmailAsync(user.NormalizedEmail!);
			found.ShouldNotBeNull();
			found.UserName.ShouldBe("rena");

			// an independent mapping pins the physical names, which DDL and DML sharing one schema cannot
			var rawEmail = await ctx.GetTable<RenamedUserRow>()
				.Where(r => r.Id == user.Id)
				.Select(r => r.Email)
				.SingleAsync();
			rawEmail.ShouldBe(user.Email);

			(await store.DeleteAsync(user))
				.Succeeded
				.ShouldBeTrue();
		}

		#endregion

		#region What customization cannot do

		sealed class AuditRow
		{
			public int Id { get; set; }
		}

		[Test]
		public void DataOptionsMappingSchemaDoesNotOverrideDefaults([DataSources(false)] string context)
		{
			var consumer = new MappingSchema();

			new FluentMappingBuilder(consumer)
				.Entity<IdentityUser>().HasTableName("ShouldNotWin")
				.Entity<AuditRow>().HasTableName("AuditLog")
				.Build();

			using var db = new IdentityDataConnection(new DataOptions()
				.UseConfiguration(context.StripRemote())
				.UseMappingSchema(consumer));

			// The context adds its own schema in the constructor, after the options have been applied, and the
			// later addition takes priority - so a schema passed this way cannot override a default.
			db.MappingSchema
				.GetEntityDescriptor(typeof(IdentityUser))
				.Name.Name
				.ShouldBe("AspNetUsers");

			// It still maps what the defaults leave alone.
			db.MappingSchema
				.GetEntityDescriptor(typeof(AuditRow))
				.Name.Name
				.ShouldBe("AuditLog");
		}

		[Test]
		public void AddedMappingSchemaReplacesDefaultColumn([DataSources(false)] string context)
		{
			var consumer = new MappingSchema();

			new FluentMappingBuilder(consumer)
				.Entity<IdentityUser>()
					.Property(e => e.Email).HasColumnName("EmailAddress")
				.Build();

			using var db = new IdentityDataConnection(new DataOptions().UseConfiguration(context.StripRemote()));

			db.AddMappingSchema(consumer);

			var email = db.MappingSchema
				.GetEntityDescriptor(typeof(IdentityUser))
				.Columns
				.Single(c => c.MemberName == nameof(IdentityUser.Email));

			// unlike a second pass in ConfigureMappings, a schema built on its own starts from a blank attribute,
			// so the rename wins but the default length is gone
			email.ColumnName.ShouldBe("EmailAddress");
			email.Length.ShouldNotBe(256);
		}

		sealed class RenamedTokenConnection(DataOptions options) : IdentityDataConnection(options)
		{
			protected override void ConfigureMappings(MappingSchema mappingSchema)
			{
				base.ConfigureMappings(mappingSchema);

				new FluentMappingBuilder(mappingSchema)
					.Entity<IdentityUserToken<string>>()
						.Property(e => e.UserId).HasColumnName("UserRef")
					.Build();
			}
		}

		[Test]
		public void ProviderScopedColumnResistsUnscopedRename([DataSources(false)] string context)
		{
			using var db = new RenamedTokenConnection(new DataOptions().UseConfiguration(context.StripRemote()));

			var userId = db.MappingSchema
				.GetEntityDescriptor(typeof(IdentityUserToken<string>))
				.Columns.Single(c => c.MemberName == nameof(IdentityUserToken<>.UserId));

			// Firebird 2.5 cannot index the token table's key, so DefaultMappings drops it with a provider-scoped
			// ColumnAttribute - and a scoped mapping outranks an unscoped one, which is what swallows the rename.
			// Everywhere else the same override applies normally.
			userId.ColumnName.ShouldBe(context.IsAnyOf(TestProvName.AllFirebirdLess3) ? "UserId" : "UserRef");
		}

		#endregion

		#region Provider-scoped overrides

		sealed class OracleVarChar2Connection(DataOptions options) : IdentityDataConnection(options)
		{
			protected override void ConfigureMappings(MappingSchema mappingSchema)
			{
				base.ConfigureMappings(mappingSchema);

				new FluentMappingBuilder(mappingSchema)
					.Entity<IdentityUser>()
						.Property(e => e.NormalizedUserName).HasAttribute(new DataTypeAttribute(DataType.VarChar) { Configuration = ProviderName.Oracle })
						.Property(e => e.NormalizedEmail)   .HasAttribute(new DataTypeAttribute(DataType.VarChar) { Configuration = ProviderName.Oracle })
					.Build();
			}
		}

		[Test]
		public void ProviderScopedDataTypeOverridesScopedDefault([DataSources(false)] string context)
		{
			var options = new DataOptions().UseConfiguration(context.StripRemote());

			using var defaults = new IdentityDataConnection(options);
			using var db       = new OracleVarChar2Connection(options);

			var oracle = context.IsAnyOf(TestProvName.AllOracle);
			var name   = NormalizedUserNameColumn(db);

			name.DataType.ShouldBe(oracle ? DataType.VarChar : NormalizedUserNameColumn(defaults).DataType);
			name.Length.ShouldBe(256);
			name.CanBeNull.ShouldBeTrue();

			// the lookup parameter follows the column type, which is what keeps a VARCHAR2 index usable on Oracle
			LookupParameterType(db).ShouldBe(oracle ? DataType.VarChar : LookupParameterType(defaults));

			static ColumnDescriptor NormalizedUserNameColumn(IdentityDataConnection db)
				=> db.MappingSchema
					.GetEntityDescriptor(typeof(IdentityUser))
					.Columns.Single(c => c.MemberName == nameof(IdentityUser.NormalizedUserName));

			static DataType LookupParameterType(IdentityDataConnection db)
			{
				var normalizedName = "ALICE";

				return db.GetTable<IdentityUser>()
					.Where(u => u.NormalizedUserName == normalizedName)
					.ToSqlQuery()
					.Parameters
					.Single()
					.DbDataType.DataType;
			}
		}

		sealed class Firebird25RenamedTokenConnection(DataOptions options) : IdentityDataConnection(options)
		{
			protected override void ConfigureMappings(MappingSchema mappingSchema)
			{
				base.ConfigureMappings(mappingSchema);

				new FluentMappingBuilder(mappingSchema)
					.Entity<IdentityUserToken<string>>()
						.Property(e => e.UserId)
							.HasColumnName("UserRef")
							.HasAttribute(new ColumnAttribute
							{
								Configuration = ProviderName.Firebird25,
								Name          = "UserRef",
								Length        = 36,
								CanBeNull     = false,
								IsPrimaryKey  = false,
							})
					.Build();
			}
		}

		[Test]
		public void ProviderScopedColumnOverridesScopedDefault([DataSources(false)] string context)
		{
			using var db = new Firebird25RenamedTokenConnection(new DataOptions().UseConfiguration(context.StripRemote()));

			var userId = db.MappingSchema
				.GetEntityDescriptor(typeof(IdentityUserToken<string>))
				.Columns.Single(c => c.MemberName == nameof(IdentityUserToken<>.UserId));

			userId.ColumnName.ShouldBe("UserRef");
			userId.Length.ShouldBe(36);
			userId.CanBeNull.ShouldBeFalse();
			// a scoped ColumnAttribute replaces the whole column mapping, so it restates the dropped key itself
			userId.IsPrimaryKey.ShouldBe(!context.IsAnyOf(TestProvName.AllFirebirdLess3));
		}

		// Every provider-scoped default, as the readme's "Provider-specific mappings" section lists it.
		sealed record ScopedDefault(Type Entity, string Member, string Kind, string Configuration);

		const string DataTypeKind  = nameof(DataTypeAttribute);
		const string ConverterKind = nameof(ValueConverterAttribute);
		const string ColumnKind    = nameof(ColumnAttribute);

		static readonly string[] TextLimitProviders = [ProviderName.Access, ProviderName.Informix, ProviderName.Oracle];
		static readonly string[] TypeAndCommentProviders = [ProviderName.Informix, ProviderName.Oracle];
		static readonly string[] LockoutEndProviders =
		[
			ProviderName.MySql, ProviderName.Access, ProviderName.SqlCe, ProviderName.SqlServer2005, ProviderName.Sybase,
			ProviderName.SapHana, ProviderName.Informix, ProviderName.DB2LUW, ProviderName.Firebird, ProviderName.Ydb,
		];
		static readonly string[] LongTextProviders =
		[
			ProviderName.MySql, ProviderName.Oracle, ProviderName.Firebird, ProviderName.Access, ProviderName.DB2,
			ProviderName.Informix, ProviderName.SapHana, ProviderName.Sybase, ProviderName.SqlCe,
		];

		static IEnumerable<ScopedDefault> Rows<T>(string kind, string[] configurations, params string[] members)
			=> from m in members from c in configurations select new ScopedDefault(typeof(T), m, kind, c);

		static readonly ScopedDefault[] ScopedDefaults =
		[
			.. Rows<IdentityUser>(DataTypeKind, TextLimitProviders,
				nameof(IdentityUser.UserName), nameof(IdentityUser.NormalizedUserName), nameof(IdentityUser.Email), nameof(IdentityUser.NormalizedEmail),
				nameof(IdentityUser.PasswordHash), nameof(IdentityUser.SecurityStamp), nameof(IdentityUser.PhoneNumber)),
			.. Rows<IdentityUser>(DataTypeKind,  LockoutEndProviders, nameof(IdentityUser.LockoutEnd)),
			.. Rows<IdentityUser>(ConverterKind, LockoutEndProviders, nameof(IdentityUser.LockoutEnd)),
			.. Rows<IdentityRole>(DataTypeKind, TextLimitProviders, nameof(IdentityRole.Name), nameof(IdentityRole.NormalizedName)),
			.. Rows<IdentityUserClaim<string>>(DataTypeKind, TypeAndCommentProviders, nameof(IdentityUserClaim<>.ClaimType)),
			.. Rows<IdentityUserClaim<string>>(DataTypeKind, LongTextProviders,       nameof(IdentityUserClaim<>.ClaimValue)),
			.. Rows<IdentityRoleClaim<string>>(DataTypeKind, TypeAndCommentProviders, nameof(IdentityRoleClaim<>.ClaimType)),
			.. Rows<IdentityRoleClaim<string>>(DataTypeKind, LongTextProviders,       nameof(IdentityRoleClaim<>.ClaimValue)),
			.. Rows<IdentityUserLogin<string>>(DataTypeKind, TypeAndCommentProviders, nameof(IdentityUserLogin<>.ProviderDisplayName)),
			.. Rows<IdentityUserToken<string>>(DataTypeKind, LongTextProviders,       nameof(IdentityUserToken<>.Value)),
			.. Rows<IdentityUserToken<string>>(ColumnKind, [ProviderName.Firebird25],
				nameof(IdentityUserToken<>.UserId), nameof(IdentityUserToken<>.LoginProvider), nameof(IdentityUserToken<>.Name)),
#if NET10_0_OR_GREATER
			.. Rows<IdentityUserPasskey<string>>(ColumnKind,
				[ProviderName.MySql, ProviderName.Oracle, ProviderName.Firebird, ProviderName.Access, ProviderName.Informix, ProviderName.SqlCe],
				nameof(IdentityUserPasskey<>.Data)),
			.. Rows<IdentityUserPasskey<string>>(ColumnKind, [ProviderName.Access, ProviderName.Firebird25, ProviderName.Informix],
				nameof(IdentityUserPasskey<>.CredentialId)),
			.. Rows<IdentityUserPasskey<string>>(ConverterKind, [ProviderName.Informix], nameof(IdentityUserPasskey<>.CredentialId)),
#endif
		];

		static readonly Type[] IdentityEntities =
		[
			typeof(IdentityUser), typeof(IdentityRole), typeof(IdentityUserClaim<string>), typeof(IdentityUserRole<string>),
			typeof(IdentityUserLogin<string>), typeof(IdentityUserToken<string>), typeof(IdentityRoleClaim<string>),
#if NET10_0_OR_GREATER
			typeof(IdentityUserPasskey<string>),
#endif
		];

		static string Describe(ScopedDefault row) => $"{row.Entity.Name}.{row.Member} {row.Kind} @{row.Configuration}";

		// Pins the readme's per-provider list to DefaultMappings: a scoped default added, dropped or moved to another
		// provider fails here on that provider's leg until the list (and the readme) follow.
		[Test]
		public void ScopedDefaultsMatchDocumentedInventory([DataSources(false)] string context)
		{
			using var db = GetSetup(context);

			var ms = db.MappingSchema;

			var actual =
			(
				from type in IdentityEntities
				from column in ms.GetEntityDescriptor(type).Columns
				from attribute in ms.GetAttributes<MappingAttribute>(type, column.MemberInfo)
				where !string.IsNullOrEmpty(attribute.Configuration)
				let kind = attribute switch
				{
					DataTypeAttribute       => DataTypeKind,
					ValueConverterAttribute => ConverterKind,
					ColumnAttribute         => ColumnKind,
					_                       => attribute.GetType().Name,
				}
				select Describe(new ScopedDefault(type, column.MemberName, kind, attribute.Configuration!))
			)
			// the stock IdentityUser / IdentityRole see their defaults again through the IdentityUser<string> /
			// IdentityRole<string> mirror registration, so one documented row resolves more than once
			.Distinct(StringComparer.Ordinal)
			.OrderBy(d => d, StringComparer.Ordinal)
			.ToArray();

			var expected = ScopedDefaults
				.Where(r => ms.ConfigurationList.Contains(r.Configuration, StringComparer.Ordinal))
				.Select(Describe)
				.OrderBy(d => d, StringComparer.Ordinal)
				.ToArray();

			actual.ShouldBe(expected);
		}

		const string OverrideDbType = "OverriddenDbType";

		static readonly IValueConverter OverrideConverter = new ValueConverter<object, object>(v => v, v => v, handlesNulls: false);

		// One later scoped attribute per documented row, with the row's own configuration, in the readme's recipe form.
		sealed class EveryScopedDefaultOverriddenConnection(DataOptions options) : IdentityDataConnection(options)
		{
			protected override void ConfigureMappings(MappingSchema mappingSchema)
			{
				base.ConfigureMappings(mappingSchema);

				var builder = new FluentMappingBuilder(mappingSchema);

				foreach (var row in ScopedDefaults)
				{
					MappingAttribute attribute = row.Kind switch
					{
						DataTypeKind  => new DataTypeAttribute(DataType.VarChar, OverrideDbType) { Configuration = row.Configuration },
						ConverterKind => new ValueConverterAttribute { Configuration = row.Configuration, ValueConverter = OverrideConverter },
						_             => new ColumnAttribute { Configuration = row.Configuration, Name = "Overridden" + row.Member, IsPrimaryKey = false },
					};

					typeof(EveryScopedDefaultOverriddenConnection)
						.GetMethod(nameof(AddOverride), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
						.MakeGenericMethod(row.Entity)
						.Invoke(null, [builder, row.Member, attribute]);
				}

				builder.Build();
			}

			static void AddOverride<T>(FluentMappingBuilder builder, string member, MappingAttribute attribute)
			{
				var entity = Expression.Parameter(typeof(T), "e");
				var access = Expression.Lambda<Func<T, object?>>(Expression.Convert(Expression.PropertyOrField(entity, member), typeof(object)), entity);

				builder.Entity<T>().Property(access).HasAttribute(attribute);
			}
		}

		[Test]
		public void ScopedOverrideWinsOverEveryScopedDefault([DataSources(false)] string context)
		{
			using var db = new EveryScopedDefaultOverriddenConnection(new DataOptions().UseConfiguration(context.StripRemote()));

			var ms = db.MappingSchema;

			foreach (var row in ScopedDefaults.Where(r => ms.ConfigurationList.Contains(r.Configuration, StringComparer.Ordinal)))
			{
				var column = ms.GetEntityDescriptor(row.Entity).Columns.Single(c => c.MemberInfo.Name == row.Member);

				switch (row.Kind)
				{
					case DataTypeKind:
						column.DbType.ShouldBe(OverrideDbType, Describe(row));
						column.DataType.ShouldBe(DataType.VarChar, Describe(row));
						break;
					case ConverterKind:
						column.ValueConverter.ShouldBeSameAs(OverrideConverter, Describe(row));
						break;
					default:
						column.ColumnName.ShouldBe("Overridden" + row.Member, Describe(row));
						break;
				}
			}
		}

		#endregion
	}
}
