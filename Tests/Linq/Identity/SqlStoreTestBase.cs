using System;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Tests.Identity
{
	public abstract class SqlStoreTestBase<TUser, TRole, TKey> : UserManagerTestBase<TUser, TRole, TKey>
		where TUser : LinqToDB.Identity.IdentityUser<TKey>, new()
		where TRole : LinqToDB.Identity.IdentityRole<TKey>, new()
		where TKey : IEquatable<TKey>
	{
		private ScratchDatabaseFixture _fixture = default!;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_fixture = new ScratchDatabaseFixture();
		}

		[OneTimeTearDown]
		public void OneTimeTearDown()
		{
			_fixture.Dispose();
		}

		protected override bool ShouldSkipDbTests()
		{
			//return TestPlatformHelper.IsMono || !TestPlatformHelper.IsWindows;
			return false;
		}


		protected override TUser CreateTestUser(string namePrefix = "", string email = "", string phoneNumber = "",
			bool lockoutEnabled = false, DateTimeOffset? lockoutEnd = default(DateTimeOffset?),
			bool useNamePrefixAsUserName = false)
		{
			return new TUser
			{
				UserName = useNamePrefixAsUserName ? namePrefix : string.Format("{0}{1}", namePrefix, Guid.NewGuid()),
				Email = email,
				PhoneNumber = phoneNumber,
				LockoutEnabled = lockoutEnabled,
				LockoutEnd = lockoutEnd
			};
		}

		protected override TRole CreateTestRole(string roleNamePrefix = "", bool useRoleNamePrefixAsRoleName = false)
		{
			var roleName = useRoleNamePrefixAsRoleName
				? roleNamePrefix
				: string.Format("{0}{1}", roleNamePrefix, Guid.NewGuid());
			return new TRole {Name = roleName};
		}

		protected override Expression<Func<TRole, bool>> RoleNameEqualsPredicate(string roleName)
		{
			return r => r.Name == roleName;
		}

		protected override Expression<Func<TUser, bool>> UserNameEqualsPredicate(string userName)
		{
			return u => u.UserName == userName;
		}

		protected override Expression<Func<TRole, bool>> RoleNameStartsWithPredicate(string roleName)
		{
			return r => r.Name.StartsWith(roleName);
		}

		protected override Expression<Func<TUser, bool>> UserNameStartsWithPredicate(string userName)
		{
			return u => u.UserName.StartsWith(userName);
		}


		public TestConnectionFactory CreateContext()
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

		protected override void AddUserStore(IServiceCollection services, TestConnectionFactory? context = null)
		{
			services.AddSingleton<IUserStore<TUser>>(
				new UserStore<TKey, TUser, TRole>(CreateTestContext()));
		}

		protected override void AddRoleStore(IServiceCollection services, TestConnectionFactory? context = null)
		{
			services.AddSingleton<IRoleStore<TRole>>(
				new RoleStore<TKey, TRole>(CreateTestContext()));
		}

		protected override void SetUserPasswordHash(TUser user, string hashedPassword)
		{
			user.PasswordHash = hashedPassword;
		}

		[Test]
		public void EnsureDefaultSchema()
		{
			VerifyDefaultSchema(CreateContext().GetConnection());
		}

		internal static void VerifyDefaultSchema(DataConnection dbContext)
		{
			var sqlConn = dbContext.Connection;

			using (var db = new SqlConnection(sqlConn.ConnectionString))
			{
				var ms = dbContext.MappingSchema;
				var u  = ms.GetEntityDescriptor(typeof(TUser));
				var r  = ms.GetEntityDescriptor(typeof(TRole));
				var ur = ms.GetEntityDescriptor(typeof(LinqToDB.Identity.IdentityUserRole<TKey>));
				var uc = ms.GetEntityDescriptor(typeof(LinqToDB.Identity.IdentityUserClaim<TKey>));
				var ul = ms.GetEntityDescriptor(typeof(LinqToDB.Identity.IdentityUserLogin<TKey>));
				var ut = ms.GetEntityDescriptor(typeof(LinqToDB.Identity.IdentityUserToken<TKey>));


				db.Open();
				Assert.True(VerifyColumns(db, u.TableName, "Id", "UserName", "Email", "PasswordHash", "SecurityStamp",
					"EmailConfirmed", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled",
					"LockoutEnd", "AccessFailedCount", "ConcurrencyStamp", "NormalizedUserName", "NormalizedEmail"));
				Assert.True(VerifyColumns(db, r.TableName, "Id", "Name", "NormalizedName", "ConcurrencyStamp"));
				Assert.True(VerifyColumns(db, ur.TableName, "UserId", "RoleId"));
				Assert.True(VerifyColumns(db, uc.TableName, "Id", "UserId", "ClaimType", "ClaimValue"));
				Assert.True(VerifyColumns(db, ul.TableName, "UserId", "ProviderKey", "LoginProvider", "ProviderDisplayName"));
				Assert.True(VerifyColumns(db, ut.TableName, "UserId", "LoginProvider", "Name", "Value"));

				db.Close();
			}
		}

		internal static bool VerifyColumns(SqlConnection conn, string table, params string[] columns)
		{
			var count = 0;
			using (
				var command =
					new SqlCommand("SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.COLUMNS where TABLE_NAME=@Table", conn))
			{
				command.Parameters.Add(new SqlParameter("Table", table));
				using (var reader = command.ExecuteReader())
				{
					while (reader.Read())
					{
						count++;
						if (!columns.Contains(reader.GetString(0)))
							return false;
					}
					return count == columns.Length;
				}
			}
		}

		internal static void VerifyIndex(SqlConnection conn, string table, string index, bool isUnique = false)
		{
			using (
				var command =
					new SqlCommand(
						"SELECT COUNT(*) FROM sys.indexes where NAME=@Index AND object_id = OBJECT_ID(@Table) AND is_unique = @Unique",
						conn))
			{
				command.Parameters.Add(new SqlParameter("Index", index));
				command.Parameters.Add(new SqlParameter("Table", table));
				command.Parameters.Add(new SqlParameter("Unique", isUnique));
				using (var reader = command.ExecuteReader())
				{
					Assert.True(reader.Read());
					Assert.True(reader.GetInt32(0) > 0);
				}
			}
		}

		[Test]
		public async Task DeleteRoleNonEmptySucceedsTest()
		{
			// Need fail if not empty?
			var context = CreateTestContext();
			var userMgr = CreateManager(context);
			var roleMgr = CreateRoleManager(context);
			var roleName = "delete" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			Assert.False(await roleMgr.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));
			var roles = await userMgr.GetRolesAsync(user);
			Assert.AreEqual(1, roles.Count);
			IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));
			Assert.Null(await roleMgr.FindByNameAsync(roleName));
			Assert.False(await roleMgr.RoleExistsAsync(roleName));
			// REVIEW: We should throw if deleteing a non empty role?
			roles = await userMgr.GetRolesAsync(user);

			Assert.AreEqual(0, roles.Count());
		}

		[Test]
		public async Task DeleteUserRemovesFromRoleTest()
		{
			// Need fail if not empty?
			var userMgr = CreateManager();
			var roleMgr = CreateRoleManager();
			var roleName = "deleteUserRemove" + Guid.NewGuid();
			var role = CreateTestRole(roleName, true);
			Assert.False(await roleMgr.RoleExistsAsync(roleName));
			IdentityResultAssert.IsSuccess(await roleMgr.CreateAsync(role));
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await userMgr.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await userMgr.AddToRoleAsync(user, roleName));

			var roles = await userMgr.GetRolesAsync(user);
			Assert.AreEqual(1, roles.Count());

			IdentityResultAssert.IsSuccess(await userMgr.DeleteAsync(user));
			IdentityResultAssert.IsSuccess(await roleMgr.DeleteAsync(role));

			roles = await userMgr.GetRolesAsync(user);
			Assert.AreEqual(0, roles.Count);
		}


		[Test]
		public void CanCreateUserTest()
		{
			using (var db = CreateContext().GetConnection())
			{
				var user = CreateTestUser();
				db.Insert(user);

				Assert.True(db.GetTable<TUser>().Any(u => u.UserName == user.UserName));
				Assert.NotNull(db.GetTable<TUser>().FirstOrDefault(u => u.UserName == user.UserName));
			}
		}

		[Test]
		public async Task CanCreateUsingManagerTest()
		{
			var manager = CreateManager();
			var user = CreateTestUser();
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(await manager.DeleteAsync(user));
		}

		private async Task LazyLoadTestSetup(TUser user)
		{
			var context = CreateContext();
			var manager = CreateManager(context);
			var role = CreateRoleManager(context);
			var admin = CreateTestRole("Admin" + Guid.NewGuid());
			var local = CreateTestRole("Local" + Guid.NewGuid());
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(user));
			IdentityResultAssert.IsSuccess(
				await manager.AddLoginAsync(user, new UserLoginInfo("provider", user.Id.ToString(), "display")));
			IdentityResultAssert.IsSuccess(await role.CreateAsync(admin));
			IdentityResultAssert.IsSuccess(await role.CreateAsync(local));
			IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, admin.Name));
			IdentityResultAssert.IsSuccess(await manager.AddToRoleAsync(user, local.Name));
			Claim[] userClaims =
			{
				new Claim("Whatever", "Value"),
				new Claim("Whatever2", "Value2")
			};
			foreach (var c in userClaims)
				IdentityResultAssert.IsSuccess(await manager.AddClaimAsync(user, c));
		}

		[Test]
		public async Task LoadFromDbFindByIdTest()
		{
			var user = CreateTestUser();
			await LazyLoadTestSetup(user);

			var factory = CreateContext();
			var manager = CreateManager(factory);

			var userById = await manager.FindByIdAsync(user.Id.ToString());
			Assert.AreEqual(2, (await manager.GetClaimsAsync(userById)).Count);
			Assert.AreEqual(1, (await manager.GetLoginsAsync(userById)).Count);
			Assert.AreEqual(2, (await manager.GetRolesAsync(userById)).Count);
		}

		[Test]
		public async Task LoadFromDbFindByNameTest()
		{
			var db = CreateContext();
			var user = CreateTestUser();
			await LazyLoadTestSetup(user);

			var manager = CreateManager(db);
			var userByName = await manager.FindByNameAsync(user.UserName);
			Assert.AreEqual(2, (await manager.GetClaimsAsync(userByName)).Count);
			Assert.AreEqual(1, (await manager.GetLoginsAsync(userByName)).Count);
			Assert.AreEqual(2, (await manager.GetRolesAsync(userByName)).Count);
		}

		[Test]
		public async Task LoadFromDbFindByLoginTest()
		{
			var db = CreateContext();
			var user = CreateTestUser();
			await LazyLoadTestSetup(user);

			var manager = CreateManager(db);
			var userByLogin = await manager.FindByLoginAsync("provider", user.Id.ToString());
			Assert.AreEqual(2, (await manager.GetClaimsAsync(userByLogin)).Count);
			Assert.AreEqual(1, (await manager.GetLoginsAsync(userByLogin)).Count);
			Assert.AreEqual(2, (await manager.GetRolesAsync(userByLogin)).Count);
		}

		[Test]
		public async Task LoadFromDbFindByEmailTest()
		{
			var db = CreateContext();
			var user = CreateTestUser();
			user.Email = "fooz@fizzy.pop";
			await LazyLoadTestSetup(user);

			var manager = CreateManager(db);
			var userByEmail = await manager.FindByEmailAsync(user.Email);
			Assert.AreEqual(2, (await manager.GetClaimsAsync(userByEmail)).Count);
			Assert.AreEqual(1, (await manager.GetLoginsAsync(userByEmail)).Count);
			Assert.AreEqual(2, (await manager.GetRolesAsync(userByEmail)).Count);
		}
	}
}
