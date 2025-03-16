using System;
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
			public static Func<MyDB> CreateFactory(string configuration, int retryCount, TimeSpan delay, double randomFactor, double exponentialBase, TimeSpan coefficient)
			{
				return () =>
				{
					var db = new MyDB(configuration)
					{
						// No exception if policy removed
						RetryPolicy = new SqlServerRetryPolicy(retryCount, delay, randomFactor, exponentialBase, coefficient, null)
					};
					return db;
				};
			}
			public ITable<User> Users => this.GetTable<User>();

			public MyDB()
			{
			}

			public MyDB(string configuration)
				: base(configuration)
			{
			}
			public MyDB(IDataProvider dataProvider, string connectionString) : base(new DataOptions().UseConnectionString(dataProvider, connectionString))
			{
			}
		}

		public class User
		{
			[PrimaryKey, NotNull] public Guid Id { get; set; }
			[Column, Nullable] public string? Name { get; set; }
		}

		[Test]
		public void TestConcurrentSelect([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			var dbFactory = MyDB.CreateFactory(context, 1, TimeSpan.FromSeconds(1), LinqToDB.Common.Configuration.RetryPolicy.DefaultRandomFactor, LinqToDB.Common.Configuration.RetryPolicy.DefaultExponentialBase, LinqToDB.Common.Configuration.RetryPolicy.DefaultCoefficient);

			var users = new[]
			{
				new User { Id = TestData.Guid1, Name = "User1" },
				new User { Id = TestData.Guid2, Name = "User2" }
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

					Assert.Multiple(() =>
					{
						Assert.That(user1Task.Result, Is.Not.Null);
						Assert.That(user2Task.Result, Is.Not.Null);
					});
				}
			}
		}
	}
}
