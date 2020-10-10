using System;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace Tests.Identity
{
	public class CustomPocoTests
	{
		private readonly SqlServerDataProvider _dataProvider = new SqlServerDataProvider("*", SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient);
		private TestConnectionFactory _factory = default!;
		private ScratchDatabaseFixture _fixture = default!;

		[OneTimeSetUp]
		public void OneTimeSetUp()
		{
			_fixture = new ScratchDatabaseFixture();
			_factory = new TestConnectionFactory(_dataProvider, nameof(CustomPocoTests), _fixture.ConnectionString);
		}

		[OneTimeTearDown]
		public void OneTimeTearsDown()
		{
			_fixture?.Dispose();
		}

		public CustomDbContext<TKey> CreateContext<TKey>(bool delete = false) where TKey : IEquatable<TKey>
		{
			var db = new CustomDbContext<TKey>(_dataProvider, _fixture.ConnectionString);
			if (delete)
				_factory.DropTable<User<TKey>>();

			_factory.CreateTable<User<TKey>>();
			return db;
		}

		[Test]
		public void CanUpdateNameGuid()
		{
			using (var db = CreateContext<Guid>(true))
			{
				var oldName = Guid.NewGuid().ToString();
				var user = new User<Guid> {UserName = oldName, Id = Guid.NewGuid()};
				db.Insert(user);
				var newName = Guid.NewGuid().ToString();

				user.UserName = newName;
				db.Update(user);

				Assert.Null(db.Users.SingleOrDefault(u => u.UserName == oldName));
				Assert.AreEqual(user, db.Users.Single(u => u.UserName == newName));
			}
		}

		[Test]
		public void CanUpdateNameString()
		{
			using (var db = CreateContext<string>(true))
			{
				var oldName = Guid.NewGuid().ToString();
				var user = new User<string> {UserName = oldName, Id = Guid.NewGuid().ToString()};
				db.Insert(user);

				var newName = Guid.NewGuid().ToString();
				user.UserName = newName;
				db.Update(user);

				Assert.Null(db.Users.SingleOrDefault(u => u.UserName == oldName));
				Assert.AreEqual(user, db.Users.Single(u => u.UserName == newName));
			}
		}

		[Test]
		public void CanCreateUserInt()
		{
			using (var db = CreateContext<int>(true))
			{
				var user = new User<int>();
				db.Insert(user);

				user.UserName = "Boo";
				db.Update(user);

				var fetch = db.Users.First(u => u.UserName == "Boo");
				Assert.AreEqual(user, fetch);
			}
		}

		[Test]
		public void CanUpdateNameInt()
		{
			using (var db = CreateContext<int>(true))
			{
				var oldName = Guid.NewGuid().ToString();
				var user = new User<int> {UserName = oldName};
				db.Insert(user);

				var newName = Guid.NewGuid().ToString();
				user.UserName = newName;
				db.Update(user);

				Assert.Null(db.Users.SingleOrDefault(u => u.UserName == oldName));
				Assert.AreEqual(user, db.Users.Single(u => u.UserName == newName));
			}
		}

		public class User<TKey> where TKey : IEquatable<TKey>
		{
			[PrimaryKey]
			[Column(Length = 255, CanBeNull = false)]
			public TKey Id { get; set; } = default!;

			public string UserName { get; set; } = default!;

			public override bool Equals(object obj)
			{
				if (!(obj is User<TKey> other))
					return false;

				return Id.Equals(other.Id) && UserName == other.UserName;
			}

			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}
		}

		public class CustomDbContext<TKey> : DataConnection where TKey : IEquatable<TKey>
		{
			public CustomDbContext(IDataProvider dataProvider, string connectionString)
				: base(dataProvider, connectionString)
			{
			}

			public ITable<User<TKey>> Users => GetTable<User<TKey>>();
		}
	}
}
