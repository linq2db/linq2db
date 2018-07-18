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
			private static readonly IDataProvider _dataProvider = new SqlServerDataProvider("mssql", SqlServerVersion.v2012);

			public static Func<MyDB> CreateFactory(string connectionString, int retryCount, TimeSpan delay)
			{
				return () =>
				{
					var db = new MyDB(_dataProvider, connectionString)
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

		private Func<MyDB> _dbFactory;

		[OneTimeSetUp]
		public void SetUp()
		{
			var dbConnStr = DataConnection.GetConnectionString(ProviderName.SqlServer2008);
			_dbFactory = MyDB.CreateFactory(dbConnStr, 1, TimeSpan.FromSeconds(1));
		}

		[Test, IncludeDataContextSource(false, ProviderName.SqlServer2008)]
		public void TestConcurrentSelect(string context)
		{
			var users = new[]
			{
				new User { Id = Guid.NewGuid(), Name = "User1" },
				new User { Id = Guid.NewGuid(), Name = "User2" }
			};

			// Ensures that concurrent async queries for multiple db instances are handled correctly
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(users))
			{
				using (var db1 = _dbFactory())
				using (var db2 = _dbFactory())
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
