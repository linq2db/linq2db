using System;
using System.Linq.Expressions;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

using IdentityRole = LinqToDB.Identity.IdentityRole;
using IdentityUser = LinqToDB.Identity.IdentityUser;

namespace Tests.Identity
{
	public partial class InMemoryUserStoreTests : UserManagerTestBase<IdentityUser, IdentityRole>
	{
		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			_storage = new InMemoryStorage();
		}

		[OneTimeTearDown]
		public void OneTimeTearsDown()
		{
			_storage.Dispose();
		}

		private InMemoryStorage _storage = default!;

		public TestConnectionFactory CreateContext(bool delete = false)
		{
			var connectionString = _storage.ConnectionString;

			var factory = new TestConnectionFactory(new SQLiteDataProvider(ProviderName.SQLite), "RoleStoreTest", connectionString);
			factory.CreateTables<IdentityUser, IdentityRole, string>();
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
	}

}
