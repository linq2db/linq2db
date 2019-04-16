using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Mapping;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

namespace Tests.Android
{
	[TestFixture]
	public class TestClass
	{
		[Table]
		class Test
		{
			[Column]
			public int Field { get; set; }
		}
		[Test]
		public void TestSqlLiteMS()
		{
			var provider = new SQLiteDataProvider(ProviderName.SQLiteMS);

			try
			{
				using (var db = new DataConnection(provider, "Data Source=" + MainActivity.SQLiteDbPath))
				{
					db.CreateTable<Test>();

					db.Insert(new Test() { Field = 5 });

					var results = db.GetTable<Test>().ToArray();

					Assert.AreEqual(1, results.Length);
					Assert.AreEqual(5, results[0].Field);
				}

			}
			finally
			{
				SQLiteTools.DropDatabase("test_db");
			}
		}
	}
}
