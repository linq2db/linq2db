using System;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Identity
{
	public partial class InMemoryUserStoreTests
	{
		public class WithGenerics : UserManagerTestBase<IdentityUserWithGenerics, MyIdentityRole, string>
		{
			[OneTimeSetUp]
			public void OneTimeSetUp()
			{
				var services = new ServiceCollection();
				services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
				_storage = new InMemoryStorage();
			}

			[OneTimeTearDown]
			public void OneTimeTearDown()
			{
				_storage.Dispose();
			}

			private UserStoreWithGenerics _store   = default!;
			private InMemoryStorage       _storage = default!;

			protected override TestConnectionFactory CreateTestContext()
			{
				var connectionString = _storage.ConnectionString; //"Data Source=:memory:;";

				var Factory = new TestConnectionFactory(new SQLiteDataProvider(ProviderName.SQLite), "InMemoryEFUserStoreTestWithGenerics",
				connectionString);
				CreateTables(Factory, connectionString);
				return Factory;
			}

			protected override void AddUserStore(IServiceCollection services, TestConnectionFactory? context = null)
			{
				_store = new UserStoreWithGenerics(context ?? CreateTestContext(), "TestContext");
				services.AddSingleton<IUserStore<IdentityUserWithGenerics>>(_store);
			}

			protected override void AddRoleStore(IServiceCollection services, TestConnectionFactory? context = null)
			{
				services.AddSingleton<IRoleStore<MyIdentityRole>>(
					new RoleStoreWithGenerics(context ?? CreateTestContext(), "TestContext"));
			}

			protected override IdentityUserWithGenerics CreateTestUser(string namePrefix = "", string email = "",
				string phoneNumber = "",
				bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?),
				bool useNamePrefixAsUserName = false)
			{
				return new IdentityUserWithGenerics
				{
					UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
					Email = email,
					PhoneNumber = phoneNumber,
					LockoutEnabled = lockoutEnabled,
					LockoutEnd = lockoutEnd
				};
			}

			protected override MyIdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
			{
				var roleName = useRoleNamePrefixAsRoleName
				? roleNamePrefix
				: string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
				return new MyIdentityRole(roleName);
			}

			protected override void SetUserPasswordHash(IdentityUserWithGenerics user, string hashedPassword)
			{
				user.PasswordHash = hashedPassword;
			}

			protected override Expression<Func<IdentityUserWithGenerics, bool>> UserNameEqualsPredicate(string userName)
			{
				return u => u.UserName == userName;
			}

			protected override Expression<Func<MyIdentityRole, bool>> RoleNameEqualsPredicate(string roleName)
			{
				return r => r.Name == roleName;
			}

			protected override Expression<Func<IdentityUserWithGenerics, bool>> UserNameStartsWithPredicate(string userName)
			{
				return u => u.UserName.StartsWith(userName);
			}

			protected override Expression<Func<MyIdentityRole, bool>> RoleNameStartsWithPredicate(string roleName)
			{
				return r => r.Name.StartsWith(roleName);
			}

			[Test]
			public async Task CanAddRemoveUserClaimWithIssuer()
			{
				if (ShouldSkipDbTests())
					return;
				var manager = CreateManager();
				var user = CreateTestUser();
				IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
				Claim[] claims =
				{new Claim("c1", "v1", null, "i1"), new Claim("c2", "v2", null, "i2"), new Claim("c2", "v3", null, "i3")};
				foreach (var c in claims)
					IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));

				var userId = await manager.GetUserIdAsync(user);
				var userClaims = await manager.GetClaimsAsync(user);
				Assert.AreEqual(3, userClaims.Count);
				Assert.AreEqual(3, userClaims.Intersect(claims, ClaimEqualityComparer.Default).Count());

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
			public async Task CanReplaceUserClaimWithIssuer()
			{
				if (ShouldSkipDbTests())
					return;
				var manager = CreateManager();
				var user = CreateTestUser();
				IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, new Claim("c", "a", "i")));
				var userClaims = await manager.GetClaimsAsync(user);
				Assert.AreEqual(1, userClaims.Count);
				var claim = new Claim("c", "b", "i");
				var oldClaim = userClaims.FirstOrDefault();
				IdentityResultAssert.IsSuccess(await manager.ReplaceClaimAsync(user, oldClaim, claim));
				var newUserClaims = await manager.GetClaimsAsync(user);
				Assert.AreEqual(1, newUserClaims.Count);
				var newClaim = newUserClaims.FirstOrDefault();
				Assert.AreEqual(claim.Type, newClaim.Type);
				Assert.AreEqual(claim.Value, newClaim.Value);
				Assert.AreEqual(claim.Issuer, newClaim.Issuer);
			}

			[Test]
			public async Task RemoveClaimWithIssuerOnlyAffectsUser()
			{
				if (ShouldSkipDbTests())
					return;
				var manager = CreateManager();
				var user = CreateTestUser();
				var user2 = CreateTestUser();
				IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
				IdentityResultAssert.IsSuccess(await manager.CreateAsync(user2));
				Claim[] claims =
				{new Claim("c", "v", null, "i1"), new Claim("c2", "v2", null, "i2"), new Claim("c2", "v3", null, "i3")};
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
		}

		public class IdentityUserWithGenerics : LinqToDB.Identity.IdentityUser<string, IdentityUserClaimWithIssuer, IdentityUserRoleWithDate,
	IdentityUserLoginWithContext>
		{
			public IdentityUserWithGenerics()
			{
				Id = Guid.NewGuid().ToString();
			}
		}

		public class UserStoreWithGenerics : UserStore<string, IdentityUserWithGenerics, MyIdentityRole,
			IdentityUserClaimWithIssuer, IdentityUserRoleWithDate, IdentityUserLoginWithContext,
			IdentityUserTokenWithStuff>
		{
			public UserStoreWithGenerics(TestConnectionFactory Factory, string loginContext)
				: base(Factory)
			{
				LoginContext = loginContext;
				Factory.CreateTable<IdentityUserClaimWithIssuer>();
				Factory.CreateTable<IdentityUserRoleWithDate>();
				Factory.CreateTable<IdentityUserLoginWithContext>();
				Factory.CreateTable<IdentityUserTokenWithStuff>();
			}

			public string LoginContext { get; set; }

			protected override IdentityUserRoleWithDate CreateUserRole(IdentityUserWithGenerics user, MyIdentityRole role)
			{
				return new IdentityUserRoleWithDate
				{
					RoleId = role.Id,
					UserId = user.Id,
					Created = DateTime.UtcNow
				};
			}

			protected override IdentityUserClaimWithIssuer CreateUserClaim(IdentityUserWithGenerics user, Claim claim)
			{
				return new IdentityUserClaimWithIssuer
				{
					UserId = user.Id,
					ClaimType = claim.Type,
					ClaimValue = claim.Value,
					Issuer = claim.Issuer
				};
			}

			protected override IdentityUserLoginWithContext CreateUserLogin(IdentityUserWithGenerics user, UserLoginInfo login)
			{
				return new IdentityUserLoginWithContext
				{
					UserId = user.Id,
					ProviderKey = login.ProviderKey,
					LoginProvider = login.LoginProvider,
					ProviderDisplayName = login.ProviderDisplayName,
					Context = LoginContext
				};
			}

			protected override IdentityUserTokenWithStuff CreateUserToken(IdentityUserWithGenerics user, string loginProvider,
				string name, string value)
			{
				return new IdentityUserTokenWithStuff
				{
					UserId = user.Id,
					LoginProvider = loginProvider,
					Name = name,
					Value = value,
					Stuff = "stuff"
				};
			}
		}

		public class RoleStoreWithGenerics : RoleStore<string, MyIdentityRole, LinqToDB.Identity.IdentityRoleClaim<string>>
		{
			private string _loginContext;

			public RoleStoreWithGenerics(TestConnectionFactory Factory, string loginContext) : base(Factory)
			{
				_loginContext = loginContext;
				Factory.CreateTable<LinqToDB.Identity.IdentityRoleClaim<string>>();
				Factory.CreateTable<IdentityUserRoleWithDate>();
			}

			protected override LinqToDB.Identity.IdentityRoleClaim<string> CreateRoleClaim(MyIdentityRole role, Claim claim)
			{
				return new LinqToDB.Identity.IdentityRoleClaim<string> { RoleId = role.Id, ClaimType = claim.Type, ClaimValue = claim.Value };
			}
		}

		public class IdentityUserClaimWithIssuer : LinqToDB.Identity.IdentityUserClaim<string>
		{
			public string Issuer { get; set; } = default!;

			public override Claim ToClaim()
			{
				return new Claim(ClaimType, ClaimValue, null, Issuer);
			}

			public override void InitializeFromClaim(Claim other)
			{
				ClaimValue = other.Value;
				ClaimType = other.Type;
				Issuer = other.Issuer;
			}
		}

		public class IdentityUserRoleWithDate : LinqToDB.Identity.IdentityUserRole<string>
		{
			public DateTime Created { get; set; }
		}

		public class MyIdentityRole : LinqToDB.Identity.IdentityRole<string, IdentityUserRoleWithDate, LinqToDB.Identity.IdentityRoleClaim<string>>
		{
			public MyIdentityRole()
			{
				Id = Guid.NewGuid().ToString();
			}

			public MyIdentityRole(string roleName) : this()
			{
				Name = roleName;
			}
		}

		public class IdentityUserTokenWithStuff : LinqToDB.Identity.IdentityUserToken<string>
		{
			public string Stuff { get; set; } = default!;
		}

		public class IdentityUserLoginWithContext : LinqToDB.Identity.IdentityUserLogin<string>
		{
			public string Context { get; set; } = default!;
		}

	}

}
