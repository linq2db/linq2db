using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class GroupBySubqueryTests : TestBase
	{
		sealed class Table1
		{
			[PrimaryKey] public int Field1 { get; set; }
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

		sealed class Table2
		{
			[PrimaryKey] public int     Field2 { get; set; }
			public string? Field4 { get; set; }
		}

		sealed class Table3
		{
			[PrimaryKey] public int  Field5 { get; set; }
			public int Field1 { get; set; }

			[Association(ThisKey = "Field5", OtherKey = "Field5", CanBeNull = false)]
			public Table4 Ref4 { get; set; } = null!;
		}

		sealed class Table4
		{
			[PrimaryKey] public int Field5 { get; set; }
			public int Field6 { get; set; }
		}

		public class Table5
		{
			[Nullable]
			public int? Field3 { get; set; }
			[PrimaryKey] public int  Field7 { get; set; }

			[Association(ThisKey = "Field7", OtherKey = "Field7", CanBeNull = true)]
			public Table6? Ref5 { get; set; }
		}

		public class Table6
		{
			[PrimaryKey] public int     Field7 { get; set; }
			public string? Field8 { get; set; }
		}

		[Test]
		public void Test([DataSources(ProviderName.Ydb)] string context)
		{
			using var db = GetDataContext(context, o => o.OmitUnsupportedCompareNulls(context));
			using var t8 = db.CreateLocalTable<Table1>();
			using var t2 = db.CreateLocalTable<Table2>();
			using var t7 = db.CreateLocalTable<Table3>();
			using var t4 = db.CreateLocalTable<Table4>();
			using var t5 = db.CreateLocalTable<Table5>();
			using var t6 = db.CreateLocalTable<Table6>();

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

			_ = q1.ToArray();

			var sql1 = q1.GetSelectQuery();

			Assert.That(sql1.Select.IsDistinct, "Distinct not present");

			var q2 =
					from t3 in q1
					group t3 by new { t3.Field6, t3.Field4 }
					into g
					where g.Count() > 1
					select new { g.Key.Field6, EngineeringCircuitNumber = g.Key.Field4, Count = g.Count() };

			_ = q2.ToArray();

			var sql2 = q2.GetSelectQuery();

			var distinct = q2.EnumQueries().FirstOrDefault(q => q.Select.IsDistinct)!;

			Assert.That(distinct, Is.Not.Null);
			Assert.That(distinct.Select.Columns, Has.Count.EqualTo(3));
		}
	}
}
