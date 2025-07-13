using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Internal.Common;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class ConditionalTests : TestBase
	{
		class ConditionalData
		{
			[PrimaryKey]
			public int Id { get; set; }

			[Column]
			public string? StringProp { get; set; }

			public static ConditionalData[] Seed()
			{
				return Enumerable.Range(1, 10)
					.Select(x => new ConditionalData { Id = x, StringProp = x % 3 == 0 ? null : "String" + x })
					.Concat([new ConditionalData { Id = 11, StringProp = "-1" }])
					.ToArray();
			}
		}

		class TestChildClass
		{
			public int?    IntProp    { get; set; }
			public string? StringProp { get; set; }
		}

		[Test]
		public void ViaConditionWithNull1([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new { Id = p.Id, child = p.StringProp == "1" ? null : new TestChildClass { StringProp = p.StringProp } };

				query = query.Where(x => x.child.StringProp!.Contains("2"));

				AssertQuery(query);
			}
		}

		[Test]
		public void ViaConditionWithNull2([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new { Id = p.Id, child = p.StringProp == "1" ? new TestChildClass { StringProp = p.StringProp } : null };

				query = query.Where(x => x.child.StringProp!.Contains("2"));

				AssertQuery(query);
			}
		}

		[Test]
		public void ViaCondition([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id = p.Id,
						child = p.StringProp == "1"
							? new TestChildClass { StringProp = "2" }
							: new TestChildClass { StringProp = p.StringProp }
					};

				query = query.Where(x => x.child.StringProp!.Contains("2"));

				AssertQuery(query);
			}
		}

		[Test]
		public void ViaConditionDeep([DataSources(false)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id = p.Id,
						child = p.StringProp == "1" || p.StringProp == null ? new TestChildClass { StringProp = "2" }
							: p.StringProp == "2" ? new TestChildClass { StringProp                           = p.StringProp, IntProp       = 1 }
							: new TestChildClass { StringProp                                                 = p.StringProp + "2", IntProp = 2 }
					};

				query = query.Where(x => x.child.StringProp!.EndsWith("2") && x.child.IntProp == 2);

				AssertQuery(query);
			}
		}

		[Test]
		public void ViaConditionDeepFail([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id = p.Id,
						child = p.StringProp == "1" ? new TestChildClass { StringProp = "2" }
							: p.StringProp   == "2" ? new TestChildClass { StringProp = p.StringProp }
							: new TestChildClass { StringProp                         = p.StringProp + "2" }
					};

				query = query.Where(m => m.child.StringProp!.Contains("2") && m.child.IntProp == 1);

				var act = () => query.ToArray();
				act.ShouldThrow<LinqToDBException>().Message.ShouldContain("m.child.IntProp");
			}
		}

		[Test]
		public void ViaConditionNull([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					from p2 in table.Where(p2 => p2.StringProp != null && p2.Id == p.Id).DefaultIfEmpty()
					select new
					{
						Id = p.Id,
						child = p2 == null
							? new TestChildClass { StringProp = "-1" }
							: new TestChildClass { StringProp = p2.StringProp }
					};

				query = query.Where(x => x.child.StringProp == "-1");

				AssertQuery(query);
			}
		}

		[Test]
		public void NestedProperties([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var data = ConditionalData.Seed();

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(data))
			{
				var query =
					from p in table
					select new
					{
						Id = p.Id,
						Sub = p == null
							? new { Prop = new { V = "-1" } }
							: new { Prop = p.StringProp!.Contains("1") ? new { V = "1" } : new { V = "2" } }
					};

				query = query.Where(x => x.Sub.Prop.V == "-1");

				AssertQuery(query);
			}
		}

		[Test]
		[ThrowsForProvider(typeof(LinqToDBException), TestProvName.AllAccess, TestProvName.AllSybase, ErrorMessage = ErrorHelper.Error_OUTER_Joins)]
		public void CrossToOuterApply([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(p => p.Children)
				select p.ParentID == 2
					// this must be promoted to outer join
					? p.Children.OrderBy(c => c.ChildID).First()
					: p.Children.OrderBy(c => c.ChildID).FirstOrDefault();

			AssertQuery(query);
		}

		[ExpressionMethod(nameof(DivideExpr))]
		static double Divide(double value, double divisor) => divisor == 0 ? value : value / divisor;

		static Expression<Func<double, double, double>> DivideExpr() => (value, divisor) => divisor == 0 ? value : value / divisor;

		[Test]
		public void Conditional_DoNotEvalueBothBranches([DataSources] string context)
		{
			using var db = GetDataContext(context);

			// Sql.AsSql to force ExpressionMethod translation
			var result = db.Select(() => Divide(Sql.AsSql(20D), Sql.AsSql(0D)));

			Assert.That(result, Is.EqualTo(20D));
		}

		[Test]
		public void ConditionInsideCondition_BoolWrapping([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(p => p.Children)
				select new
				{
					Value = (p.ParentID % 2 == 0)
						? (p.ParentID   % 3 == 0)
						: (p.ParentID   % 4 == 0)
							? (p.ParentID   > 0)
							: (p.ParentID   < 5)
				};

			AssertQuery(query);
		}

		[Test]
		public void ConditionInsideCondition_BoolWrappingFilter([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Parent.LoadWith(p => p.Children)
				select new
				{
					Value = (p.ParentID % 2 == 0)
						? (p.ParentID   % 3 == 0)
						: (p.ParentID   % 4 == 0)
							? (p.ParentID   > 0)
							: (p.ParentID   < 5)
				}
				into s
				where s.Value
				select s;

			AssertQuery(query);
		}

	}
}
