using System;
using System.Globalization;
using System.Threading.Tasks;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1174Tests : TestBase
	{
		public class MyDB : DataConnection
		{
			public static Func<MyDB> CreateFactory(string configuration, int retryCount, TimeSpan delay)
			{
				return () =>
				{
					var db = new MyDB(configuration)
					{
						// No exception if policy removed
						RetryPolicy = new SqlServerRetryPolicy(retryCount, delay, null)
					};
					return db;
				};
			}
			public ITable<User> Users => GetTable<User>();

			public MyDB()
			{
			}

			public MyDB(string configuration)
				: base(configuration)
			{
			}
			public MyDB(IDataProvider dataProvider, string connectionString) : base(dataProvider, connectionString)
			{
			}
		}

		public class User
		{
			[PrimaryKey, NotNull] public Guid Id { get; set; }
			[Column, Nullable] public string Name { get; set; }
		}

		[Test, Combinatorial]
		public void TestConcurrentSelect(
			[IncludeDataSources(false, ProviderName.SqlServer,ProviderName.SqlServer2008, ProviderName.SqlServer2012)] string context
		)
		{
			var dbFactory = MyDB.CreateFactory(context, 1, TimeSpan.FromSeconds(1));

			var users = new[]
			{
				new User { Id = Guid.NewGuid(), Name = "User1" },
				new User { Id = Guid.NewGuid(), Name = "User2" }
			};

			// Ensures that concurrent async queries for multiple db instances are handled correctly
			using (var db = dbFactory())
			using (db.CreateLocalTable(users))
			{
				using (var db1 = dbFactory())
				using (var db2 = dbFactory())
				{
					var user1Task = db1.Users.FirstAsync();
					var user2Task = db2.Users.FirstAsync();

					Assert.DoesNotThrowAsync(() => Task.WhenAll(user1Task, user2Task));

					Assert.IsNotNull(user1Task.Result);
					Assert.IsNotNull(user2Task.Result);
				}
			}
		}		
	}
}
