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
	public class IdentityStoreTests : IdentityTestData
	{
		// Regression for LinqToDB.Identity#15: default mappings must produce the canonical AspNet* table names,
		// not the generic CLR type name (which used to leak as e.g. "IdentityUserClaim`1").
		[Test]
		public void DefaultMappingsUseAspNetTableNames([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var setup = GetSetup(context);
			var ms = setup.MappingSchema;

			Assert.Multiple(() =>
			{
				Assert.That(ms.GetEntityDescriptor(typeof(IdentityUser)).Name.Name,              Is.EqualTo("AspNetUsers"));
				Assert.That(ms.GetEntityDescriptor(typeof(IdentityRole)).Name.Name,              Is.EqualTo("AspNetRoles"));
				Assert.That(ms.GetEntityDescriptor(typeof(IdentityUserClaim<string>)).Name.Name, Is.EqualTo("AspNetUserClaims"));
				Assert.That(ms.GetEntityDescriptor(typeof(IdentityUserRole <string>)).Name.Name, Is.EqualTo("AspNetUserRoles"));
				Assert.That(ms.GetEntityDescriptor(typeof(IdentityUserLogin<string>)).Name.Name, Is.EqualTo("AspNetUserLogins"));
				Assert.That(ms.GetEntityDescriptor(typeof(IdentityUserToken<string>)).Name.Name, Is.EqualTo("AspNetUserTokens"));
				Assert.That(ms.GetEntityDescriptor(typeof(IdentityRoleClaim<string>)).Name.Name, Is.EqualTo("AspNetRoleClaims"));
			});
		}

		[Test]
		public async Task UserCrud([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("alice");

			Assert.That((await store.CreateAsync(user)).Succeeded, Is.True);

			var byId = await store.FindByIdAsync(user.Id);
			Assert.That(byId,           Is.Not.Null);
			Assert.That(byId!.UserName, Is.EqualTo("alice"));

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
		public async Task UserProfileFields([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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
			Assert.That((await store.UpdateAsync(user)).Succeeded, Is.True);

			var u = await store.FindByIdAsync(user.Id);
			Assert.That(u, Is.Not.Null);
			Assert.That(await store.GetPasswordHashAsync(u!),          Is.EqualTo("HASH"));
			Assert.That(await store.HasPasswordAsync(u),              Is.True);
			Assert.That(await store.GetPhoneNumberAsync(u),           Is.EqualTo("+15551234"));
			Assert.That(await store.GetPhoneNumberConfirmedAsync(u),  Is.True);
			Assert.That(await store.GetEmailConfirmedAsync(u),        Is.True);
			Assert.That(await store.GetTwoFactorEnabledAsync(u),      Is.True);
			Assert.That(await store.GetSecurityStampAsync(u),         Is.EqualTo("STAMP-1"));

			// the original instance is still usable for a further update (concurrency stamp refreshed)
			await store.SetPhoneNumberAsync(user, "+15559999");
			Assert.That((await store.UpdateAsync(user)).Succeeded, Is.True);
		}

		[Test]
		public async Task UserLockout([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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

			var u = await store.FindByIdAsync(user.Id);
			Assert.That(u, Is.Not.Null);
			Assert.That(await store.GetLockoutEnabledAsync(u!),    Is.True);
			Assert.That(await store.GetAccessFailedCountAsync(u),  Is.EqualTo(2));
			Assert.That(await store.GetLockoutEndDateAsync(u),     Is.EqualTo(end).Within(TimeSpan.FromSeconds(2)));

			await store.ResetAccessFailedCountAsync(u);
			await store.UpdateAsync(u);
			Assert.That(await store.GetAccessFailedCountAsync((await store.FindByIdAsync(user.Id))!), Is.EqualTo(0));
		}

		[Test]
		public async Task AuthenticatorKeyAndRecoveryCodes([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("frank");
			await store.CreateAsync(user);

			// authenticator key + recovery codes are persisted through the token table
			await store.SetAuthenticatorKeyAsync(user, "AUTH-KEY", default);
			Assert.That(await store.GetAuthenticatorKeyAsync(user, default), Is.EqualTo("AUTH-KEY"));

			await store.ReplaceCodesAsync(user, new[] { "c1", "c2", "c3" }, default);
			Assert.That(await store.CountCodesAsync(user, default), Is.EqualTo(3));

			Assert.That(await store.RedeemCodeAsync(user, "c2", default), Is.True);
			Assert.That(await store.CountCodesAsync(user, default),       Is.EqualTo(2));
			Assert.That(await store.RedeemCodeAsync(user, "c2", default), Is.False); // already redeemed
		}

		[Test]
		public async Task UserClaims([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("grace");
			await store.CreateAsync(user);

			await store.AddClaimsAsync(user, new[] { new Claim("scope", "read"), new Claim("scope", "write") });
			Assert.That((await store.GetClaimsAsync(user)).Select(c => c.Value), Is.EquivalentTo(new[] { "read", "write" }));

			await store.ReplaceClaimAsync(user, new Claim("scope", "read"), new Claim("scope", "admin"));
			Assert.That((await store.GetClaimsAsync(user)).Select(c => c.Value), Is.EquivalentTo(new[] { "admin", "write" }));

			var usersWithWrite = await store.GetUsersForClaimAsync(new Claim("scope", "write"));
			Assert.That(usersWithWrite.Select(u => u.Id), Has.Member(user.Id));

			await store.RemoveClaimsAsync(user, new[] { new Claim("scope", "admin") });
			Assert.That((await store.GetClaimsAsync(user)).Select(c => c.Value), Is.EquivalentTo(new[] { "write" }));
		}

		[Test]
		public async Task UserRoles([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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
			Assert.That(await users.IsInRoleAsync(user, "EDITOR"), Is.True);
			Assert.That(await users.GetRolesAsync(user),          Has.Member("editor"));
			Assert.That((await users.GetUsersInRoleAsync("EDITOR")).Select(u => u.Id), Has.Member(user.Id));

			// re-adding an existing role is an idempotent no-op (Upsert/SkipUpdate insert-or-ignore), not a PK violation
			await users.AddToRoleAsync(user, "EDITOR");
			Assert.That(await users.GetRolesAsync(user), Has.Exactly(1).EqualTo("editor"));

			await users.RemoveFromRoleAsync(user, "EDITOR");
			Assert.That(await users.IsInRoleAsync(user, "EDITOR"), Is.False);
		}

		[Test]
		public async Task NavigationAssociations([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
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

			Assert.Multiple(() =>
			{
				Assert.That(roleIds,   Has.Member(role.Id));
				Assert.That(claims,    Has.Member("read"));
				Assert.That(logins,    Has.Member("google"));
				Assert.That(roleUsers, Has.Member(user.Id));
			});
		}

		[Test]
		public async Task LoginsAndTokens([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("carol");
			await store.CreateAsync(user);

			await store.AddLoginAsync(user, new UserLoginInfo("google", "key-123", "Google"));
			var byLogin = await store.FindByLoginAsync("google", "key-123");
			Assert.That(byLogin,     Is.Not.Null);
			Assert.That(byLogin!.Id, Is.EqualTo(user.Id));

			Assert.That((await store.GetLoginsAsync(user)).Select(l => l.ProviderKey), Has.Member("key-123"));

			await store.SetTokenAsync(user, "google", "refresh", "tok-1", default);
			Assert.That(await store.GetTokenAsync(user, "google", "refresh", default), Is.EqualTo("tok-1"));
			// update existing token value
			await store.SetTokenAsync(user, "google", "refresh", "tok-2", default);
			Assert.That(await store.GetTokenAsync(user, "google", "refresh", default), Is.EqualTo("tok-2"));
			await store.RemoveTokenAsync(user, "google", "refresh", default);
			Assert.That(await store.GetTokenAsync(user, "google", "refresh", default), Is.Null);

			await store.RemoveLoginAsync(user, "google", "key-123");
			Assert.That(await store.FindByLoginAsync("google", "key-123"), Is.Null);
		}

		[Test]
		public async Task RoleCrudAndClaims([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new RoleStore<IdentityRole>(ctx);
			var role  = NewRole("admin");

			Assert.That((await store.CreateAsync(role)).Succeeded, Is.True);

			var byId = await store.FindByIdAsync(role.Id);
			Assert.That(byId,       Is.Not.Null);
			Assert.That(byId!.Name, Is.EqualTo("admin"));

			await store.SetRoleNameAsync(byId, "administrator");
			await store.SetNormalizedRoleNameAsync(byId, "ADMINISTRATOR");
			Assert.That((await store.UpdateAsync(byId)).Succeeded, Is.True);
			Assert.That((await store.FindByNameAsync("ADMINISTRATOR"))?.Id, Is.EqualTo(role.Id));

			// the same instance stays usable after an optimistic update (its concurrency stamp is refreshed)
			await store.SetRoleNameAsync(byId, "admin2");
			await store.SetNormalizedRoleNameAsync(byId, "ADMIN2");
			Assert.That((await store.UpdateAsync(byId)).Succeeded, Is.True);

			await store.AddClaimAsync(byId, new Claim("perm", "all"));
			Assert.That((await store.GetClaimsAsync(byId)).Select(c => c.Value), Has.Member("all"));
			await store.RemoveClaimAsync(byId, new Claim("perm", "all"));
			Assert.That(await store.GetClaimsAsync(byId), Is.Empty);

			Assert.That((await store.DeleteAsync(byId)).Succeeded, Is.True);
			Assert.That(await store.FindByIdAsync(role.Id), Is.Null);
		}

#if NET10_0_OR_GREATER
		[Test]
		public async Task Passkeys([IncludeDataSources(true, TestProvName.AllSQLite)] string context)
		{
			using var setup  = GetSetup(context);
			using var schema = new Schema(setup);
			using var ctx    = GetDataContext(context, setup.MappingSchema);

			var store = new UserStore<IdentityUser>(ctx);
			var user  = NewUser("heidi");
			await store.CreateAsync(user);

			var credId = new byte[] { 1, 2, 3, 4 };
			var passkey = new UserPasskeyInfo(
				credId, new byte[] { 9, 9 }, DateTimeOffset.UtcNow, signCount: 1,
				transports: new[] { "internal" }, isUserVerified: true, isBackupEligible: false,
				isBackedUp: false, attestationObject: new byte[] { 5 }, clientDataJson: new byte[] { 6 })
			{ Name = "key1" };

			await store.AddOrUpdatePasskeyAsync(user, passkey, default);

			Assert.That((await store.GetPasskeysAsync(user, default)).Count, Is.EqualTo(1));
			Assert.That((await store.FindByPasskeyIdAsync(credId, default))?.Id, Is.EqualTo(user.Id));

			var found = await store.FindPasskeyAsync(user, credId, default);
			Assert.That(found,           Is.Not.Null);
			Assert.That(found!.SignCount, Is.EqualTo((uint)1));

			// update (SignCount/Name are the mutable-after-creation fields)
			passkey.SignCount = 5;
			passkey.Name      = "key1-renamed";
			await store.AddOrUpdatePasskeyAsync(user, passkey, default);
			var updated = await store.FindPasskeyAsync(user, credId, default);
			Assert.That(updated!.SignCount, Is.EqualTo((uint)5));
			Assert.That(updated.Name,       Is.EqualTo("key1-renamed"));
			Assert.That((await store.GetPasskeysAsync(user, default)).Count, Is.EqualTo(1));

			await store.RemovePasskeyAsync(user, credId, default);
			Assert.That(await store.FindPasskeyAsync(user, credId, default), Is.Null);
		}
#endif
	}
}
