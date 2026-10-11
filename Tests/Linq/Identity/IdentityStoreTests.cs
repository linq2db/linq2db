using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Async;
using LinqToDB.Identity;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

using NUnit.Framework;

using Shouldly;

namespace Tests.Identity
{
	[TestFixture]
	public class IdentityStoreTests : IdentityTestData
	{
		// Regression for LinqToDB.Identity#15: default mappings must produce the canonical AspNet* table names,
		// not the generic CLR type name (which used to leak as e.g. "IdentityUserClaim`1").
		[Test]
		public void DefaultMappingsUseAspNetTableNames([DataSources(false)] string context)
		{
			using var setup = GetSetup(context);
			var ms = setup.MappingSchema;

			ms.GetEntityDescriptor(typeof(IdentityUser)).Name.Name             .ShouldBe("AspNetUsers");
			ms.GetEntityDescriptor(typeof(IdentityRole)).Name.Name             .ShouldBe("AspNetRoles");
			ms.GetEntityDescriptor(typeof(IdentityUserClaim<string>)).Name.Name.ShouldBe("AspNetUserClaims");
			ms.GetEntityDescriptor(typeof(IdentityUserRole <string>)).Name.Name.ShouldBe("AspNetUserRoles");
			ms.GetEntityDescriptor(typeof(IdentityUserLogin<string>)).Name.Name.ShouldBe("AspNetUserLogins");
			ms.GetEntityDescriptor(typeof(IdentityUserToken<string>)).Name.Name.ShouldBe("AspNetUserTokens");
			ms.GetEntityDescriptor(typeof(IdentityRoleClaim<string>)).Name.Name.ShouldBe("AspNetRoleClaims");
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task UserCrud([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("alice");

			(await store.CreateAsync(user)).Succeeded.ShouldBeTrue();

			var byId = (await store.FindByIdAsync(user.Id)).ShouldNotBeNull();
			byId.UserName.ShouldBe("alice");

			var byName = (await store.FindByNameAsync("ALICE")).ShouldNotBeNull();
			byName.Id.ShouldBe(user.Id);

			await store.SetEmailAsync(byName, "new@test.com");
			await store.SetNormalizedEmailAsync(byName, "NEW@TEST.COM");
			(await store.UpdateAsync(byName)).Succeeded.ShouldBeTrue();

			var byEmail = (await store.FindByEmailAsync("NEW@TEST.COM")).ShouldNotBeNull();
			byEmail.Id.ShouldBe(user.Id);

			(await store.DeleteAsync(byEmail)).Succeeded.ShouldBeTrue();
			(await store.FindByIdAsync(user.Id)).ShouldBeNull();
		}

		// CreateTable emits no foreign keys, so nothing cascades: the store itself has to remove the dependents
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task UserDeleteRemovesDependents([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var user = NewUser("gone");
			var role = NewRole("gone-role");
			await users.CreateAsync(user);
			await roles.CreateAsync(role);
			await users.AddToRoleAsync(user, "GONE-ROLE");
			await users.AddClaimsAsync(user, [new Claim("scope", "read")]);
			await users.AddLoginAsync(user, new UserLoginInfo("google", "key-gone", "Google"));
			await users.SetTokenAsync(user, "google", "refresh", "tok", default);

			(await users.DeleteAsync(user)).Succeeded.ShouldBeTrue();

			ctx.GetTable<IdentityUserClaim<string>>().Count(r => r.UserId == user.Id).ShouldBe(0);
			ctx.GetTable<IdentityUserLogin<string>>().Count(r => r.UserId == user.Id).ShouldBe(0);
			ctx.GetTable<IdentityUserToken<string>>().Count(r => r.UserId == user.Id).ShouldBe(0);
			ctx.GetTable<IdentityUserRole <string>>().Count(r => r.UserId == user.Id).ShouldBe(0);

			// the same external account can sign up again
			var again = NewUser("again");
			await users.CreateAsync(again);
			await users.AddLoginAsync(again, new UserLoginInfo("google", "key-gone", "Google"));
			(await users.FindByLoginAsync("google", "key-gone")).ShouldNotBeNull().Id.ShouldBe(again.Id);
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task RoleDeleteRemovesDependents([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var user = NewUser("member");
			var role = NewRole("doomed");
			await users.CreateAsync(user);
			await roles.CreateAsync(role);
			await users.AddToRoleAsync(user, "DOOMED");
			await roles.AddClaimAsync(role, new Claim("perm", "all"));

			(await roles.DeleteAsync(role)).Succeeded.ShouldBeTrue();

			ctx.GetTable<IdentityUserRole <string>>().Count(r => r.RoleId == role.Id).ShouldBe(0);
			ctx.GetTable<IdentityRoleClaim<string>>().Count(r => r.RoleId == role.Id).ShouldBe(0);
		}

		// a delete that loses the optimistic check must not have removed the dependents either
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task UserDeleteConflictKeepsDependents([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);

			var user = NewUser("kept");
			await users.CreateAsync(user);
			await users.AddClaimsAsync(user, [new Claim("scope", "read")]);

			var stale = (await users.FindByIdAsync(user.Id)).ShouldNotBeNull();
			(await users.UpdateAsync(user)).Succeeded.ShouldBeTrue();

			(await users.DeleteAsync(stale)).Succeeded.ShouldBeFalse();

			(await users.FindByIdAsync(user.Id)).ShouldNotBeNull();
			(await users.GetClaimsAsync(user)).Select(c => c.Value).ShouldBe(["read"]);
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		public async Task UserProfileFields([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("dave");
			await store.CreateAsync(user);

			await store.SetPasswordHashAsync(user, "HASH");
			await store.SetPhoneNumberAsync(user, "+15551234");
			await store.SetPhoneNumberConfirmedAsync(user, true);
			await store.SetEmailConfirmedAsync(user, true);
			await store.SetTwoFactorEnabledAsync(user, true);
			await store.SetSecurityStampAsync(user, "STAMP-1");
			(await store.UpdateAsync(user)).Succeeded.ShouldBeTrue();

			var u = (await store.FindByIdAsync(user.Id)).ShouldNotBeNull();
			(await store.GetPasswordHashAsync(u))        .ShouldBe("HASH");
			(await store.HasPasswordAsync(u))            .ShouldBeTrue();
			(await store.GetPhoneNumberAsync(u))         .ShouldBe("+15551234");
			(await store.GetPhoneNumberConfirmedAsync(u)).ShouldBeTrue();
			(await store.GetEmailConfirmedAsync(u))      .ShouldBeTrue();
			(await store.GetTwoFactorEnabledAsync(u))    .ShouldBeTrue();
			(await store.GetSecurityStampAsync(u))       .ShouldBe("STAMP-1");

			// the original instance is still usable for a further update (concurrency stamp refreshed)
			await store.SetPhoneNumberAsync(user, "+15559999");
			(await store.UpdateAsync(user)).Succeeded.ShouldBeTrue();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		public async Task UserLockout([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("erin");
			await store.CreateAsync(user);

			var end = DateTimeOffset.UtcNow.AddMinutes(30);
			await store.SetLockoutEnabledAsync(user, true);
			await store.SetLockoutEndDateAsync(user, end);
			await store.IncrementAccessFailedCountAsync(user);
			await store.IncrementAccessFailedCountAsync(user);
			await store.UpdateAsync(user);

			var u = (await store.FindByIdAsync(user.Id)).ShouldNotBeNull();
			(await store.GetLockoutEnabledAsync(u))   .ShouldBeTrue();
			(await store.GetAccessFailedCountAsync(u)).ShouldBe(2);
			(await store.GetLockoutEndDateAsync(u))   .ShouldNotBeNull().ShouldBe(end, TimeSpan.FromSeconds(2));

			await store.ResetAccessFailedCountAsync(u);
			await store.UpdateAsync(u);
			(await store.GetAccessFailedCountAsync((await store.FindByIdAsync(user.Id)).ShouldNotBeNull())).ShouldBe(0);

			// a near-permanent lockout, past the end of a 1970-2038 TIMESTAMP
			var permanent = DateTimeOffset.MaxValue.AddDays(-1);
			await store.SetLockoutEndDateAsync(u, permanent);
			(await store.UpdateAsync(u)).Succeeded.ShouldBeTrue();
			var locked = await store.GetLockoutEndDateAsync((await store.FindByIdAsync(user.Id)).ShouldNotBeNull());
			locked.ShouldNotBeNull().ShouldBe(permanent, TimeSpan.FromSeconds(1));
		}

		[Test]
		public async Task AuthenticatorKeyAndRecoveryCodes([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("frank");
			await store.CreateAsync(user);

			// authenticator key + recovery codes are persisted through the token table
			await store.SetAuthenticatorKeyAsync(user, "AUTH-KEY", default);
			(await store.GetAuthenticatorKeyAsync(user, default)).ShouldBe("AUTH-KEY");

			await store.ReplaceCodesAsync(user, new[] { "c1", "c2", "c3" }, default);
			(await store.CountCodesAsync(user, default)).ShouldBe(3);

			(await store.RedeemCodeAsync(user, "c2", default)).ShouldBeTrue();
			(await store.CountCodesAsync(user, default))      .ShouldBe(2);
			(await store.RedeemCodeAsync(user, "c2", default)).ShouldBeFalse(); // already redeemed
		}

		[Test]
		public async Task UserClaims([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("grace");
			await store.CreateAsync(user);

			await store.AddClaimsAsync(user, new[] { new Claim("scope", "read"), new Claim("scope", "write") });
			(await store.GetClaimsAsync(user)).Select(c => c.Value).ShouldBe(["read", "write"], ignoreOrder: true);

			await store.ReplaceClaimAsync(user, new Claim("scope", "read"), new Claim("scope", "admin"));
			(await store.GetClaimsAsync(user)).Select(c => c.Value).ShouldBe(["admin", "write"], ignoreOrder: true);

			var usersWithWrite = await store.GetUsersForClaimAsync(new Claim("scope", "write"));
			usersWithWrite.Select(u => u.Id).ShouldContain(user.Id);

			await store.RemoveClaimsAsync(user, new[] { new Claim("scope", "admin") });
			(await store.GetClaimsAsync(user)).Select(c => c.Value).ShouldBe(["write"]);

			// a serialized-permissions claim: past the 255 default, and still matched by '=' in the store's filters
			var permissions = new Claim("permissions", string.Join(",", Enumerable.Range(0, 120).Select(i => $"perm{i:D3}")));
			await store.AddClaimsAsync(user, [permissions]);
			(await store.GetUsersForClaimAsync(permissions)).Select(u => u.Id).ShouldBe([user.Id]);
			await store.RemoveClaimsAsync(user, [permissions]);
			(await store.GetClaimsAsync(user)).Select(c => c.Value).ShouldBe(["write"]);
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task UserRoles([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var user = NewUser("bob");
			var role = NewRole("editor");
			await users.CreateAsync(user);
			await roles.CreateAsync(role);

			await users.AddToRoleAsync(user, "EDITOR");
			(await users.IsInRoleAsync(user, "EDITOR")).ShouldBeTrue();
			(await users.GetRolesAsync(user))          .ShouldContain("editor");
			(await users.GetUsersInRoleAsync("EDITOR")).Select(u => u.Id).ShouldContain(user.Id);

			// re-adding an existing role is an idempotent no-op (Upsert/SkipUpdate insert-or-ignore), not a PK violation
			await users.AddToRoleAsync(user, "EDITOR");
			(await users.GetRolesAsync(user)).Count(r => r == "editor").ShouldBe(1);

			await users.RemoveFromRoleAsync(user, "EDITOR");
			(await users.IsInRoleAsync(user, "EDITOR")).ShouldBeFalse();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task NavigationAssociations([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var users = new UserStore<IdentityUser>(ctx);
			var roles = new RoleStore<IdentityRole>(ctx);

			var user = NewUser("nav");
			var role = NewRole("nav-role");
			await users.CreateAsync(user);
			await roles.CreateAsync(role);
			await users.AddToRoleAsync(user, "NAV-ROLE");
			await users.AddClaimsAsync(user, new[] { new Claim("scope", "read") });
			await users.AddLoginAsync(user, new UserLoginInfo("google", "key-1", "Google"));

			// extension-method associations on the standard IdentityUser/IdentityRole types, expanded by linq2db inside the query
			var roleIds   = await ctx.GetTable<IdentityUser>().Where(u => u.Id == user.Id).SelectMany(u => u.Roles ()).Select(r  => r.RoleId)       .ToListAsync();
			var claims    = await ctx.GetTable<IdentityUser>().Where(u => u.Id == user.Id).SelectMany(u => u.Claims()).Select(c  => c.ClaimValue)   .ToListAsync();
			var logins    = await ctx.GetTable<IdentityUser>().Where(u => u.Id == user.Id).SelectMany(u => u.Logins()).Select(l  => l.LoginProvider).ToListAsync();
			var roleUsers = await ctx.GetTable<IdentityRole>().Where(r => r.Id == role.Id).SelectMany(r => r.Users ()).Select(ur => ur.UserId)      .ToListAsync();

			roleIds  .ShouldContain(role.Id);
			claims   .ShouldContain("read");
			logins   .ShouldContain("google");
			roleUsers.ShouldContain(user.Id);
		}

		[Test]
		public async Task LoginsAndTokens([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("carol");
			await store.CreateAsync(user);

			await store.AddLoginAsync(user, new UserLoginInfo("google", "key-123", "Google"));
			(await store.FindByLoginAsync("google", "key-123")).ShouldNotBeNull().Id.ShouldBe(user.Id);

			(await store.GetLoginsAsync(user)).Select(l => l.ProviderKey).ShouldContain("key-123");

			await store.SetTokenAsync(user, "google", "refresh", "tok-1", default);
			(await store.GetTokenAsync(user, "google", "refresh", default)).ShouldBe("tok-1");
			// update existing token value
			await store.SetTokenAsync(user, "google", "refresh", "tok-2", default);
			(await store.GetTokenAsync(user, "google", "refresh", default)).ShouldBe("tok-2");
			await store.RemoveTokenAsync(user, "google", "refresh", default);
			(await store.GetTokenAsync(user, "google", "refresh", default)).ShouldBeNull();

			// an id_token saved by SaveTokens - a JWT is typically 1-2 KB
			var jwt = new string('j', 1800);
			await store.SetTokenAsync(user, "google", "id_token", jwt, default);
			(await store.GetTokenAsync(user, "google", "id_token", default)).ShouldBe(jwt);

			await store.RemoveLoginAsync(user, "google", "key-123");
			(await store.FindByLoginAsync("google", "key-123")).ShouldBeNull();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllYdb,        Details = YdbGate)]
		public async Task RoleCrudAndClaims([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new RoleStore<IdentityRole>(ctx);
			var role  = NewRole("admin");

			(await store.CreateAsync(role)).Succeeded.ShouldBeTrue();

			var byId = (await store.FindByIdAsync(role.Id)).ShouldNotBeNull();
			byId.Name.ShouldBe("admin");

			await store.SetRoleNameAsync(byId, "administrator");
			await store.SetNormalizedRoleNameAsync(byId, "ADMINISTRATOR");
			(await store.UpdateAsync(byId)).Succeeded.ShouldBeTrue();
			(await store.FindByNameAsync("ADMINISTRATOR")).ShouldNotBeNull().Id.ShouldBe(role.Id);

			// the same instance stays usable after an optimistic update (its concurrency stamp is refreshed)
			await store.SetRoleNameAsync(byId, "admin2");
			await store.SetNormalizedRoleNameAsync(byId, "ADMIN2");
			(await store.UpdateAsync(byId)).Succeeded.ShouldBeTrue();

			await store.AddClaimAsync(byId, new Claim("perm", "all"));
			(await store.GetClaimsAsync(byId)).Select(c => c.Value).ShouldContain("all");
			await store.RemoveClaimAsync(byId, new Claim("perm", "all"));
			(await store.GetClaimsAsync(byId)).ShouldBeEmpty();

			(await store.DeleteAsync(byId)).Succeeded.ShouldBeTrue();
			(await store.FindByIdAsync(role.Id)).ShouldBeNull();
		}

#if NET10_0_OR_GREATER
		[Test]
		[ActiveIssue(Configuration = TestProvName.AllInformix, Details = InformixPasskeyGate)]
		public async Task Passkeys([DataSources] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("heidi");
			await store.CreateAsync(user);

			// sizes of a real P-256 registration with "none" attestation - the stored JSON comes to ~900 characters
			var credId = new byte[] { 1, 2, 3, 4 };
			var passkey = new UserPasskeyInfo(
				credId, Bytes(77), DateTimeOffset.UtcNow, signCount: 1,
				transports: new[] { "internal", "hybrid" }, isUserVerified: true, isBackupEligible: false,
				isBackedUp: false, attestationObject: Bytes(220), clientDataJson: Bytes(200))
			{ Name = "key1" };
#if NET11_0_OR_GREATER
			passkey.Aaguid = Bytes(16);
#endif

			static byte[] Bytes(int length)
			{
				var bytes = new byte[length];
				for (var i = 0; i < length; i++)
					bytes[i] = (byte)(i * 7 + 1);
				return bytes;
			}

			await store.AddOrUpdatePasskeyAsync(user, passkey, default);

			(await store.GetPasskeysAsync(user, default)).Count.ShouldBe(1);
			(await store.FindByPasskeyIdAsync(credId, default)).ShouldNotBeNull().Id.ShouldBe(user.Id);

			var found = (await store.FindPasskeyAsync(user, credId, default)).ShouldNotBeNull();
			found.SignCount.ShouldBe(1u);
			found.PublicKey        .ShouldBe(passkey.PublicKey);
			found.AttestationObject.ShouldBe(passkey.AttestationObject);
			found.ClientDataJson   .ShouldBe(passkey.ClientDataJson);
#if NET11_0_OR_GREATER
			found.Aaguid           .ShouldBe(passkey.Aaguid);
#endif

			// update (SignCount/Name are the mutable-after-creation fields)
			passkey.SignCount = 5;
			passkey.Name      = "key1-renamed";
			await store.AddOrUpdatePasskeyAsync(user, passkey, default);
			var updated = (await store.FindPasskeyAsync(user, credId, default)).ShouldNotBeNull();
			updated.SignCount.ShouldBe(5u);
			updated.Name     .ShouldBe("key1-renamed");
			(await store.GetPasskeysAsync(user, default)).Count.ShouldBe(1);

			await store.RemovePasskeyAsync(user, credId, default);
			(await store.FindPasskeyAsync(user, credId, default)).ShouldBeNull();
		}

		[Test]
		[ActiveIssue(Configuration = TestProvName.AllClickHouse, Details = ClickHouseGate)]
		[ActiveIssue(Configuration = TestProvName.AllInformix,   Details = InformixPasskeyGate)]
		public async Task UserDeleteRemovesPasskeys([DataSources] string context, [Values] bool schemaV3)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			// without Version3 the store must not touch AspNetUserPasskeys - the table doesn't exist in older schemas
			var options = schemaV3 ? Options.Create(new IdentityOptions { Stores = { SchemaVersion = IdentitySchemaVersions.Version3 } }) : null;
			var store   = new UserStore<IdentityUser>(ctx, options: options);
			var user    = NewUser("ivan");
			await store.CreateAsync(user);

			var passkey = new UserPasskeyInfo(
				[7, 7, 7], [9, 9], DateTimeOffset.UtcNow, signCount: 1,
				transports: ["internal"], isUserVerified: true, isBackupEligible: false,
				isBackedUp: false, attestationObject: [5], clientDataJson: [6]);

			await store.AddOrUpdatePasskeyAsync(user, passkey, default);

			(await store.DeleteAsync(user)).Succeeded.ShouldBeTrue();

			ctx.GetTable<IdentityUserPasskey<string>>().Count(r => r.UserId == user.Id).ShouldBe(schemaV3 ? 0 : 1);
		}
#endif
	}
}
