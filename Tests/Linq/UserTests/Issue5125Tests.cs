using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5125Tests : TestBase
	{
		// Faithful repro of issue #5125 (https://github.com/linq2db/linq2db/issues/5125):
		// non-nullable `string` properties with [AllowNull] + [ExpressionMethod] containing
		// `string.IsNullOrEmpty`, projected to the entity itself via `new T { ... }` after
		// OrderBy on a calculated expression. Confirmed reproducible on v6.0.0-rc.3 with
		// `42P01: missing FROM-clause entry for table "x_1"` on PostgreSQL; this test pins
		// the fix from PR #5126 and guards against regression.
		[Table("InterpolatedTest5125", IsColumnAttributeRequired = false)]
		public class InterpolatedTest5125
		{
			public int     Id     { get; set; }
			public string? StrVal { get; set; }
			public int     IntVal { get; set; }

			[System.Diagnostics.CodeAnalysis.AllowNull]
			[ExpressionMethod(nameof(Expr1Impl))]
			public string Expr1 { get; set; } = null!;

			private static Expression<Func<InterpolatedTest5125, IDataContext, string>> Expr1Impl() =>
				(e, ctx) => !string.IsNullOrEmpty(e.StrVal) ? e.StrVal : e.IntVal.ToString();

			[System.Diagnostics.CodeAnalysis.AllowNull]
			[ExpressionMethod(nameof(Expr2Impl))]
			public string Expr2 { get; set; } = null!;

			private static Expression<Func<InterpolatedTest5125, IDataContext, string>> Expr2Impl() =>
				(e, ctx) => $"{(!string.IsNullOrEmpty(e.StrVal) ? e.StrVal : e.IntVal.ToString())}";
		}

		[Test]
		public void TestIssue5125_Repro([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			var data = new[]
			{
				new InterpolatedTest5125 { Id = 1, StrVal = null,     IntVal = 11 },
				new InterpolatedTest5125 { Id = 2, StrVal = "",       IntVal = 12 },
				new InterpolatedTest5125 { Id = 3, StrVal = "Value3", IntVal = 13 },
			};

			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(data);

			var query = tb
				.OrderBy(x => x.Expr1)
				.Select(x => new InterpolatedTest5125
				{
					Id    = x.Id,
					Expr1 = x.Expr1,
					Expr2 = x.Expr2
				});
			var list = query.ToList();

			Assert.That(list, Has.Count.EqualTo(3));
		}
	}
}
