using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5616Tests : TestBase
	{
		sealed class Table1
		{
			[PrimaryKey] public int Id { get; set; }

			public static readonly Table1[] Data =
			{
				new() { Id = 1 },
				new() { Id = 2 },
				new() { Id = 3 },
			};
		}

		sealed class Table2
		{
			[PrimaryKey] public int Id { get; set; }

			public static readonly Table2[] Data =
			{
				new() { Id = 10 },
				new() { Id = 20 },
				new() { Id = 30 },
			};
		}

		[Sql.Extension("count_if({predicate})", IsAggregate = true, ServerSideOnly = true)]
		public static long CountIf<TSource>(IEnumerable<TSource> src, [ExprParameter] Expression<Func<TSource, bool>> predicate)
		{
			throw new InvalidOperationException();
		}

		// A built-in aggregate (Average) in one UNION ALL branch, a constant in the other.
		// Issue #5616: building the query threw InvalidCastException
		// (SqlPathExpression -> SqlPlaceholderExpression) in VisitSqlReaderIsNullExpression.
		[Test]
		public void Average_InUnionAll([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var t1 = db.CreateLocalTable(Table1.Data);
			using var t2 = db.CreateLocalTable(Table2.Data);

			var aggregates =
				from tr in t2
				group tr by tr.Id into g
				select new
				{
					Count = g.Average(x => x.Id, Sql.AggregateModifier.None)
				};

			var query = t1
				.Select(_ => new { Count = 0d })
				.UnionAll(aggregates);

			var result = query.ToList();

			// constant branch: one row per Table1 row; aggregate branch: one row per Table2 group (grouped by PK).
			result.Count.ShouldBe(Table1.Data.Length + Table2.Data.Length);
			result.Count(x => x.Count == 0d).ShouldBe(Table1.Data.Length);
		}

		// Same shape with the custom IsAggregate extension from the issue. count_if is not executable
		// on the default providers, so this asserts the query builds (the reported failure was at build time).
		[Test]
		public void CustomAggregate_InUnionAll([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var aggregates =
				from tr in db.GetTable<Table2>()
				group tr by tr.Id into g
				select new
				{
					Count = CountIf(g, x => x.Id > 123)
				};

			var query = db.GetTable<Table1>()
				.Select(_ => new { Count = 0L })
				.UnionAll(aggregates);

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("count_if");
		}
	}
}
