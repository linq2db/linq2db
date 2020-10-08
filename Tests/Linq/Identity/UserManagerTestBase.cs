using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using LinqToDB.Identity;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Tests.Identity
{
	public abstract class UserManagerTestBase<TUser, TRole> : UserManagerTestBase<TUser, TRole, string>
		where TUser : class, IIdentityUser<string>
		where TRole : class, IIdentityRole<string>
	{
	}

	public abstract class UserManagerTestBase<TUser, TRole, TKey>
		where TUser : class, IIdentityUser<TKey>
		where TRole : class, IIdentityRole<TKey>
		where TKey : IEquatable<TKey>
	{
		static UserManagerTestBase()
		{
			MappingSchema.Default
				.GetFluentMappingBuilder()
				.Entity<TRole>()
				.HasPrimaryKey(_ => _.Id)
				.Property(_ => _.Id)
				.HasLength(255)
				.IsNullable(false)
				.Entity<TUser>()
				.HasPrimaryKey(_ => _.Id)
				.Property(_ => _.Id)
				.HasLength(255)
				.IsNullable(false);
		}

		protected void CreateTables(TestConnectionFactory factory, string connectionString)
		{
			factory.CreateTables<TUser, TRole, TKey>();
		}


		protected const string NullValue = "(null)";

		protected readonly IdentityErrorDescriber _errorDescriber = new IdentityErrorDescriber();

		protected virtual bool ShouldSkipDbTests()
		{
			return false;
		}

		protected virtual void SetupIdentityServices(IServiceCollection services, TestConnectionFactory? context = null)
		{
			services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
			services.AddIdentity<TUser, TRole>(options =>
				{
					options.Password.RequireDigit = false;
					options.Password.RequireLowercase = false;
					options.Password.RequireNonAlphanumeric = false;
					options.Password.RequireUppercase = false;
					options.User.AllowedUserNameCharacters = null;
				})
				.AddDefaultTokenProviders();
			AddUserStore(services, context);
			AddRoleStore(services, context);
			services.AddLogging();
			services.AddSingleton<ILogger<UserManager<TUser>>>(new TestLogger<UserManager<TUser>>());
			services.AddSingleton<ILogger<RoleManager<TRole>>>(new TestLogger<RoleManager<TRole>>());
		}

		protected virtual UserManager<TUser> CreateManager(TestConnectionFactory? context = null,
			IServiceCollection? services = null, Action<IServiceCollection>? configureServices = null)
		{
			if (services == null)
				services = new ServiceCollection();
			if (context == null)
				context = CreateTestContext();

			services.AddSingleton<IConnectionFactory>(context);

			SetupIdentityServices(services, context);
			if (configureServices != null)
				configureServices(services);

			return services.BuildServiceProvider().GetService<UserManager<TUser>>();
		}

		protected RoleManager<TRole> CreateRoleManager(TestConnectionFactory? context = null,
			IServiceCollection? services = null)
		{
			if (services == null)
				services = new ServiceCollection();
			if (context == null)
				context = CreateTestContext();
			SetupIdentityServices(services, context);
			return services.BuildServiceProvider().GetService<RoleManager<TRole>>();
		}

		protected abstract TestConnectionFactory CreateTestContext();

		protected abstract void AddUserStore(IServiceCollection services, TestConnectionFactory? context = null);
		protected abstract void AddRoleStore(IServiceCollection services, TestConnectionFactory? context = null);

		protected abstract void SetUserPasswordHash(TUser user, string hashedPassword);

		protected abstract TUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
			bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = null, bool useNamePrefixAsUserName = false);

		protected abstract TRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false);

		protected abstract Expression<Func<TUser, bool>> UserNameEqualsPredicate(string userName);
		protected abstract Expression<Func<TUser, bool>> UserNameStartsWithPredicate(string userName);

		protected abstract Expression<Func<TRole, bool>> RoleNameEqualsPredicate(string roleName);
		protected abstract Expression<Func<TRole, bool>> RoleNameStartsWithPredicate(string roleName);

		public List<TUser> GenerateUsers(string userNamePrefix, int count)
		{
			var users = new List<TUser>(count);
			for (var i = 0; i < count; i++)
				users.Add(CreateTestUser(userNamePrefix + i));
			return users;
		}

		public List<TRole> GenerateRoles(string namePrefix, int count)
		{
			var roles = new List<TRole>(count);
			for (var i = 0; i < count; i++)
				roles.Add(CreateTestRole(namePrefix + i));
			return roles;
		}


		protected class StaticTokenProvider : IUserTwoFactorTokenProvider<TUser>
		{
			public async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
			{
				return MakeToken(purpose, await manager.GetUserIdAsync(user));
			}

			public async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
			{
				return token == MakeToken(purpose, await manager.GetUserIdAsync(user));
			}

			public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
			{
				return Task.FromResult(true);
			}

			private static string MakeToken(string purpose, string userId)
			{
				return string.Join(":", userId, purpose, "ImmaToken");
			}
		}

		protected class AlwaysBadValidator : IUserValidator<TUser>, IRoleValidator<TRole>, IPasswordValidator<TUser>
		{
			public static readonly IdentityError ErrorMessage =
				new IdentityError {Description = "I'm Bad.", Code = "BadValidator"};

			public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user, string password)
			{
				return Task.FromResult(IdentityResult.Failed(ErrorMessage));
			}

			public Task<IdentityResult> ValidateAsync(RoleManager<TRole> manager, TRole role)
			{
				return Task.FromResult(IdentityResult.Failed(ErrorMessage));
			}

			public Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
			{
				return Task.FromResult(IdentityResult.Failed(ErrorMessage));
			}
		}

		[Test]
		public async Task CanDeleteUser()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var userId = await manager.GetUserIdAsync(user);
			IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
			Assert.Null(await manager.FindByIdAsync(userId));
		}

		[Test]
		public async Task CanUpdateUserName()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var name = Guid.NewGuid().ToString();
			var user = CreateTestUser(name);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var newName = Guid.NewGuid().ToString();
			Assert.Null(await manager.FindByNameAsync(newName));
			IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newName));
			IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
			Assert.NotNull(await manager.FindByNameAsync(newName));
			Assert.Null(await manager.FindByNameAsync(name));
		}

		[Test]
		public async Task CheckSetUserNameValidatesUser()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var username = "UpdateAsync" + Guid.NewGuid();
			var newUsername = "New" + Guid.NewGuid();
			var user = CreateTestUser(username, useNamePrefixAsUserName: true);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.Null(await manager.FindByNameAsync(newUsername));
			IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newUsername));
			Assert.NotNull(await manager.FindByNameAsync(newUsername));
			Assert.Null(await manager.FindByNameAsync(username));

			var newUser = CreateTestUser(username, useNamePrefixAsUserName: true);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(newUser));
			var error = _errorDescriber.InvalidUserName("");
			IdentityResultAssert.IsFailure(await manager.SetUserNameAsync(newUser, ""), error);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(newUser)} validation failed: {error.Code}.");

			error = _errorDescriber.DuplicateUserName(newUsername);
			IdentityResultAssert.IsFailure(await manager.SetUserNameAsync(newUser, newUsername), error);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(newUser)} validation failed: {error.Code}.");
		}

		[Test]
		public async Task SetUserNameUpdatesSecurityStamp()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var username = "UpdateAsync" + Guid.NewGuid();
			var newUsername = "New" + Guid.NewGuid();
			var user = CreateTestUser(username, useNamePrefixAsUserName: true);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.Null(await manager.FindByNameAsync(newUsername));
			IdentityResultAssert.IsSuccess(await manager.SetUserNameAsync(user, newUsername));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CreateUpdatesSecurityStamp()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var username = "Create" + Guid.NewGuid();
			var user = CreateTestUser(username, useNamePrefixAsUserName: true);
			var stamp = await manager.GetSecurityStampAsync(user);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.NotNull(await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CheckSetEmailValidatesUser()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.Options.User.RequireUniqueEmail = true;
			manager.UserValidators.Add(new UserValidator<TUser>());
			var random = new Random();
			var email = "foo" + random.Next() + "@example.com";
			var newEmail = "bar" + random.Next() + "@example.com";
			var user = CreateTestUser(email: email);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, newEmail));

			var newUser = CreateTestUser(email: email);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(newUser));
			IdentityResultAssert.IsFailure(await manager.SetEmailAsync(newUser, newEmail),
				_errorDescriber.DuplicateEmail(newEmail));
			IdentityResultAssert.IsFailure(await manager.SetEmailAsync(newUser, ""), _errorDescriber.InvalidEmail(""));
		}

		[Test]
		public async Task CanUpdatePasswordUsingHasher()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser("UpdatePassword");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
			Assert.True(await manager.CheckPasswordAsync(user, "password"));
			var userId = await manager.GetUserIdAsync(user);

			SetUserPasswordHash(user, manager.PasswordHasher.HashPassword(user, "New"));
			IdentityResultAssert.IsSuccess(await manager.UpdateAsync(user));
			Assert.False(await manager.CheckPasswordAsync(user, "password"));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"Invalid password for user {await manager.GetUserIdAsync(user)}.");
			Assert.True(await manager.CheckPasswordAsync(user, "New"));
		}

		[Test]
		public async Task CanFindById()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.NotNull(await manager.FindByIdAsync(await manager.GetUserIdAsync(user)));
		}

		[Test]
		public async Task UserValidatorCanBlockCreate()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			manager.UserValidators.Clear();
			manager.UserValidators.Add(new AlwaysBadValidator());
			IdentityResultAssert.IsFailure(await manager.CreateAsync(user), AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
		}

		[Test]
		public async Task UserValidatorCanBlockUpdate()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			manager.UserValidators.Clear();
			manager.UserValidators.Add(new AlwaysBadValidator());
			IdentityResultAssert.IsFailure(await manager.UpdateAsync(user), AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
		}

		[Test]
		public async Task CanChainUserValidators()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.UserValidators.Clear();
			var user = CreateTestUser();
			manager.UserValidators.Add(new AlwaysBadValidator());
			manager.UserValidators.Add(new AlwaysBadValidator());
			var result = await manager.CreateAsync(user);
			IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code};{AlwaysBadValidator.ErrorMessage.Code}.");
			Assert.AreEqual(2, result.Errors.Count());
		}

		[TestCase("")]
		[TestCase(null)]
		public async Task UserValidatorBlocksShortEmailsWhenRequiresUniqueEmail(string email)
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			manager.Options.User.RequireUniqueEmail = true;
			IdentityResultAssert.IsFailure(await manager.CreateAsync(user), _errorDescriber.InvalidEmail(email));
		}

