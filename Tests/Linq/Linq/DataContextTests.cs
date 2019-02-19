using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class DataContextTests : TestBase
	{
		[Test]
		public void TestContext([IncludeDataSources(
			ProviderName.SqlServer2008, ProviderName.SqlServer2012,
			ProviderName.SqlServer2014, ProviderName.SapHana)]
			string context)
		{
			var ctx = new DataContext(context);

			ctx.GetTable<Person>().ToList();

			ctx.KeepConnectionAlive = true;

			ctx.GetTable<Person>().ToList();
			ctx.GetTable<Person>().ToList();

			ctx.KeepConnectionAlive = false;

			using (var tran = new DataContextTransaction(ctx))
			{
				ctx.GetTable<Person>().ToList();

				tran.BeginTransaction();

				ctx.GetTable<Person>().ToList();
				ctx.GetTable<Person>().ToList();

				tran.CommitTransaction();
			}
		}

		[Test]
		public void TestContextToString([IncludeDataSources(
			ProviderName.SqlServer2008, ProviderName.SqlServer2012,
			ProviderName.SqlServer2014, ProviderName.SapHana)]
			string context)
		{
			using (var ctx = new DataContext(context))
			{
				Console.WriteLine(ctx.GetTable<Person>().ToString());

				var q =
					from s in ctx.GetTable<Person>()
					select s.FirstName;

				Console.WriteLine(q.ToString());
			}
		}

		[Test]
		public void Issue210([IncludeDataSources(
			ProviderName.SqlServer2008, ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			using (var ctx = new DataContext(context))
			{
				ctx.KeepConnectionAlive = true;
				ctx.KeepConnectionAlive = false;
			}
		}

		[Test]
		public void issue1513Test([DataSources(false)]string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
				var _db = new DataContext(db.DataProvider.Name, db.ConnectionString);
				var _db1 = new DataContext(db.DataProvider.Name, "FakeConnectionString");

				Assert.AreEqual(db.DataProvider.Name, _db.DataProvider.Name);
				Assert.AreEqual(db.ConnectionString, _db.ConnectionString);
				Assert.Throws(typeof(LinqToDBException), () => new DataContext("FakeAdapter", db.ConnectionString));
				Assert.Throws(typeof(System.ArgumentException), () => _db1.GetTable<Child>().FirstOrDefault());
			}
		}
	}
}
