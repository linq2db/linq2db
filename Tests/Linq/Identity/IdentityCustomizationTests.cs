using System;
using System.Linq;
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

		[Test]
		public void CustomModelTypesKeepDefaultMappings([DataSources(false)] string context)
		{
			using var db = new AppIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));

			var ms = db.MappingSchema;

			ms.GetEntityDescriptor(typeof(AppUser     )).Name.Name.ShouldBe("AspNetUsers");
			ms.GetEntityDescriptor(typeof(AppRole     )).Name.Name.ShouldBe("AspNetRoles");
			ms.GetEntityDescriptor(typeof(AppUserClaim)).Name.Name.ShouldBe("AspNetUserClaims");
			ms.GetEntityDescriptor(typeof(AppUserRole )).Name.Name.ShouldBe("AspNetUserRoles");
			ms.GetEntityDescriptor(typeof(AppUserLogin)).Name.Name.ShouldBe("AspNetUserLogins");
			ms.GetEntityDescriptor(typeof(AppRoleClaim)).Name.Name.ShouldBe("AspNetRoleClaims");
			ms.GetEntityDescriptor(typeof(AppUserToken)).Name.Name.ShouldBe("AspNetUserTokens");

			var user     = ms.GetEntityDescriptor(typeof(AppUser));
			var userRole = ms.GetEntityDescriptor(typeof(AppUserRole));

			// The defaults are registered against the consumer's type, so they reach its inherited members,
			// and its own members are mapped alongside them.
			user.Columns.Single(c => c.MemberName == nameof(AppUser.Email)).Length.ShouldBe(256);
			user.Columns.Select(c => c.MemberName).ShouldContain(nameof(AppUser.Tenant));

			// The string-key width has to reach the generic entry points as well: unpinned, these widen to the
			// provider default, which diverges from the EF Core schema everywhere and overruns Firebird 2.5's
			// index limit on the AspNetUserRoles composite key.
			user    .Columns.Single(c => c.MemberName == nameof(AppUser    .Id    )).Length.ShouldBe(36);
			userRole.Columns.Single(c => c.MemberName == nameof(AppUserRole.UserId)).Length.ShouldBe(36);
			userRole.Columns.Single(c => c.MemberName == nameof(AppUserRole.RoleId)).Length.ShouldBe(36);
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task CustomModelTypesRoundTrip([DataSources] string context)
		{
			using var setup  = new AppIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new KeyedSchema<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<AppUser, AppRole, IDataContext, string, AppUserClaim, AppUserRole, AppUserLogin, AppUserToken, AppRoleClaim>(ctx);
			var roles = new RoleStore<AppRole, IDataContext, string, AppUserRole, AppRoleClaim>(ctx);

			var user = new AppUser { Id = Guid.NewGuid().ToString(), UserName = "ada", NormalizedUserName = "ADA", Tenant = "acme" };
			var role = new AppRole { Id = Guid.NewGuid().ToString(), Name = "custom", NormalizedName = "CUSTOM" };

			(await users.CreateAsync(user)).Succeeded.ShouldBeTrue();
			(await roles.CreateAsync(role)).Succeeded.ShouldBeTrue();

			// the consumer's own member survives the round-trip
			var loaded = await users.FindByIdAsync(user.Id);
			loaded.ShouldNotBeNull();
			loaded.Tenant.ShouldBe("acme");

			// every satellite below is the consumer's type, not the stock one
			await users.AddClaimsAsync(user, [new Claim("tenant", "acme")]);
			(await users.GetClaimsAsync(user)).Single().Type.ShouldBe("tenant");

			await users.AddToRoleAsync(user, "CUSTOM");
			(await users.IsInRoleAsync(user, "CUSTOM")).ShouldBeTrue();

			await users.AddLoginAsync(user, new UserLoginInfo("prov", "key", "disp"));
			var byLogin = await users.FindByLoginAsync("prov", "key");
			byLogin.ShouldNotBeNull();
			byLogin.Id.ShouldBe(user.Id);

			await users.SetTokenAsync(user, "prov", "tok", "value", default);
			(await users.GetTokenAsync(user, "prov", "tok", default)).ShouldBe("value");

			(await users.DeleteAsync(user)).Succeeded.ShouldBeTrue();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
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

			// the navigations return the stock satellite types - which the custom-model context maps to the same tables
			(await ctx.GetTable<AppUser>().Where(u => u.Id == user.Id).SelectMany(u => u.Roles ()).Select(r => r.RoleId)       .ToListAsync()).ShouldBe([role.Id]);
			(await ctx.GetTable<AppUser>().Where(u => u.Id == user.Id).SelectMany(u => u.Claims()).Select(c => c.ClaimValue)   .ToListAsync()).ShouldBe(["read"]);
			(await ctx.GetTable<AppUser>().Where(u => u.Id == user.Id).SelectMany(u => u.Logins()).Select(l => l.LoginProvider).ToListAsync()).ShouldBe(["google"]);
			(await ctx.GetTable<AppRole>().Where(r => r.Id == role.Id).SelectMany(r => r.Users ()).Select(u => u.UserId)       .ToListAsync()).ShouldBe([user.Id]);
			(await ctx.GetTable<AppRole>().Where(r => r.Id == role.Id).SelectMany(r => r.Claims()).Select(c => c.ClaimValue)   .ToListAsync()).ShouldBe(["all"]);
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
			user.Columns.Single(c => c.MemberName == nameof(IdentityUser.Id)).Length.ShouldBe(36);
			user.Columns.Select(c => c.MemberName).ShouldContain(nameof(AppUser.Tenant));

			ms.GetEntityDescriptor(typeof(TUserClaim)).Name.Name.ShouldBe("AspNetUserClaims");
			ms.GetEntityDescriptor(typeof(TUserLogin)).Name.Name.ShouldBe("AspNetUserLogins");
			ms.GetEntityDescriptor(typeof(TUserToken)).Name.Name.ShouldBe("AspNetUserTokens");

			// what the user navigations return, whatever the context's own satellite types are
			ms.GetEntityDescriptor(typeof(IdentityUserClaim<string>)).Name.Name.ShouldBe("AspNetUserClaims");
			ms.GetEntityDescriptor(typeof(IdentityUserLogin<string>)).Name.Name.ShouldBe("AspNetUserLogins");

			if (role != null)
				ms.GetEntityDescriptor(role).Name.Name.ShouldBe("AspNetRoles");
			else
				ms.GetEntityDescriptor(typeof(IdentityRole)).Name.Name.ShouldBe(nameof(IdentityRole));
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
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

			(await store.CreateAsync(user)).Succeeded.ShouldBeTrue();
			(await store.FindByNameAsync("SOLO")).ShouldNotBeNull().Tenant.ShouldBe("acme");

			await store.AddClaimsAsync(user, [new Claim("tenant", "acme")]);
			(await store.GetClaimsAsync(user)).Single().Type.ShouldBe("tenant");

			await store.AddLoginAsync(user, new UserLoginInfo("prov", "key", "disp"));
			(await store.FindByLoginAsync("prov", "key")).ShouldNotBeNull().Id.ShouldBe(user.Id);

			await store.SetTokenAsync(user, "prov", "tok", "value", default);
			(await store.GetTokenAsync(user, "prov", "tok", default)).ShouldBe("value");

			(await store.DeleteAsync(user)).Succeeded.ShouldBeTrue();
			ctx.GetTable<AppUserClaim>().Count(r => r.UserId == user.Id).ShouldBe(0);
			ctx.GetTable<AppUserLogin>().Count(r => r.UserId == user.Id).ShouldBe(0);
			ctx.GetTable<AppUserToken>().Count(r => r.UserId == user.Id).ShouldBe(0);
		}