#if NET451
        [TestCase("@@afd")]
        [TestCase"bogus")]
        public async Task UserValidatorBlocksInvalidEmailsWhenRequiresUniqueEmail(string email)
        {
            if (ShouldSkipDbTests())
            {
                return;
            }
            var manager = CreateManager();
            var user = CreateTestUser("UpdateBlocked", email);
            manager.Options.User.RequireUniqueEmail = true;
            IdentityResultAssert.IsFailure(await manager.CreateAsync(user), _errorDescriber.InvalidEmail(email));
        }
#endif

		[Test]
		public async Task PasswordValidatorCanBlockAddPassword()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			manager.PasswordValidators.Clear();
			manager.PasswordValidators.Add(new AlwaysBadValidator());
			IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
				AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user)} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
		}

		[Test]
		public async Task CanChainPasswordValidators()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.PasswordValidators.Clear();
			manager.PasswordValidators.Add(new AlwaysBadValidator());
			manager.PasswordValidators.Add(new AlwaysBadValidator());
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var result = await manager.AddPasswordAsync(user, "pwd");
			IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
			Assert.AreEqual(2, result.Errors.Count());
		}

		[Test]
		public async Task PasswordValidatorCanBlockChangePassword()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
			manager.PasswordValidators.Clear();
			manager.PasswordValidators.Add(new AlwaysBadValidator());
			IdentityResultAssert.IsFailure(await manager.ChangePasswordAsync(user, "password", "new"),
				AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user) ?? NullValue} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
		}

		[Test]
		public async Task PasswordValidatorCanBlockCreateUser()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			manager.PasswordValidators.Clear();
			manager.PasswordValidators.Add(new AlwaysBadValidator());
			IdentityResultAssert.IsFailure(await manager.CreateAsync(user, "password"), AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user) ?? NullValue} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
		}

		[Test]
		public async Task CanCreateUserNoPassword()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var username = "CreateUserTest" + Guid.NewGuid();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(CreateTestUser(username, useNamePrefixAsUserName: true)));
			var user = await manager.FindByNameAsync(username);
			Assert.NotNull(user);
			Assert.False(await manager.HasPasswordAsync(user));
			Assert.False(await manager.CheckPasswordAsync(user, "whatever"));
			var logins = await manager.GetLoginsAsync(user);
			Assert.NotNull(logins);
			Assert.IsEmpty(logins);
		}

		[Test]
		public async Task CanCreateUserAddLogin()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			const string provider = "ZzAuth";
			const string display = "display";
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var providerKey = await manager.GetUserIdAsync(user);
			IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, display)));
			var logins = await manager.GetLoginsAsync(user);
			Assert.NotNull(logins);
			Assert.NotNull(logins.Single());
			Assert.AreEqual(provider, logins.First().LoginProvider);
			Assert.AreEqual(providerKey, logins.First().ProviderKey);
			Assert.AreEqual(display, logins.First().ProviderDisplayName);
		}

		[Test]
		public async Task CanCreateUserLoginAndAddPassword()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var userId = await manager.GetUserIdAsync(user);
			var login = new UserLoginInfo("Provider", userId, "display");
			IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
			Assert.False(await manager.HasPasswordAsync(user));
			IdentityResultAssert.IsSuccess(await manager.AddPasswordAsync(user, "password"));
			Assert.True(await manager.HasPasswordAsync(user));
			var logins = await manager.GetLoginsAsync(user);
			Assert.NotNull(logins);
			Assert.NotNull(logins.Single());
			Assert.AreEqual(user.Id, (await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey)).Id);
			Assert.True(await manager.CheckPasswordAsync(user, "password"));
		}

		[Test]
		public async Task AddPasswordFailsIfAlreadyHave()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "Password"));
			Assert.True(await manager.HasPasswordAsync(user));
			IdentityResultAssert.IsFailure(await manager.AddPasswordAsync(user, "password"),
				"User already has a password set.");
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user)} already has a password.");
		}

		[Test]
		public async Task CanCreateUserAddRemoveLogin()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			var result = await manager.CreateAsync(user);
			Assert.NotNull(user);
			var userId = await manager.GetUserIdAsync(user);
			var login = new UserLoginInfo("Provider", userId, "display");
			IdentityResultAssert.IsSuccess(result);
			IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
			Assert.AreEqual(user.Id, (await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey)).Id);
			var logins = await manager.GetLoginsAsync(user);
			Assert.NotNull(logins);
			Assert.NotNull(logins.Single());
			Assert.AreEqual(login.LoginProvider, logins.Last().LoginProvider);
			Assert.AreEqual(login.ProviderKey, logins.Last().ProviderKey);
			Assert.AreEqual(login.ProviderDisplayName, logins.Last().ProviderDisplayName);
			var stamp = await manager.GetSecurityStampAsync(user);
			IdentityResultAssert.IsSuccess(await manager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey));
			Assert.Null(await manager.FindByLoginAsync(login.LoginProvider, login.ProviderKey));
			logins = await manager.GetLoginsAsync(user);
			Assert.NotNull(logins);
			Assert.IsEmpty(logins);
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CanRemovePassword()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser("CanRemovePassword");
			const string password = "password";
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
			var stamp = await manager.GetSecurityStampAsync(user);
			var username = await manager.GetUserNameAsync(user);
			IdentityResultAssert.IsSuccess(await manager.RemovePasswordAsync(user));
			var u = await manager.FindByNameAsync(username);
			Assert.NotNull(u);
			Assert.False(await manager.HasPasswordAsync(user));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CanChangePassword()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			const string password = "password";
			const string newPassword = "newpassword";
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, password, newPassword));
			Assert.False(await manager.CheckPasswordAsync(user, password));
			Assert.True(await manager.CheckPasswordAsync(user, newPassword));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CanAddRemoveUserClaim()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Claim[] claims = {new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3")};
			foreach (var c in claims)
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
			var userId = await manager.GetUserIdAsync(user);
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(3, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(2, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(1, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(0, userClaims.Count);
		}

		[Test]
		public async Task CanAddRemoveUserClaim2()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Claim[] claims = {new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3")};
			IdentityResultAssert.IsSuccess(await manager.AddClaimsAsync(user, claims));
			var userId = await manager.GetUserIdAsync(user);
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(3, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimsAsync(user, claims));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(0, userClaims.Count);
		}

		[Test]
		public async Task RemoveClaimOnlyAffectsUser()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			var user2 = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
			Claim[] claims = {new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3")};
			foreach (var c in claims)
			{
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user2, c));
			}
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(3, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[0]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(2, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[1]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(1, userClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(user, claims[2]));
			userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(0, userClaims.Count);
			var userClaims2 = await manager.GetClaimsAsync(user2);
			Assert.AreEqual(3, userClaims2.Count);
		}

		[Test]
		public async Task CanReplaceUserClaim()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("c", "a")));
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(1, userClaims.Count);
			var claim = new Claim("c", "b");
			var oldClaim = userClaims.FirstOrDefault();
			IdentityResultAssert.IsSuccess(await manager.ReplaceClaimAsync(user, oldClaim, claim));
			var newUserClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(1, newUserClaims.Count);
			var newClaim = newUserClaims.FirstOrDefault();
			Assert.AreEqual(claim.Type, newClaim.Type);
			Assert.AreEqual(claim.Value, newClaim.Value);
		}

		[Test]
		public async Task ReplaceUserClaimOnlyAffectsUser()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			var user2 = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
			IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("c", "a")));
			IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user2, new Claim("c", "a")));
			var userClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(1, userClaims.Count);
			var userClaims2 = await manager.GetClaimsAsync(user);
			Assert.AreEqual(1, userClaims2.Count);
			var claim = new Claim("c", "b");
			var oldClaim = userClaims.FirstOrDefault();
			IdentityResultAssert.IsSuccess(await manager.ReplaceClaimAsync(user, oldClaim, claim));
			var newUserClaims = await manager.GetClaimsAsync(user);
			Assert.AreEqual(1, newUserClaims.Count);
			var newClaim = newUserClaims.FirstOrDefault();
			Assert.AreEqual(claim.Type, newClaim.Type);
			Assert.AreEqual(claim.Value, newClaim.Value);
			userClaims2 = await manager.GetClaimsAsync(user2);
			Assert.AreEqual(1, userClaims2.Count);
			var oldClaim2 = userClaims2.FirstOrDefault();
			Assert.AreEqual("c", oldClaim2.Type);
			Assert.AreEqual("a", oldClaim2.Value);
		}

		[Test]
		public async Task ChangePasswordFallsIfPasswordWrong()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
			var result = await manager.ChangePasswordAsync(user, "bogus", "newpassword");
			IdentityResultAssert.IsFailure(result, "Incorrect password.");
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"Change password failed for user {await manager.GetUserIdAsync(user)}.");
		}

		[Test]
		public async Task AddDupeUserNameFails()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var username = "AddDupeUserNameFails" + Guid.NewGuid();
			var user = CreateTestUser(username, useNamePrefixAsUserName: true);
			var user2 = CreateTestUser(username, useNamePrefixAsUserName: true);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), _errorDescriber.DuplicateUserName(username));
		}

		[Test]
		public async Task AddDupeEmailAllowedByDefault()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser(email: "yup@yup.com");
			var user2 = CreateTestUser(email: "yup@yup.com");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user2, await manager.GetEmailAsync(user)));
		}

		[Test]
		public async Task AddDupeEmailFailsWhenUniqueEmailRequired()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.Options.User.RequireUniqueEmail = true;
			var user = CreateTestUser(email: "FooUser@yup.com");
			var user2 = CreateTestUser(email: "FooUser@yup.com");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsFailure(await manager.CreateAsync(user2), _errorDescriber.DuplicateEmail("FooUser@yup.com"));
		}

		[Test]
		public async Task UpdateSecurityStampActuallyChanges()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			Assert.Null(await manager.GetSecurityStampAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task AddDupeLoginFails()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			var login = new UserLoginInfo("Provider", "key", "display");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.AddLoginAsync(user, login));
			var result = await manager.AddLoginAsync(user, login);
			IdentityResultAssert.IsFailure(result, _errorDescriber.LoginAlreadyAssociated());
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"AddLogin for user {await manager.GetUserIdAsync(user)} failed because it was already associated with another user.");
		}

		// Email tests
		[Test]
		public async Task CanFindByEmail()
		{
			if (ShouldSkipDbTests())
				return;
			var email = "foouser@test.com";
			var manager = CreateManager();
			var user = CreateTestUser(email: email);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var fetch = await manager.FindByEmailAsync(email);
			Assert.AreEqual(user.Id, fetch.Id);
		}

		[Test]
		public async Task CanFindUsersViaUserQuerable()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			if (mgr.SupportsQueryableUsers)
			{
				var users = GenerateUsers("CanFindUsersViaUserQuerable", 4);
				foreach (var u in users)
					IdentityResultAssert.IsSuccess(await mgr.CreateAsync(u));
				Assert.AreEqual(users.Count, mgr.Users.Count(UserNameStartsWithPredicate("CanFindUsersViaUserQuerable")));
				Assert.Null(mgr.Users.FirstOrDefault(UserNameEqualsPredicate("bogus")));
			}
		}

		[Test]
		public async Task ConfirmEmailFalseByDefaultTest()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.False(await manager.IsEmailConfirmedAsync(user));
		}


		[Test]
		public async Task CanResetPasswordWithStaticTokenProvider()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.RegisterTokenProvider("Static", new StaticTokenProvider());
			manager.Options.Tokens.PasswordResetTokenProvider = "Static";
			var user = CreateTestUser();
			const string password = "password";
			const string newPassword = "newpassword";
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			var token = await manager.GeneratePasswordResetTokenAsync(user);
			Assert.NotNull(token);
			var userId = await manager.GetUserIdAsync(user);
			IdentityResultAssert.IsSuccess(await manager.ResetPasswordAsync(user, token, newPassword));
			Assert.False(await manager.CheckPasswordAsync(user, password));
			Assert.True(await manager.CheckPasswordAsync(user, newPassword));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task PasswordValidatorCanBlockResetPasswordWithStaticTokenProvider()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.RegisterTokenProvider("Static", new StaticTokenProvider());
			manager.Options.Tokens.PasswordResetTokenProvider = "Static";
			var user = CreateTestUser();
			const string password = "password";
			const string newPassword = "newpassword";
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			var token = await manager.GeneratePasswordResetTokenAsync(user);
			Assert.NotNull(token);
			manager.PasswordValidators.Add(new AlwaysBadValidator());
			IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, token, newPassword),
				AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"User {await manager.GetUserIdAsync(user)} password validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
			Assert.True(await manager.CheckPasswordAsync(user, password));
			Assert.AreEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task ResetPasswordWithStaticTokenProviderFailsWithWrongToken()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.RegisterTokenProvider("Static", new StaticTokenProvider());
			manager.Options.Tokens.PasswordResetTokenProvider = "Static";
			var user = CreateTestUser();
			const string password = "password";
			const string newPassword = "newpassword";
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, password));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			IdentityResultAssert.IsFailure(await manager.ResetPasswordAsync(user, "bogus", newPassword), "Invalid token.");
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: ResetPassword for user {await manager.GetUserIdAsync(user)}.");
			Assert.True(await manager.CheckPasswordAsync(user, password));
			Assert.AreEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CanGenerateAndVerifyUserTokenWithStaticTokenProvider()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.RegisterTokenProvider("Static", new StaticTokenProvider());
			var user = CreateTestUser();
			var user2 = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
			var userId = await manager.GetUserIdAsync(user);
			var token = await manager.GenerateUserTokenAsync(user, "Static", "test");

			Assert.True(await manager.VerifyUserTokenAsync(user, "Static", "test", token));

			Assert.False(await manager.VerifyUserTokenAsync(user, "Static", "test2", token));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: test2 for user {await manager.GetUserIdAsync(user)}.");

			Assert.False(await manager.VerifyUserTokenAsync(user, "Static", "test", token + "a"));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: test for user {await manager.GetUserIdAsync(user)}.");

			Assert.False(await manager.VerifyUserTokenAsync(user2, "Static", "test", token));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: test for user {await manager.GetUserIdAsync(user2)}.");
		}

		[Test]
		public async Task CanConfirmEmailWithStaticToken()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.RegisterTokenProvider("Static", new StaticTokenProvider());
			manager.Options.Tokens.EmailConfirmationTokenProvider = "Static";
			var user = CreateTestUser();
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var token = await manager.GenerateEmailConfirmationTokenAsync(user);
			Assert.NotNull(token);
			var userId = await manager.GetUserIdAsync(user);
			IdentityResultAssert.IsSuccess(await manager.ConfirmEmailAsync(user, token));
			Assert.True(await manager.IsEmailConfirmedAsync(user));
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
			Assert.False(await manager.IsEmailConfirmedAsync(user));
		}

		[Test]
		public async Task ConfirmEmailWithStaticTokenFailsWithWrongToken()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			manager.RegisterTokenProvider("Static", new StaticTokenProvider());
			manager.Options.Tokens.EmailConfirmationTokenProvider = "Static";
			var user = CreateTestUser();
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, "bogus"), "Invalid token.");
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: EmailConfirmation for user {await manager.GetUserIdAsync(user)}.");
		}

		[Test]
		public async Task ConfirmTokenFailsAfterPasswordChange()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser("Test");
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user, "password"));
			var token = await manager.GenerateEmailConfirmationTokenAsync(user);
			Assert.NotNull(token);
			IdentityResultAssert.IsSuccess(await manager.ChangePasswordAsync(user, "password", "newpassword"));
			IdentityResultAssert.IsFailure(await manager.ConfirmEmailAsync(user, token), "Invalid token.");
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: EmailConfirmation for user {await manager.GetUserIdAsync(user)}.");
			Assert.False(await manager.IsEmailConfirmedAsync(user));
		}

		// Lockout tests

		[Test]
		public async Task SingleFailureLockout()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
			mgr.Options.Lockout.MaxFailedAccessAttempts = 0;
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
			Assert.True(await mgr.IsLockedOutAsync(user));
			Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			IdentityResultAssert.VerifyLogMessage(mgr.Logger, $"User {await mgr.GetUserIdAsync(user)} is locked out.");

			Assert.AreEqual(0, await mgr.GetAccessFailedCountAsync(user));
		}

		[Test]
		public async Task TwoFailureLockout()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
			mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			Assert.AreEqual(1, await mgr.GetAccessFailedCountAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
			Assert.True(await mgr.IsLockedOutAsync(user));
			Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			IdentityResultAssert.VerifyLogMessage(mgr.Logger, $"User {await mgr.GetUserIdAsync(user)} is locked out.");
			Assert.AreEqual(0, await mgr.GetAccessFailedCountAsync(user));
		}

		[Test]
		public async Task ResetAccessCountPreventsLockout()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
			mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			Assert.AreEqual(1, await mgr.GetAccessFailedCountAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.ResetAccessFailedCountAsync(user));
			Assert.AreEqual(0, await mgr.GetAccessFailedCountAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			Assert.AreEqual(1, await mgr.GetAccessFailedCountAsync(user));
		}

		[Test]
		public async Task CanEnableLockoutManuallyAndLockout()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			mgr.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromHours(1);
			mgr.Options.Lockout.AllowedForNewUsers = false;
			mgr.Options.Lockout.MaxFailedAccessAttempts = 2;
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.False(await mgr.GetLockoutEnabledAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.SetLockoutEnabledAsync(user, true));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
			Assert.False(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			Assert.AreEqual(1, await mgr.GetAccessFailedCountAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.AccessFailedAsync(user));
			Assert.True(await mgr.IsLockedOutAsync(user));
			Assert.True(await mgr.GetLockoutEndDateAsync(user) > DateTimeOffset.UtcNow.AddMinutes(55));
			IdentityResultAssert.VerifyLogMessage(mgr.Logger, $"User {await mgr.GetUserIdAsync(user)} is locked out.");
			Assert.AreEqual(0, await mgr.GetAccessFailedCountAsync(user));
		}

		[Test]
		public async Task UserNotLockedOutWithNullDateTimeAndIsSetToNullDate()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()));
			Assert.False(await mgr.IsLockedOutAsync(user));
			Assert.AreEqual(new DateTimeOffset(), await mgr.GetLockoutEndDateAsync(user));
		}

		[Test]
		public async Task LockoutFailsIfNotEnabled()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			mgr.Options.Lockout.AllowedForNewUsers = false;
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.False(await mgr.GetLockoutEnabledAsync(user));
			IdentityResultAssert.IsFailure(await mgr.SetLockoutEndDateAsync(user, new DateTimeOffset()),
				"Lockout is not enabled for this user.");
			IdentityResultAssert.VerifyLogMessage(mgr.Logger,
				$"Lockout for user {await mgr.GetUserIdAsync(user)} failed because lockout is not enabled for this user.");
			Assert.False(await mgr.IsLockedOutAsync(user));
		}

		[Test]
		public async Task LockoutEndToUtcNowMinus1SecInUserShouldNotBeLockedOut()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			var user = CreateTestUser(lockoutEnd: DateTimeOffset.UtcNow.AddSeconds(-1));
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			Assert.False(await mgr.IsLockedOutAsync(user));
		}

		[Test]
		public async Task LockoutEndToUtcNowSubOneSecondWithManagerShouldNotBeLockedOut()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddSeconds(-1)));
			Assert.False(await mgr.IsLockedOutAsync(user));
		}

		[Test]
		public async Task LockoutEndToUtcNowPlus5ShouldBeLockedOut()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			var lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(5);
			var user = CreateTestUser(lockoutEnd: lockoutEnd);
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			Assert.True(await mgr.IsLockedOutAsync(user));
		}

		[Test]
		public async Task UserLockedOutWithDateTimeLocalKindNowPlus30()
		{
			if (ShouldSkipDbTests())
				return;
			var mgr = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await mgr.CreateAsync(user));
			Assert.True(await mgr.GetLockoutEnabledAsync(user));
			var lockoutEnd = new DateTimeOffset(DateTime.Now.AddMinutes(30).ToLocalTime());
			IdentityResultAssert.IsSuccess(await mgr.SetLockoutEndDateAsync(user, lockoutEnd));
			Assert.True(await mgr.IsLockedOutAsync(user));
			var end = await mgr.GetLockoutEndDateAsync(user);
			Assert.AreEqual(lockoutEnd, end);
		}

		// Role Tests
		[Test]
		public async Task CanCreateRoleTest()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var roleName = "create" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			Assert.False(await manager.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			Assert.True(await manager.RoleExistsAsync(roleName));
		}



		[Test]
		public async Task BadValidatorBlocksCreateRole()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			manager.RoleValidators.Clear();
			manager.RoleValidators.Add(new AlwaysBadValidator());
			var role = CreateTestRole("blocked");
			IdentityResultAssert.IsFailure(await manager.CreateAsync(role),
				AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"Role {await manager.GetRoleIdAsync(role) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
		}

		[Test]
		public async Task CanChainRoleValidators()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			manager.RoleValidators.Clear();
			manager.RoleValidators.Add(new AlwaysBadValidator());
			manager.RoleValidators.Add(new AlwaysBadValidator());
			var role = CreateTestRole("blocked");
			var result = await manager.CreateAsync(role);
			IdentityResultAssert.IsFailure(result, AlwaysBadValidator.ErrorMessage);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"Role {await manager.GetRoleIdAsync(role) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code};{AlwaysBadValidator.ErrorMessage.Code}.");
			Assert.AreEqual(2, result.Errors.Count());
		}

		[Test]
		public async Task BadValidatorBlocksRoleUpdate()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var role = CreateTestRole("poorguy");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			var error = AlwaysBadValidator.ErrorMessage;
			manager.RoleValidators.Clear();
			manager.RoleValidators.Add(new AlwaysBadValidator());
			IdentityResultAssert.IsFailure(await manager.UpdateAsync(role), error);
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"Role {await manager.GetRoleIdAsync(role) ?? NullValue} validation failed: {AlwaysBadValidator.ErrorMessage.Code}.");
		}

		[Test]
		public async Task CanDeleteRole()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var roleName = "delete" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			Assert.False(await manager.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			Assert.True(await manager.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await manager.DeleteAsync(role));
			Assert.False(await manager.RoleExistsAsync(roleName));
		}

		[Test]
		public async Task CanAddRemoveRoleClaim()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var role = CreateTestRole("ClaimsAddRemove");
			var roleSafe = CreateTestRole("ClaimsAdd");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(roleSafe));
			Claim[] claims = {new Claim("c", "v"), new Claim("c2", "v2"), new Claim("c2", "v3")};
			foreach (var c in claims)
			{
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(role, c));
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(roleSafe, c));
			}
			var roleClaims = await manager.GetClaimsAsync(role);
			var safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
			Assert.AreEqual(3, roleClaims.Count);
			Assert.AreEqual(3, safeRoleClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[0]));
			roleClaims = await manager.GetClaimsAsync(role);
			safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
			Assert.AreEqual(2, roleClaims.Count);
			Assert.AreEqual(3, safeRoleClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[1]));
			roleClaims = await manager.GetClaimsAsync(role);
			safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
			Assert.AreEqual(1, roleClaims.Count);
			Assert.AreEqual(3, safeRoleClaims.Count);
			IdentityResultAssert.IsSuccess(await manager.RemoveClaimAsync(role, claims[2]));
			roleClaims = await manager.GetClaimsAsync(role);
			safeRoleClaims = await manager.GetClaimsAsync(roleSafe);
			Assert.AreEqual(0, roleClaims.Count);
			Assert.AreEqual(3, safeRoleClaims.Count);
		}

		[Test]
		public async Task CanRoleFindById()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var role = CreateTestRole("FindByIdAsync");
			Assert.Null(await manager.FindByIdAsync(await manager.GetRoleIdAsync(role)));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			Assert.AreEqual(role.Id, (await manager.FindByIdAsync(await manager.GetRoleIdAsync(role))).Id);
		}

		[Test]
		public async Task CanRoleFindByName()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var roleName = "FindByNameAsync" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			Assert.Null(await manager.FindByNameAsync(roleName));
			Assert.False(await manager.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			Assert.AreEqual(role.Id, (await manager.FindByNameAsync(roleName)).Id);
		}

		[Test]
		public async Task CanUpdateRoleName()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var roleName = "update" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			Assert.False(await manager.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			Assert.True(await manager.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await manager.SetRoleNameAsync(role, "Changed"));
			IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
			Assert.False(await manager.RoleExistsAsync("update"));
			Assert.AreEqual(role.Id, (await manager.FindByNameAsync("Changed")).Id);
		}

		[Test]
		public async Task CanQueryableRoles()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			if (manager.SupportsQueryableRoles)
			{
				var roles = GenerateRoles("CanQuerableRolesTest", 4);
				foreach (var r in roles)
					IdentityResultAssert.IsSuccess(await manager.CreateAsync(r));
				Assert.AreEqual(roles.Count, manager.Roles.Count(RoleNameStartsWithPredicate("CanQuerableRolesTest")));
				Assert.Null(manager.Roles.FirstOrDefault(RoleNameEqualsPredicate("bogus")));
			}
		}

		[Test]
		public async Task CreateRoleFailsIfExists()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateRoleManager();
			var roleName = "dupeRole" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			Assert.False(await manager.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			Assert.True(await manager.RoleExistsAsync(roleName));
			var role2 = CreateTestRole(roleName, true);
			IdentityResultAssert.IsFailure(await manager.CreateAsync(role2));
		}

		[Test]
		public async Task CanAddUsersToRole()
		{
			if (ShouldSkipDbTests())
				return;
			var context = CreateTestContext();
			var manager = CreateManager(context);
			var roleManager = CreateRoleManager(context);
			var roleName = "AddUserTest" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
			TUser[] users =
			{
				CreateTestUser("1"), CreateTestUser("2"), CreateTestUser("3"), CreateTestUser("4")
			};
			foreach (var u in users)
			{
				IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
				IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(u, roleName));
				Assert.True(await manager.IsInRoleAsync(u, roleName));
			}
		}

		public async Task CanGetRolesForUser()
		{
			if (ShouldSkipDbTests())
				return;

			var context = CreateTestContext();
			var userManager = CreateManager(context);
			var roleManager = CreateRoleManager(context);
			var users = GenerateUsers("CanGetRolesForUser", 4);
			var roles = GenerateRoles("CanGetRolesForUserRole", 4);
			foreach (var u in users)
				IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
			foreach (var r in roles)
			{
				IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
				foreach (var u in users)
				{
					IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
					Assert.True(await userManager.IsInRoleAsync(u, await roleManager.GetRoleNameAsync(r)));
				}
			}

			foreach (var u in users)
			{
				var rs = await userManager.GetRolesAsync(u);
				Assert.AreEqual(roles.Count, rs.Count);
				foreach (var r in roles)
				{
					var expectedRoleName = await roleManager.GetRoleNameAsync(r);
					Assert.IsNotEmpty(rs.Where(role => role == expectedRoleName));
				}
			}
		}

		[Test]
		public async Task RemoveUserFromRoleWithMultipleRoles()
		{
			if (ShouldSkipDbTests())
				return;
			var context = CreateTestContext();
			var userManager = CreateManager(context);
			var roleManager = CreateRoleManager(context);
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));
			var roles = GenerateRoles("RemoveUserFromRoleWithMultipleRoles", 4);
			foreach (var r in roles)
			{
				IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
				IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, await roleManager.GetRoleNameAsync(r)));
				Assert.True(await userManager.IsInRoleAsync(user, await roleManager.GetRoleNameAsync(r)));
			}
			IdentityResultAssert.IsSuccess(
				await userManager.RemoveFromRoleAsync(user, await roleManager.GetRoleNameAsync(roles[2])));
			Assert.False(await userManager.IsInRoleAsync(user, await roleManager.GetRoleNameAsync(roles[2])));
		}

		[Test]
		public async Task CanRemoveUsersFromRole()
		{
			if (ShouldSkipDbTests())
				return;
			var context = CreateTestContext();
			var userManager = CreateManager(context);
			var roleManager = CreateRoleManager(context);
			var users = GenerateUsers("CanRemoveUsersFromRole", 4);
			foreach (var u in users)
				IdentityResultAssert.IsSuccess(await userManager.CreateAsync(u));
			var r = CreateTestRole("r1");
			var roleName = await roleManager.GetRoleNameAsync(r);
			IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(r));
			foreach (var u in users)
			{
				IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(u, roleName));
				Assert.True(await userManager.IsInRoleAsync(u, roleName));
			}
			foreach (var u in users)
			{
				IdentityResultAssert.IsSuccess(await userManager.RemoveFromRoleAsync(u, roleName));
				Assert.False(await userManager.IsInRoleAsync(u, roleName));
			}
		}

		[Test]
		public async Task RemoveUserNotInRoleFails()
		{
			if (ShouldSkipDbTests())
				return;
			var context = CreateTestContext();
			var userMgr = CreateManager(context);
			var roleMgr = CreateRoleManager(context);
			var roleName = "addUserDupeTest" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
			var result = await userMgr.RemoveFromRoleAsync(user, roleName);
			IdentityResultAssert.IsFailure(result, _errorDescriber.UserNotInRole(roleName));
			IdentityResultAssert.VerifyLogMessage(userMgr.Logger,
				$"User {await userMgr.GetUserIdAsync(user)} is not in role {roleName}.");
		}

		[Test]
		public async Task AddUserToRoleFailsIfAlreadyInRole()
		{
			if (ShouldSkipDbTests())
				return;
			var context = CreateTestContext();
			var userMgr = CreateManager(context);
			var roleMgr = CreateRoleManager(context);
			var roleName = "addUserDupeTest" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
			IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));
			Assert.True(await userMgr.IsInRoleAsync(user, roleName));
			IdentityResultAssert.IsFailure(await userMgr.AddToRoleAsync(user, roleName),
				_errorDescriber.UserAlreadyInRole(roleName));
			IdentityResultAssert.VerifyLogMessage(userMgr.Logger,
				$"User {await userMgr.GetUserIdAsync(user)} is already in role {roleName}.");
		}

		[Test]
		public async Task AddUserToRolesIgnoresDuplicates()
		{
			if (ShouldSkipDbTests())
				return;
			var context = CreateTestContext();
			var userMgr = CreateManager(context);
			var roleMgr = CreateRoleManager(context);
			var roleName = "addUserDupeTest" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
			Assert.False(await userMgr.IsInRoleAsync(user, roleName));
			IdentityResultAssert.IsSuccess(await userMgr.AddToRolesAsync(user, new[] { roleName, roleName }));
			Assert.True(await userMgr.IsInRoleAsync(user, roleName));
		}

		[Test]
		public async Task CanFindRoleByNameWithManager()
		{
			if (ShouldSkipDbTests())
				return;
			var roleMgr = CreateRoleManager();
			var roleName = "findRoleByNameTest" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
			Assert.NotNull(await roleMgr.FindByNameAsync(roleName));
		}

		[Test]
		public async Task CanFindRoleWithManager()
		{
			if (ShouldSkipDbTests())
				return;
			var roleMgr = CreateRoleManager();
			var roleName = "findRoleTest" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
			Assert.AreEqual(roleName, await roleMgr.GetRoleNameAsync(await roleMgr.FindByNameAsync(roleName)));
		}

		[Test]
		public async Task SetPhoneNumberTest()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser(phoneNumber: "123-456-7890");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.AreEqual("123-456-7890", await manager.GetPhoneNumberAsync(user));
			IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
			Assert.AreEqual("111-111-1111", await manager.GetPhoneNumberAsync(user));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CanChangePhoneNumber()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser(phoneNumber: "123-456-7890");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
			IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user, "111-111-1111", token1));
			Assert.True(await manager.IsPhoneNumberConfirmedAsync(user));
			Assert.AreEqual("111-111-1111", await manager.GetPhoneNumberAsync(user));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task ChangePhoneNumberFailsWithWrongToken()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser(phoneNumber: "123-456-7890");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user, "111-111-1111", "bogus"),
				"Invalid token.");

			Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
			Assert.AreEqual("123-456-7890", await manager.GetPhoneNumberAsync(user));
			Assert.AreEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task ChangePhoneNumberFailsWithWrongPhoneNumber()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser(phoneNumber: "123-456-7890");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
			IdentityResultAssert.IsFailure(await manager.ChangePhoneNumberAsync(user, "bogus", token1),
				"Invalid token.");
			Assert.False(await manager.IsPhoneNumberConfirmedAsync(user));
			Assert.AreEqual("123-456-7890", await manager.GetPhoneNumberAsync(user));
			Assert.AreEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CanVerifyPhoneNumber()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			const string num1 = "111-123-4567";
			const string num2 = "111-111-1111";
			var userId = await manager.GetUserIdAsync(user);
			var token1 = await manager.GenerateChangePhoneNumberTokenAsync(user, num1);

			var token2 = await manager.GenerateChangePhoneNumberTokenAsync(user, num2);
			Assert.AreNotEqual(token1, token2);
			Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num1));
			Assert.True(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num2));
			Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token2, num1));
			Assert.False(await manager.VerifyChangePhoneNumberTokenAsync(user, token1, num2));
		}

		[Test]
		public async Task CanChangeEmail()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser("foouser");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			var newEmail = await manager.GetUserNameAsync(user) + "@en.vec";
			var token1 = await manager.GenerateChangeEmailTokenAsync(user, newEmail);
			IdentityResultAssert.IsSuccess(await manager.ChangeEmailAsync(user, newEmail, token1));
			Assert.True(await manager.IsEmailConfirmedAsync(user));
			Assert.AreEqual(await manager.GetEmailAsync(user), newEmail);
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task CanChangeEmailWithDifferentTokenProvider()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager(null, null,
				s => s.Configure<IdentityOptions>(
					o => o.Tokens.ProviderMap["NewProvider2"] = new TokenProviderDescriptor(typeof(EmailTokenProvider<TUser>))));
			manager.Options.Tokens.ChangeEmailTokenProvider = "NewProvider2";
			var user = CreateTestUser("foouser");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			var newEmail = await manager.GetUserNameAsync(user) + "@en.vec";
			var token1 = await manager.GenerateChangeEmailTokenAsync(user, newEmail);
			IdentityResultAssert.IsSuccess(await manager.ChangeEmailAsync(user, newEmail, token1));
			Assert.True(await manager.IsEmailConfirmedAsync(user));
			Assert.AreEqual(await manager.GetEmailAsync(user), newEmail);
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task ChangeEmailFailsWithWrongToken()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser("foouser");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
			var oldEmail = email;
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "whatevah@foo.barf", "bogus"),
				"Invalid token.");
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: ChangeEmail:whatevah@foo.barf for user {await manager.GetUserIdAsync(user)}.");
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			Assert.AreEqual(await manager.GetEmailAsync(user), oldEmail);
			Assert.AreEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task ChangeEmailFailsWithEmail()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser("foouser");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
			var oldEmail = email;
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			var token1 = await manager.GenerateChangeEmailTokenAsync(user, "forgot@alrea.dy");
			IdentityResultAssert.IsFailure(await manager.ChangeEmailAsync(user, "oops@foo.barf", token1),
				"Invalid token.");
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyUserTokenAsync() failed with purpose: ChangeEmail:oops@foo.barf for user {await manager.GetUserIdAsync(user)}.");
			Assert.False(await manager.IsEmailConfirmedAsync(user));
			Assert.AreEqual(await manager.GetEmailAsync(user), oldEmail);
			Assert.AreEqual(stamp, await manager.GetSecurityStampAsync(user));
		}

		[Test]
		public async Task EmailFactorFailsAfterSecurityStampChangeTest()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var factorId = "Email"; //default
			var user = CreateTestUser("foouser");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var email = await manager.GetUserNameAsync(user) + "@diddly.bop";
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, email));
			var token = await manager.GenerateEmailConfirmationTokenAsync(user);
			await manager.ConfirmEmailAsync(user, token);

			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
			Assert.NotNull(token);
			IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
			Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
		}

		[Test]
		public async Task EnableTwoFactorChangesSecurityStamp()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			IdentityResultAssert.IsSuccess(await manager.SetTwoFactorEnabledAsync(user, true));
			Assert.AreNotEqual(stamp, await manager.GetSecurityStampAsync(user));
			Assert.True(await manager.GetTwoFactorEnabledAsync(user));
		}

		[Test]
		public async Task GenerateTwoFactorWithUnknownFactorProviderWillThrow()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			const string error = "No IUserTwoFactorTokenProvider<TUser> named 'bogus' is registered.";
			Assert.ThrowsAsync<NotSupportedException>(
					() => manager.GenerateTwoFactorTokenAsync(user, "bogus"), error);
			Assert.ThrowsAsync<NotSupportedException>(
					() => manager.VerifyTwoFactorTokenAsync(user, "bogus", "bogus"), error);
		}

		[Test]
		public async Task GetValidTwoFactorTestEmptyWithNoProviders()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var factors = await manager.GetValidTwoFactorProvidersAsync(user);
			Assert.NotNull(factors);
			Assert.True(!factors.Any());
		}

		[Test]
		public async Task CanGetSetUpdateAndRemoveUserToken()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.Null(await manager.GetAuthenticationTokenAsync(user, "provider", "name"));
			IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "provider", "name", "value"));
			Assert.AreEqual("value", await manager.GetAuthenticationTokenAsync(user, "provider", "name"));

			IdentityResultAssert.IsSuccess(await manager.SetAuthenticationTokenAsync(user, "provider", "name", "value2"));
			Assert.AreEqual("value2", await manager.GetAuthenticationTokenAsync(user, "provider", "name"));

			IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user, "whatevs", "name"));
			Assert.AreEqual("value2", await manager.GetAuthenticationTokenAsync(user, "provider", "name"));

			IdentityResultAssert.IsSuccess(await manager.RemoveAuthenticationTokenAsync(user, "provider", "name"));
			Assert.Null(await manager.GetAuthenticationTokenAsync(user, "provider", "name"));
		}

		[Test]
		public async Task CanGetValidTwoFactor()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var userId = await manager.GetUserIdAsync(user);
			var factors = await manager.GetValidTwoFactorProvidersAsync(user);
			Assert.NotNull(factors);
			Assert.False(factors.Any());
			IdentityResultAssert.IsSuccess(await manager.SetPhoneNumberAsync(user, "111-111-1111"));
			var token = await manager.GenerateChangePhoneNumberTokenAsync(user, "111-111-1111");
			IdentityResultAssert.IsSuccess(await manager.ChangePhoneNumberAsync(user, "111-111-1111", token));
			await manager.UpdateAsync(user);
			factors = await manager.GetValidTwoFactorProvidersAsync(user);
			Assert.NotNull(factors);
			Assert.AreEqual(1, factors.Count());
			Assert.AreEqual("Phone", factors[0]);
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, "test@test.com"));
			token = await manager.GenerateEmailConfirmationTokenAsync(user);
			await manager.ConfirmEmailAsync(user, token);
			factors = await manager.GetValidTwoFactorProvidersAsync(user);
			Assert.NotNull(factors);
			Assert.AreEqual(2, factors.Count());
			IdentityResultAssert.IsSuccess(await manager.SetEmailAsync(user, null));
			factors = await manager.GetValidTwoFactorProvidersAsync(user);
			Assert.NotNull(factors);
			Assert.AreEqual(1, factors.Count());
			Assert.AreEqual("Phone", factors[0]);
		}

		[Test]
		public async Task PhoneFactorFailsAfterSecurityStampChangeTest()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var factorId = "Phone"; // default
			var user = CreateTestUser(phoneNumber: "4251234567");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var stamp = await manager.GetSecurityStampAsync(user);
			Assert.NotNull(stamp);
			var token = await manager.GenerateTwoFactorTokenAsync(user, factorId);
			Assert.NotNull(token);
			IdentityResultAssert.IsSuccess(await manager.UpdateSecurityStampAsync(user));
			Assert.False(await manager.VerifyTwoFactorTokenAsync(user, factorId, token));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
		}

		[Test]
		public async Task VerifyTokenFromWrongTokenProviderFails()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser(phoneNumber: "4251234567");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var token = await manager.GenerateTwoFactorTokenAsync(user, "Phone");
			Assert.NotNull(token);
			Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "Email", token));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
		}

		[Test]
		public async Task VerifyWithWrongSmsTokenFails()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();
			var user = CreateTestUser(phoneNumber: "4251234567");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			Assert.False(await manager.VerifyTwoFactorTokenAsync(user, "Phone", "bogus"));
			IdentityResultAssert.VerifyLogMessage(manager.Logger,
				$"VerifyTwoFactorTokenAsync() failed for user {await manager.GetUserIdAsync(user)}.");
		}

		[Test]
		public async Task NullableDateTimeOperationTest()
		{
			if (ShouldSkipDbTests())
				return;
			var userMgr = CreateManager();
			var user = CreateTestUser(lockoutEnabled: true);
			IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));

			Assert.Null(await userMgr.GetLockoutEndDateAsync(user));

			// set LockoutDateEndDate to null
			await userMgr.SetLockoutEndDateAsync(user, null);
			Assert.Null(await userMgr.GetLockoutEndDateAsync(user));

			// set to a valid value
			await userMgr.SetLockoutEndDateAsync(user, DateTimeOffset.Parse("01/01/2014"));
			Assert.AreEqual(DateTimeOffset.Parse("01/01/2014"), await userMgr.GetLockoutEndDateAsync(user));
		}

		[Test]
		public async Task CanGetUsersWithClaims()
		{
			if (ShouldSkipDbTests())
				return;
			var manager = CreateManager();

			for (var i = 0; i < 6; i++)
			{
				var user = CreateTestUser();
				IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

				if (i % 2 == 0)
					IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("foo", "bar")));
			}

			Assert.AreEqual(3, (await manager.GetUsersForClaimAsync(new Claim("foo", "bar"))).Count);

			Assert.AreEqual(0, (await manager.GetUsersForClaimAsync(new Claim("123", "456"))).Count);
		}

		[Test]
		public async Task CanGetUsersInRole()
		{
			if (ShouldSkipDbTests())
				return;
			var context = CreateTestContext();
			var manager = CreateManager(context);
			var roleManager = CreateRoleManager(context);
			var roles = GenerateRoles("UsersInRole", 4);
			var roleNameList = new List<string>();

			foreach (var role in roles)
			{
				IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));
				roleNameList.Add(await roleManager.GetRoleNameAsync(role));
			}

			for (var i = 0; i < 6; i++)
			{
				var user = CreateTestUser();
				IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

				if (i % 2 == 0)
					IdentityResultAssert.IsSuccess(await manager.AddToRolesAsync(user, roleNameList));
			}

			foreach (var role in roles)
				Assert.AreEqual(3, (await manager.GetUsersInRoleAsync(await roleManager.GetRoleNameAsync(role))).Count);

			Assert.AreEqual(0, (await manager.GetUsersInRoleAsync("123456")).Count);
		}
	}

}
