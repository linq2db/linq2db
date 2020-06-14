﻿using System;
using System.Linq;

using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using NUnit.Framework;

namespace Tests.UserTests
{
	using Model;

	[TestFixture]
	public class GroupBySubqueryTests : TestBase
	{
		class Table1
		{
			public long Field1 { get; set; }
			public int  Field2 { get; set; }

			[Nullable]
			public int? Field3 { get; set; }

			[Association(ThisKey = "Field1", OtherKey = "Field1", CanBeNull = false)]
			public Table3 Ref1 { get; set; } = null!;

			[Association(ThisKey = "Field3", OtherKey = "Field3", CanBeNull = true)]
			public Table5? Ref2 { get; set; }

			[Association(ThisKey = "Field2", OtherKey = "Field2", CanBeNull = true)]
			public Table2? Ref3 { get; set; }
		}

		class Table2
		{
			public int     Field2 { get; set; }
			public string? Field4 { get; set; }
		}

		class Table3
		{
			public int  Field5 { get; set; }
			public long Field1 { get; set; }

			[Association(ThisKey = "Field5", OtherKey = "Field5", CanBeNull = false)]
			public Table4 Ref4 { get; set; } = null!;
		}

		class Table4
		{
			public int Field5 { get; set; }
			public int Field6 { get; set; }
		}

		public class Table5
		{
			[Nullable]
			public int? Field3 { get; set; }
			public int  Field7 { get; set; }

			[Association(ThisKey = "Field7", OtherKey = "Field7", CanBeNull = true)]
			public Table6? Ref5 { get; set; }
		}

		public class Table6
		{
			public int     Field7 { get; set; }
			public string? Field8 { get; set; }
		}

		[Test]
		public void Test()
		{
			using (var db = new TestDataConnection())
			{
				var q1 = (
					from t1 in db.GetTable<Table1>()
					where t1.Field3 != null
					select new
					{
						t1.Ref1.Ref4.Field6, 
						t1.Ref3!.Field4,
						Field1 = t1.Ref2!.Ref5!.Field8 ?? string.Empty
					}
				).Distinct();

				var sql1 = q1.GetSelectQuery();
				Assert.That(sql1.Select.IsDistinct, "Distinct not present");

				var q2 =
					from t3 in q1
					group t3 by new { t3.Field6, t3.Field4 }
					into g
					where g.Count() > 1
					select new { g.Key.Field6, EngineeringCircuitNumber = g.Key.Field4, Count = g.Count() };

				var distinct = q2.EnumQueries().FirstOrDefault(q => q.Select.IsDistinct);

				Assert.That(distinct, Is.Not.Null);
				Assert.That(distinct.Select.Columns.Count, Is.EqualTo(3));
			}
		}
	}
}
