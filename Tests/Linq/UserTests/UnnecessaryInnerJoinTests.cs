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
			public long Field1 { get; set; }
			public long Field2 { get; set; }
		}

		sealed class Table2
		{
			[PrimaryKey(1)]
			[Identity]
			public long Field2 { get; set; }

			[Association(ThisKey = "Field2", OtherKey = "Field2", CanBeNull = false)]
			public List<Table1> Field3 { get; set; } = null!;
		}

		[Test]
		public void Test([DataSources] string context)
		{
			var ids = new long[] { 1, 2, 3 };

			using var db = GetDataContext(context);
			var q =
				from t1 in db.GetTable<Table2>()
				where t1.Field3.Any(x => ids.Contains(x.Field1))
				select new { t1.Field2 };

			var sql = q.ToSqlQuery().Sql;

			BaselinesManager.LogQuery(sql);

			Assert.That(sql, Does.Not.Contain("INNER JOIN"));
		}
	}
}
