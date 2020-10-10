using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Identity
{
	using IdentityUser = LinqToDB.Identity.IdentityUser;
	using IdentityRole = LinqToDB.Identity.IdentityRole;

	public partial class UserStoreTests : UserManagerTestBase<IdentityUser, IdentityRole>
	{
		private ScratchDatabaseFixture _fixture = default!;

		[OneTimeSetUp]
		public  void OneTimeSetUp()
		{
			_fixture = new ScratchDatabaseFixture();
		}

		public void OneTimeTearDown()
		{
			_fixture.Dispose();
		}

		protected override bool ShouldSkipDbTests()
		{
			//return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
			return false;
		}

		//public class ApplicationDbContext : IdentityDataConnection<ApplicationUser>
		//{
		//    public ApplicationDbContext() : base()
		//    { }
		//}

		[Test]
		public void CanCreateUserUsingEF()
		{
			using (var db = CreateContext().GetConnection())
			{
				var guid = Guid.NewGuid().ToString();
				db.Insert(new IdentityUser {Id = guid, UserName = guid});

				Assert.True(db.GetTable<IdentityUser>().Any(u => u.UserName == guid));
				Assert.NotNull(db.GetTable<IdentityUser>().FirstOrDefault(u => u.UserName == guid));
			}
		}

		public TestConnectionFactory CreateContext(bool delete = false)
		{
			var factory = new TestConnectionFactory(new SqlServerDataProvider("*", SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient), "Test",
				_fixture.ConnectionString);

			CreateTables(factory, _fixture.ConnectionString);

			return factory;
		}

		protected override TestConnectionFactory CreateTestContext()
		{
			return CreateContext();
		}

		protected void EnsureDatabase()
		{
			CreateContext();
		}

		//     public ApplicationDbContext CreateAppContext()
		//     {
		//throw new NotImplementedException();
		//         //var db = DbUtil.Create<ApplicationDbContext>(_fixture.ConnectionString);
		//         //db.Database.EnsureCreated();
		//         //return db;
		//     }

		protected override void AddUserStore(IServiceCollection services, TestConnectionFactory? context = null)
		{
			services.AddSingleton<IUserStore<IdentityUser>>(
				new UserStore<IdentityUser, IdentityRole>(context ?? CreateTestContext()));
		}

		protected override void AddRoleStore(IServiceCollection services, TestConnectionFactory? context = null)
		{
			services.AddSingleton<IRoleStore<IdentityRole>>(
				new RoleStore<IdentityRole>(context ?? CreateTestContext()));
		}

		[Test]
		public async Task CanCreateUsingManager()
		{
			var manager = CreateManager();
			var guid = Guid.NewGuid().ToString();
			var user = new IdentityUser {UserName = "New" + guid};
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
		}

		[Test]
		public async Task TwoUsersSamePasswordDifferentHash()
		{
			var manager = CreateManager();
			var userA = new IdentityUser(Guid.NewGuid().ToString());
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(userA, "password"));
			var userB = new IdentityUser(Guid.NewGuid().ToString());
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(userB, "password"));

			Assert.AreNotEqual(userA.PasswordHash, userB.PasswordHash);
		}

		[Test]
		public async Task AddUserToUnknownRoleFails()
		{
			var manager = CreateManager();
			var u = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(u));
			Assert.ThrowsAsync<InvalidOperationException>(async () => await manager.AddToRoleAsync(u, "bogus"));
		}

		[Test]
		public async Task ConcurrentUpdatesWillFail()
		{
			var user = CreateTestUser();
			var factory = CreateContext();

			var manager = CreateManager(factory);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

			var manager1 = CreateManager(factory);
			var manager2 = CreateManager(factory);
			var user1 = await manager1.FindByIdAsync(user.Id);
			var user2 = await manager2.FindByIdAsync(user.Id);
			Assert.NotNull(user1);
			Assert.NotNull(user2);
			Assert.AreNotSame(user1, user2);
			user1.UserName = Guid.NewGuid().ToString();
			user2.UserName = Guid.NewGuid().ToString();
			IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(user1));
			IdentityResultAssert.IsFailure(await manager2.UpdateAsync(user2),
				new IdentityErrorDescriber().ConcurrencyFailure());
		}

		[Test]
		public async Task ConcurrentUpdatesWillFailWithDetachedUser()
		{
			var user = CreateTestUser();
			var factory = CreateContext();
			var manager = CreateManager(factory);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));

			var manager1 = CreateManager(factory);
			var manager2 = CreateManager(factory);
			var user2 = await manager2.FindByIdAsync(user.Id);
			Assert.NotNull(user2);
			Assert.AreNotSame(user, user2);
			user.UserName = Guid.NewGuid().ToString();
			user2.UserName = Guid.NewGuid().ToString();
			IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(user));
			IdentityResultAssert.IsFailure(await manager2.UpdateAsync(user2),
				new IdentityErrorDescriber().ConcurrencyFailure());
		}

		[Test]
		public async Task DeleteAModifiedUserWillFail()
		{
			var user = CreateTestUser();
			var factory = CreateContext();
			var manager = CreateManager(factory);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			var manager1 = CreateManager(factory);
			var manager2 = CreateManager(factory);
			var user1 = await manager1.FindByIdAsync(user.Id);
			var user2 = await manager2.FindByIdAsync(user.Id);
			Assert.NotNull(user1);
			Assert.NotNull(user2);
			Assert.AreNotSame(user1, user2);
			user1.UserName = Guid.NewGuid().ToString();
			IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(user1));
			IdentityResultAssert.IsFailure(await manager2.DeleteAsync(user2),
				new IdentityErrorDescriber().ConcurrencyFailure());
		}

		[Test]
		public async Task ConcurrentRoleUpdatesWillFail()
		{
			var role = new IdentityRole(Guid.NewGuid().ToString());
			var factory = CreateContext();

			var manager = CreateRoleManager(factory);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			var manager1 = CreateRoleManager(factory);
			var manager2 = CreateRoleManager(factory);
			var role1 = await manager1.FindByIdAsync(role.Id);
			var role2 = await manager2.FindByIdAsync(role.Id);
			Assert.NotNull(role1);
			Assert.NotNull(role2);
			Assert.AreNotSame(role1, role2);
			role1.Name = Guid.NewGuid().ToString();
			role2.Name = Guid.NewGuid().ToString();
			IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(role1));
			IdentityResultAssert.IsFailure(await manager2.UpdateAsync(role2),
				new IdentityErrorDescriber().ConcurrencyFailure());
		}

		[Test]
		public async Task ConcurrentRoleUpdatesWillFailWithDetachedRole()
		{
			var role = new IdentityRole(Guid.NewGuid().ToString());
			var factory = CreateContext();
			var manager = CreateRoleManager(factory);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			var manager1 = CreateRoleManager(factory);
			var manager2 = CreateRoleManager(factory);
			var role2 = await manager2.FindByIdAsync(role.Id);
			Assert.NotNull(role);
			Assert.NotNull(role2);
			Assert.AreNotSame(role, role2);
			role.Name = Guid.NewGuid().ToString();
			role2.Name = Guid.NewGuid().ToString();
			IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(role));
			IdentityResultAssert.IsFailure(await manager2.UpdateAsync(role2),
				new IdentityErrorDescriber().ConcurrencyFailure());
		}

		[Test]
		public async Task DeleteAModifiedRoleWillFail()
		{
			var role = new IdentityRole(Guid.NewGuid().ToString());
			var factory = CreateContext();
			var manager = CreateRoleManager(factory);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			var manager1 = CreateRoleManager(factory);
			var manager2 = CreateRoleManager(factory);
			var role1 = await manager1.FindByIdAsync(role.Id);
			var role2 = await manager2.FindByIdAsync(role.Id);
			Assert.NotNull(role1);
			Assert.NotNull(role2);
			Assert.AreNotSame(role1, role2);
			role1.Name = Guid.NewGuid().ToString();
			IdentityResultAssert.IsSuccess(await manager1.UpdateAsync(role1));
			IdentityResultAssert.IsFailure(await manager2.DeleteAsync(role2),
				new IdentityErrorDescriber().ConcurrencyFailure());
		}

		protected override IdentityUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
			bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?),
			bool useNamePrefixAsUserName = false)
		{
			return new IdentityUser
			{
				UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
				Email = email,
				PhoneNumber = phoneNumber,
				LockoutEnabled = lockoutEnabled,
				LockoutEnd = lockoutEnd
			};
		}

		protected override IdentityRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
		{
			var roleName = useRoleNamePrefixAsRoleName
				? roleNamePrefix
				: string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
			return new IdentityRole(roleName);
		}

		protected override void SetUserPasswordHash(IdentityUser user, string hashedPassword)
		{
			user.PasswordHash = hashedPassword;
		}

		protected override Expression<Func<IdentityUser, bool>> UserNameEqualsPredicate(string userName)
		{
			return u => u.UserName == userName;
		}

		protected override Expression<Func<IdentityRole, bool>> RoleNameEqualsPredicate(string roleName)
		{
			return r => r.Name == roleName;
		}

		protected override Expression<Func<IdentityRole, bool>> RoleNameStartsWithPredicate(string roleName)
		{
			return r => r.Name.StartsWith(roleName);
		}

		protected override Expression<Func<IdentityUser, bool>> UserNameStartsWithPredicate(string userName)
		{
			return u => u.UserName.StartsWith(userName);
		}

		[Test]
		public void SqlUserStoreMethodsThrowWhenDisposedTest()
		{
			var store = new UserStore<IdentityUser>(CreateTestContext());
			store.Dispose();
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddClaimsAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddLoginAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.AddToRoleAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetClaimsAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetLoginsAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetRolesAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.IsInRoleAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveClaimsAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveLoginAsync(null, null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveFromRoleAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.RemoveClaimsAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.ReplaceClaimAsync(null, null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByLoginAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByIdAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByNameAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.CreateAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.UpdateAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.DeleteAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.SetEmailConfirmedAsync(null, true));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetEmailConfirmedAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.SetPhoneNumberConfirmedAsync(null, true));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetPhoneNumberConfirmedAsync(null));
		}

		[Test]
		public void UserStorePublicNullCheckTest()
		{
			Assert.Throws<ArgumentNullException>(() => new UserStore<IdentityUser>(null), "factory");
			var store = new UserStore<IdentityUser>(CreateTestContext());
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetUserIdAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetUserNameAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetUserNameAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.CreateAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.UpdateAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.DeleteAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.AddClaimsAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.ReplaceClaimAsync(null, null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.RemoveClaimsAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetClaimsAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetLoginsAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetRolesAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.AddLoginAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.RemoveLoginAsync(null, null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.AddToRoleAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.RemoveFromRoleAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.IsInRoleAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetPasswordHashAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetPasswordHashAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetSecurityStampAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetSecurityStampAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.AddLoginAsync(new IdentityUser("fake"), null), "login");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.AddClaimsAsync(new IdentityUser("fake"), null), "claims");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.RemoveClaimsAsync(new IdentityUser("fake"), null), "claims");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetEmailConfirmedAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetEmailConfirmedAsync(null, true), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetEmailAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetEmailAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetPhoneNumberAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetPhoneNumberAsync(null, null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetPhoneNumberConfirmedAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetPhoneNumberConfirmedAsync(null, true), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetTwoFactorEnabledAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetTwoFactorEnabledAsync(null, true), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetAccessFailedCountAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetLockoutEnabledAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetLockoutEnabledAsync(null, false), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetLockoutEndDateAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetLockoutEndDateAsync(null, new DateTimeOffset()), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.ResetAccessFailedCountAsync(null), "user");
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.IncrementAccessFailedCountAsync(null), "user");
			Assert.ThrowsAsync<ArgumentException>(async () => await store.AddToRoleAsync(new IdentityUser("fake"), null), "normalizedRoleName");
			Assert.ThrowsAsync<ArgumentException>(async () => await store.RemoveFromRoleAsync(new IdentityUser("fake"), null), "normalizedRoleName");
			Assert.ThrowsAsync<ArgumentException>(async () => await store.IsInRoleAsync(new IdentityUser("fake"), null), "normalizedRoleName");
			Assert.ThrowsAsync<ArgumentException>(async () => await store.AddToRoleAsync(new IdentityUser("fake"), ""), "normalizedRoleName");
			Assert.ThrowsAsync<ArgumentException>(async () => await store.RemoveFromRoleAsync(new IdentityUser("fake"), ""), "normalizedRoleName");
			Assert.ThrowsAsync<ArgumentException>(async () => await store.IsInRoleAsync(new IdentityUser("fake"), ""), "normalizedRoleName");
		}
	}

	public class ApplicationUser : IdentityUser
	{
	}
}
