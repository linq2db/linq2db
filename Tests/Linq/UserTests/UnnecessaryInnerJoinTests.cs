using System;
using System.Collections.Generic;
using System.Linq;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class UnnecessaryInnerJoinTests : TestBase
	{
		sealed class Table1
		{
			[PrimaryKey(1)]
			[Identity]
			public int Field1 { get; set; }
			public int Field2 { get; set; }
		}

		sealed class Table2
		{
			[PrimaryKey(1)]
			[Identity]
			public int Field2 { get; set; }

			[Association(ThisKey = "Field2", OtherKey = "Field2", CanBeNull = false)]
			public List<Table1> Field3 { get; set; } = null!;
		}

		[Test]
		public void Test([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var tb1 = db.CreateLocalTable<Table1>();
			using var tb2 = db.CreateLocalTable<Table2>();

			var ids = new long[] { 1, 2, 3 };

			var q =
				from t1 in db.GetTable<Table2>()
				where t1.Field3.Any(x => ids.Contains(x.Field1))
				select new { t1.Field2 };

			var sql = q.ToSqlQuery().Sql;

			Assert.That(sql, Does.Not.Contain("INNER JOIN"));

			q.ToArray();
		}
	}
}
