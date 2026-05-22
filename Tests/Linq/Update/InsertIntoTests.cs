using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	[TestFixture]
	public class InsertIntoTests : TestBase
	{
		[Table]
		sealed class InsertTestClass
		{
			[Column(IsIdentity = true)] public int  Id         { get; set; }
			[Column]                    public int  Value      { get; set; }
			[Column(CanBeNull = true)]  public int? OtherValue { get; set; }

			public static InsertTestClass[] Seed()
			{
				return new[] {new InsertTestClass {Value = 1, OtherValue = 100}};
			}
		}

		[Test]
		public void InsertFromQuery([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(InsertTestClass.Seed());
			using var destTable = db.CreateLocalTable<InsertTestClass>("InsertTestClassDest");
			table.Select(x => new InsertTestClass() { Id = x.Id + 1, Value = x.Value, OtherValue = x.OtherValue })
				.Into(destTable)
				.Insert();

			var source = table.Single();
			var result = destTable.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(source.Value, Is.EqualTo(result.Value));
				Assert.That(source.OtherValue, Is.EqualTo(result.OtherValue));
			}
		}

		[Test]
		public void InsertFromTable([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(InsertTestClass.Seed());
			using var destTable = db.CreateLocalTable<InsertTestClass>("InsertTestClassDest");
			table
				.Into(destTable)
				.Insert();

			var source = table.Single();
			var result = destTable.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(source.Value, Is.EqualTo(result.Value));
				Assert.That(source.OtherValue, Is.EqualTo(result.OtherValue));
			}
		}

		[Test]
		public void InsertFromCTE([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(InsertTestClass.Seed());
			using var destTable = db.CreateLocalTable<InsertTestClass>("InsertTestClassDest");
			table.AsCte()
				.Into(destTable)
				.Insert();

			var source = table.Single();
			var result = destTable.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(source.Value, Is.EqualTo(result.Value));
				Assert.That(source.OtherValue, Is.EqualTo(result.OtherValue));
			}
		}

		[Test]
		public void InsertFromSql([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(InsertTestClass.Seed());
			using var destTable = db.CreateLocalTable<InsertTestClass>("InsertTestClassDest");
			db.FromSql<InsertTestClass>("select * from InsertTestClass")
				.Select(x => new InsertTestClass() { Id = x.Id + 1, Value = x.Value })
				.Into(destTable)
				.Insert();

			var source = table.Single();
			var result = destTable.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(source.Value, Is.EqualTo(result.Value));
				Assert.That(result.OtherValue, Is.Null);
			}
		}

		[Test]
		public void InsertFromTableOverride([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable(InsertTestClass.Seed());
			using var destTable = db.CreateLocalTable<InsertTestClass>("InsertTestClassDest");
			table
				.Into(destTable)
				.Value(x => x.OtherValue, x => x.OtherValue + 1)
				.Value(x => x.Value, x => x.Value + 1)
				.Value(x => x.Value, x => x.Value + 2)
				.Value(x => x.OtherValue, x => x.OtherValue + 2)
				.Insert();

			var source = table.Single();
			var result = destTable.Single();
			using (Assert.EnterMultipleScope())
			{
				Assert.That(source.Value, Is.EqualTo(result.Value - 2));
				Assert.That(source.OtherValue, Is.EqualTo(result.OtherValue - 2));
			}
		}
	}
}