#if NET10_0_OR_GREATER
		[Test]
		public void CustomPasskeyTypeKeepsDefaultMappings([DataSources(false)] string context)
		{
			var options = new DataOptions().UseConfiguration(context.StripRemote());

			using (var db = new AppPasskeyConnection        (options)) ShouldMapPasskey(db.MappingSchema);
			using (var db = new AppUserOnlyPasskeyConnection(options)) ShouldMapPasskey(db.MappingSchema);
			using (var db = new IdentityDataContext<AppUser, string, AppUserClaim, AppUserLogin, AppUserToken, AppUserPasskey>(options)) ShouldMapPasskey(db.MappingSchema);
			using (var db = new IdentityDataContext<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken, AppUserPasskey>(options)) ShouldMapPasskey(db.MappingSchema);

			static void ShouldMapPasskey(MappingSchema ms)
			{
				var passkey = ms.GetEntityDescriptor(typeof(AppUserPasskey));

				passkey.Name.Name.ShouldBe("AspNetUserPasskeys");
				passkey.Columns.Single(c => c.MemberName == nameof(AppUserPasskey.CredentialId)).Length.ShouldBe(1024);
				passkey.Columns.Single(c => c.MemberName == nameof(AppUserPasskey.UserId))      .Length.ShouldBe(36);
				passkey.Columns.Select(c => c.MemberName).ShouldContain(nameof(AppUserPasskey.Device));

				// no passkey navigation exists, so the stock type is not mapped alongside the custom one
				ms.GetEntityDescriptor(typeof(IdentityUserPasskey<string>)).Name.Name.ShouldNotBe("AspNetUserPasskeys");
			}
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		[ActiveIssue(Configuration = TestProvName.AllInformix,   Details = InformixPasskeyGate)]
		public async Task CustomPasskeyTypeRoundTrip([DataSources] string context)
		{
			using var setup    = new AppPasskeyConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema   = new KeyedSchema<AppUser, AppRole, string, AppUserClaim, AppUserRole, AppUserLogin, AppRoleClaim, AppUserToken>(setup);
			using var passkeys = setup.CreateLocalTable<AppUserPasskey>();
			using var ctx      = GetDataContext(context, setup.MappingSchema);

			var store = new AppPasskeyStore(ctx, Options.Create(new IdentityOptions { Stores = { SchemaVersion = IdentitySchemaVersions.Version3 } }));
			var user  = new AppUser { Id = Guid.NewGuid().ToString(), UserName = "pk", NormalizedUserName = "PK" };
			(await store.CreateAsync(user)).Succeeded.ShouldBeTrue();

			var laptop = NewPasskey([1, 2, 3], "laptop");
			var phone  = NewPasskey([4, 5, 6], "phone");
			await store.AddOrUpdatePasskeyAsync(user, laptop, default);
			await store.AddOrUpdatePasskeyAsync(user, phone,  default);

			ctx.GetTable<AppUserPasskey>().Where(p => p.UserId == user.Id).Select(p => p.Device).ToList().ShouldBe(["laptop", "phone"], ignoreOrder: true);
			(await store.FindByPasskeyIdAsync(laptop.CredentialId, default)).ShouldNotBeNull().Id.ShouldBe(user.Id);

			laptop.SignCount = 7;
			await store.AddOrUpdatePasskeyAsync(user, laptop, default);
			(await store.FindPasskeyAsync(user, laptop.CredentialId, default)).ShouldNotBeNull().SignCount.ShouldBe(7u);

			await store.RemovePasskeyAsync(user, phone.CredentialId, default);
			(await store.GetPasskeysAsync(user, default)).Count.ShouldBe(1);

			(await store.DeleteAsync(user)).Succeeded.ShouldBeTrue();
			ctx.GetTable<AppUserPasskey>().Count(p => p.UserId == user.Id).ShouldBe(0);

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
			ed.Columns.Single(c => c.MemberName == nameof(IdentityUser.PasswordHash)).DataType.ShouldBe(DataType.VarChar);

			// The second pass clones what the defaults registered rather than starting from a blank attribute,
			// so a rename keeps the default length.
			email.Length.ShouldBe(256);
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task RenamedSchemaRoundTrips([DataSources] string context)
		{
			using var setup  = new RenamedIdentityConnection(new DataOptions().UseConfiguration(context.StripRemote()));
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("rena");

			(await store.CreateAsync(user)).Succeeded.ShouldBeTrue();

			var found = await store.FindByEmailAsync(user.NormalizedEmail!);
			found.ShouldNotBeNull();
			found.UserName.ShouldBe("rena");

			// an independent mapping pins the physical names, which DDL and DML sharing one schema cannot
			(await ctx.GetTable<RenamedUserRow>().Where(r => r.Id == user.Id).Select(r => r.Email).SingleAsync()).ShouldBe(user.Email);

			(await store.DeleteAsync(user)).Succeeded.ShouldBeTrue();
		}

		#endregion

		#region What customization cannot do

		[Test]
		public void DataOptionsMappingSchemaDoesNotOverrideDefaults([DataSources(false)] string context)
		{
			var consumer = new MappingSchema();

			new FluentMappingBuilder(consumer)
				.Entity<IdentityUser>().HasTableName("ShouldNotWin")
				.Build();

			using var db = new IdentityDataConnection(new DataOptions()
				.UseConfiguration(context.StripRemote())
				.UseMappingSchema(consumer));

			// The context adds its own schema in the constructor, after the options have been applied, and the
			// later addition takes priority - so a schema passed this way cannot override a default.
			db.MappingSchema.GetEntityDescriptor(typeof(IdentityUser)).Name.Name.ShouldBe("AspNetUsers");
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
				.Columns.Single(c => c.MemberName == nameof(IdentityUserToken<string>.UserId));

			// Firebird 2.5 cannot index the token table's key, so DefaultMappings drops it with a provider-scoped
			// ColumnAttribute - and a scoped mapping outranks an unscoped one, which is what swallows the rename.
			// Everywhere else the same override applies normally.
			userId.ColumnName.ShouldBe(context.IsAnyOf(TestProvName.AllFirebirdLess3) ? "UserId" : "UserRef");
		}

		#endregion
	}
}
