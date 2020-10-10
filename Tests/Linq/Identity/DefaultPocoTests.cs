using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Identity;
using LinqToDB.Mapping;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Microsoft.AspNetCore.Identity;

using IdentityUser = LinqToDB.Identity.IdentityUser;
using IdentityRole = LinqToDB.Identity.IdentityRole;

namespace Tests.Identity
{
	public class DefaultPocoTests
	{
		private ApplicationBuilder _builder = default!;
		private readonly SqlServerDataProvider _dataProvider = new SqlServerDataProvider("*", SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient);

		static DefaultPocoTests()
		{
			MappingSchema.Default
				.GetFluentMappingBuilder()
				
				.Entity<IdentityUser>()
				.HasPrimaryKey(_ => _.Id)
				.Property(_ => _.Id)
				.HasLength(255)
				.IsNullable(false)

				.Entity<IdentityRole>()
				.HasPrimaryKey(_ => _.Id)
				.Property(_ => _.Id)
				.HasLength(255)
				.IsNullable(false);
		}


		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			var services = new ServiceCollection();
			var fixture  = new ScratchDatabaseFixture();
			var factory  = new TestConnectionFactory(_dataProvider, nameof(DefaultPocoTests), fixture.ConnectionString);
			services
				.AddIdentity<IdentityUser, IdentityRole>()
				.AddLinqToDBStores(factory);

			services.AddTransient(_ => new IdentityDataConnection(_dataProvider, fixture.ConnectionString));

			services.AddLogging();

			var provider = services.BuildServiceProvider();
			_builder = new ApplicationBuilder(provider);

			factory.CreateTables<IdentityUser, IdentityRole, string>();
		}

		[OneTimeTearDown]
		public void OneTimeTearsDown()
		{
		}

		[Test]
		public async Task EnsureStartupUsageWorks()
		{
			var userStore   = _builder.ApplicationServices.GetRequiredService<IUserStore<IdentityUser>>();
			var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();

			Assert.NotNull(userStore);
			Assert.NotNull(userManager);

			const string userName = "admin";
			const string password = "1qaz@WSX";
			var user = new IdentityUser {UserName = userName};
			IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user, password));
			IdentityResultAssert.IsSuccess(await userManager.DeleteAsync(user));
		}

		[Test]
		public async Task CanIncludeUserClaimsTest()
		{
			// Arrange
			var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
			var dbContext   = _builder.ApplicationServices.GetRequiredService<IdentityDataConnection>();

			var username = "user" + new Random().Next();
			var user = new IdentityUser {UserName = username};
			IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

			for (var i = 0; i < 10; i++)
				IdentityResultAssert.IsSuccess(await userManager.AddClaimAsync(user, new Claim(i.ToString(), "foo")));

			user = dbContext.Users.LoadWith(x => x.Claims).FirstOrDefault(x => x.UserName == username);

			// Assert
			Assert.NotNull(user);
			Assert.NotNull(user.Claims);
			Assert.AreEqual(10, user.Claims.Count());
		}

		[Test]
		public async Task CanIncludeUserLoginsTest()
		{
			// Arrange
			var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
			var dbContext   = _builder.ApplicationServices.GetRequiredService<IdentityDataConnection>();

			var username = "user" + new Random().Next();
			var user = new IdentityUser {UserName = username};
			IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

			for (var i = 0; i < 10; i++)
				IdentityResultAssert.IsSuccess(
					await userManager.AddLoginAsync(user, new UserLoginInfo("foo" + i, "bar" + i, "foo")));

			user = dbContext.Users.LoadWith(x => x.Logins).FirstOrDefault(x => x.UserName == username);

			// Assert
			Assert.NotNull(user);
			Assert.NotNull(user.Logins);
			Assert.AreEqual(10, user.Logins.Count());
		}

		[Test]
		public async Task CanIncludeUserRolesTest()
		{
			// Arrange
			var userManager = _builder.ApplicationServices.GetRequiredService<UserManager<IdentityUser>>();
			var roleManager = _builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
			var dbContext   = _builder.ApplicationServices.GetRequiredService<IdentityDataConnection>();

			const string roleName = "Admin";
			for (var i = 0; i < 10; i++)
				IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(new IdentityRole(roleName + i)));
			var username = "user" + new Random().Next();
			var user = new IdentityUser {UserName = username};
			IdentityResultAssert.IsSuccess(await userManager.CreateAsync(user));

			for (var i = 0; i < 10; i++)
				IdentityResultAssert.IsSuccess(await userManager.AddToRoleAsync(user, roleName + i));

			user = dbContext.Users.LoadWith(x => x.Roles).FirstOrDefault(x => x.UserName == username);

			// Assert
			Assert.NotNull(user);
			Assert.NotNull(user.Roles);
			Assert.AreEqual(10, user.Roles.Count());

			for (var i = 0; i < 10; i++)
			{
				var role = dbContext.Roles.LoadWith(r => r.Users).FirstOrDefault(r => r.Name == roleName + i);
				Assert.NotNull(role);
				Assert.NotNull(role.Users);
				Assert.AreEqual(1, role.Users.Count());
			}
		}

		[Test]
		public async Task CanIncludeRoleClaimsTest()
		{
			// Arrange
			var roleManager = _builder.ApplicationServices.GetRequiredService<RoleManager<IdentityRole>>();
			var dbContext = _builder.ApplicationServices.GetRequiredService<IdentityDataConnection>();

			var role = new IdentityRole("Admin");

			IdentityResultAssert.IsSuccess(await roleManager.CreateAsync(role));

			for (var i = 0; i < 10; i++)
				IdentityResultAssert.IsSuccess(await roleManager.AddClaimAsync(role, new Claim("foo" + i, "bar" + i)));

			role = dbContext.Roles.LoadWith(x => x.Claims).FirstOrDefault(x => x.Name == "Admin");

			// Assert
			Assert.NotNull(role);
			Assert.NotNull(role.Claims);
			Assert.AreEqual(10, role.Claims.Count());
		}
	}
}
