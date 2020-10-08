using System;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

using IdentityRole = LinqToDB.Identity.IdentityRole;

namespace Tests.Identity
{
	public class InMemoryRoleStoreTests
	{
		[OneTimeSetUp]
		public void OneTimeSetup()
		{
			_storage = new InMemoryStorage();
		}

		[OneTimeTearDown]
		public void OneTimeTearsDown()
		{
			_storage?.Dispose();
		}

		private InMemoryStorage? _storage;

		private IConnectionFactory GetConnectionFactory()
		{
			var connectionString = _storage?.ConnectionString;

			var factory = new TestConnectionFactory(new SQLiteDataProvider(ProviderName.SQLite), "RoleStoreTest", connectionString);
			factory.CreateTables<LinqToDB.Identity.IdentityUser, IdentityRole, string>();
			return factory;
		}

		[Test]
		public async Task CanCreateRoleWithSingletonManager()
		{
			var services = TestIdentityFactory.CreateTestServices();
			services.AddSingleton(GetConnectionFactory());
			services.AddTransient<IRoleStore<IdentityRole>, RoleStore<IdentityRole>>();
			services.AddSingleton<RoleManager<IdentityRole>>();
			var provider = services.BuildServiceProvider();
			var manager = provider.GetRequiredService<RoleManager<IdentityRole>>();
			Assert.NotNull(manager);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityRole("someRole")));
		}

		[Test]
		public async Task CanCreateUsingAddRoleManager()
		{
			var manager = TestIdentityFactory.CreateRoleManager(GetConnectionFactory());
			Assert.NotNull(manager);
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(new IdentityRole("arole")));
		}

		[Test]
		public async Task CanUpdateRoleName()
		{
			var manager = TestIdentityFactory.CreateRoleManager(GetConnectionFactory());
			var role = new IdentityRole("UpdateRoleName");
			IdentityResultAssert.IsSuccess(await manager.CreateAsync(role));
			Assert.Null(await manager.FindByNameAsync("New"));
			IdentityResultAssert.IsSuccess(await manager.SetRoleNameAsync(role, "New"));
			IdentityResultAssert.IsSuccess(await manager.UpdateAsync(role));
			Assert.NotNull(await manager.FindByNameAsync("New"));
			Assert.Null(await manager.FindByNameAsync("UpdateAsync"));
		}

		[Test]
		public void RoleStoreMethodsThrowWhenDisposedTest()
		{
			var store = new RoleStore<IdentityRole>(GetConnectionFactory());
			store.Dispose();
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByIdAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.FindByNameAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetRoleIdAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.GetRoleNameAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.SetRoleNameAsync(null, null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.CreateAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.UpdateAsync(null));
			Assert.ThrowsAsync<ObjectDisposedException>(async () => await store.DeleteAsync(null));
		}

		[Test]
		public void RoleStorePublicNullCheckTest()
		{
			Assert.Throws<ArgumentNullException>(() => new RoleStore<IdentityRole>(null));
			var store = new RoleStore<IdentityRole>(GetConnectionFactory());
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetRoleIdAsync(null));
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.GetRoleNameAsync(null));
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.SetRoleNameAsync(null, null));
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.CreateAsync(null));
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.UpdateAsync(null));
			Assert.ThrowsAsync<ArgumentNullException>(async () => await store.DeleteAsync(null));
		}
	}
}
