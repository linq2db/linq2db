using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class PreferClientCalculationTests : TestBase
	{
		[Table]
		sealed class ClientCalcEntity
		{
			[Column, PrimaryKey] public int     Id     { get; set; }
			[Column]             public int     Value1 { get; set; }
			[Column]             public int     Value2 { get; set; }
			[Column]             public string? Name   { get; set; }

			public static readonly ClientCalcEntity[] Seed =
			[
				new() { Id = 1, Value1 = 10, Value2 = 100, Name = "Alpha" },
				new() { Id = 2, Value1 = 20, Value2 = 200, Name = "Beta"  },
				new() { Id = 3, Value1 = 30, Value2 = 300, Name = "Gamma" },
			];
		}

		// Mapped SQL functions (ABS). PreferServerSide controls whether the function stays server-side:
		// PreferServerSide = true keeps it in SQL even when client calculation is preferred; PreferServerSide = false
		// lets it move client-side when client calculation is preferred.
		[Sql.Function("ABS", PreferServerSide = true )] static int PreferServer(int value) => Math.Abs(value);
		[Sql.Function("ABS", PreferServerSide = false)] static int PreferClient(int value) => Math.Abs(value);
		[Sql.Function("ABS", ServerSideOnly   = true )] static int ServerOnly  (int value) => throw new InvalidOperationException();

		// No SQL mapping — forces client-side evaluation, used to exercise the Sql.ToNullable translator's
		// "argument can't be turned into SQL" fall-through (it returns null and the call stays client-side).
		static int ClientOnlyDouble(int value) => value * 2;

		[Test]
		public void BinaryArithmeticProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select new { e.Id, Calc = e.Value1 + 12345 };

			AssertQuery(query);

			// Client calculation preferred => every projected column is a raw field; otherwise "Value1 + 12345"
			// is pushed down as a computed SqlBinaryExpression column.
			query.GetSelectQuery().Select.Columns.All(c => c.Expression is SqlField).ShouldBe(preferClient);
		}

		[Test]
		public void NestedConditionalProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select new
				{
					e.Id,
					Bucket = e.Value1 > 15 ? (e.Value2 > 150 ? "high" : "mid") : "low",
					Score  = e.Id > 1 ? e.Value1 + e.Value2 : e.Value1 - e.Value2,
				};

			AssertQuery(query);

			var selectQuery = query.GetSelectQuery();

			// Client calculation preferred => the nested ternary and the branch arithmetic stay client-side, so no
			// CASE/condition node is emitted and every projected column is a raw field. Otherwise both ternaries
			// become CASE columns.
			(selectQuery.Find(e => e is SqlConditionExpression or SqlCaseExpression) == null).ShouldBe(preferClient);
			selectQuery.Select.Columns.All(c => c.Expression is SqlField).ShouldBe(preferClient);
		}

		[Test]
		public void UnaryNegationProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select -e.Value1;

			AssertQuery(query);

			// The projected column is the raw field when client calculation is preferred, or a computed
			// (negation) expression otherwise.
			query.GetSelectQuery().Select.Columns.All(c => c.Expression is SqlField).ShouldBe(preferClient);
		}

		[Test]
		public void ServerSidePreferredFunctionStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select PreferServer(e.Value1);

			AssertQuery(query);

			// PreferServerSide = true keeps the function in SQL even when client calculation is preferred.
			(query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }) != null).ShouldBeTrue();
		}

		[Test]
		public void NonServerPreferredFunctionMovesClientSide([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select PreferClient(e.Value1);

			AssertQuery(query);

			// PreferServerSide = false: the function moves client-side when client calculation is preferred.
			(query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }) == null).ShouldBe(preferClient);
		}

		[Test]
		public void ToNullableOverMissingLeftJoinReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// The left join never matches, so every joined row is absent and Sql.ToNullable(<joined column>) must be NULL.
			// Regression: ToNullable is a server-side nullability widener and must stay server-side even when client
			// calculation is preferred — otherwise the missing-row NULL collapses to default(int) at the client read
			// and surfaces as 0 instead of null. The generated SQL is identical either way (it selects the raw column),
			// so this can only be caught on the materialized value, not the SQL AST.
			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Joined = Sql.ToNullable(j.Value1) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void ToNullableOverNonSqlArgumentEvaluatesClientSide([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// ClientOnlyDouble has no SQL mapping, so ToNullable's argument can't be converted to SQL. The translator
			// must decline (return null) and let the whole expression evaluate client-side — it must not error, and
			// must still produce the correctly-widened value. (Without the fall-through this query would fail to build.)
			var query =
				from e in table
				select new { e.Id, Doubled = Sql.ToNullable(ClientOnlyDouble(e.Value1)) };

			AssertQuery(query);
		}

		[Test]
		public void AsNullableOverMissingLeftJoinReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// Faithful analog of the ToNullable bug: AsNullable<T>(T) takes a NON-nullable argument (int column) and the
			// nullability is applied OUTSIDE the call via (int?). The left join never matches, so the value must read NULL.
			// If AsNullable is pulled client-side under PreferClientCalculation and reads its int argument non-nullably,
			// the missing-row NULL collapses to default(int)=0 and the (int?) cast yields 0 instead of null.
			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Joined = (int?)Sql.AsNullable(j.Value1) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void GroupByAggregateStaysServerSide([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				group e by e.Name into g
				select new { g.Key, Sum = g.Sum(x => x.Value1) };

			// Grouping key + aggregate must stay server-side even with the option on (otherwise the GroupBy guard trips).
			AssertQuery(query);
		}

		[Test]
		public void ServerSideOnlyInsideConditionalStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select e.Id > 1 ? ServerOnly(e.Value1) : e.Value2;

			// A server-side-only API inside a conditional must stay in SQL even when client calculation is
			// preferred (the IsServerSideOnly gate) — otherwise it would be illegally evaluated on the client.
			query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }).ShouldNotBeNull();

			_ = query.ToArray(); // must not throw
		}

		[Test]
		public void MixedServerAndClientExpression([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select e.Value1 + PreferServer(e.Value2);

			AssertQuery(query);

			var selectQuery = query.GetSelectQuery();

			// The server-preferring leaf (ABS) stays in SQL regardless of the option...
			(selectQuery.Find(e => e is SqlFunction { Name: "ABS" }) != null).ShouldBeTrue();
			// ...while the surrounding "+" is pushed down only when client calculation is NOT preferred.
			(selectQuery.Find(e => e is SqlBinaryExpression) != null).ShouldBe(!preferClient);
		}

		[Test]
		public void SetProjectionStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				(from e in table select new { C = e.Value1 + 7777 })
				.Concat(from e in table select new { C = e.Value2 + 7777 });

			AssertQuery(query);

			// Set projections require column alignment, so the arithmetic stays in SQL even with the option on.
			(query.GetSelectQuery().Find(e => e is SqlBinaryExpression) != null).ShouldBeTrue();
		}

		[Test]
		public void ProjectionResultsMatchAcrossProviders([DataSources] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// Pure correctness sweep across every provider (including remote): the result must match client-side
			// evaluation no matter where the computation happens.
			AssertQuery(from e in table select new { e.Id, Calc = e.Value1 + 12345 });
			AssertQuery(from e in table select e.Id > 1 ? e.Value1 : e.Value2);
			AssertQuery(from e in table select -e.Value1);
			AssertQuery(from e in table select e.Value1 + PreferServer(e.Value2));
		}

		// The following tests document a rule that is independent of PreferClientCalculation: in non-projection clauses
		// (WHERE / JOIN / GROUP BY / ORDER BY / HAVING) linq2db already pre-evaluates any SQL-independent (row-data-free)
		// client expression into a SQL parameter/constant, leaves row-dependent SQL parts as columns, throws for a
		// row-dependent client-only expression with no SQL mapping, and keeps server-preferred functions server-side.
		// Each is parameterized by preferClient to assert the option does not change this behaviour.

		[Test]
		public void ClientConstantInPredicateIsServerSide([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// ClientOnlyDouble(1) has no SQL mapping but is SQL-independent, so it is pre-evaluated into a parameter and
			// the comparison runs server-side. ClientOnlyDouble(1) == 2, so this matches Id == 2.
			AssertQuery(table.Where(e => e.Id == ClientOnlyDouble(1)));
		}

		[Test]
		public void RowDependentClientOnlyInPredicateThrows([IncludeDataSources(false, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// ClientOnlyDouble(e.Value1) depends on row data and has no SQL mapping: it cannot be translated and must
			// throw, never silently becoming a client-side post-filter - even with PreferClientCalculation on.
			Assert.Throws<LinqToDBException>(() => table.Where(e => ClientOnlyDouble(e.Value1) == 20).ToArray());
		}

		[Test]
		public void MixedClientConstantAndColumnInPredicate([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// Granular split: the SQL-independent ClientOnlyDouble(1) folds to a parameter while e.Value1 stays a
			// column (WHERE Id = @p + Value1). No row matches, but the query must build and run server-side.
			AssertQuery(table.Where(e => e.Id == ClientOnlyDouble(1) + e.Value1));
		}

		[Test]
		public void ServerPreferredFunctionInPredicateStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query = table.Where(e => PreferServer(e.Value1) == 20);

			AssertQuery(query);

			// PreferServerSide = true keeps the function server-side: ABS stays in the WHERE clause.
			(query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }) != null).ShouldBeTrue();
		}

		[Test]
		public void MappedClientPreferredFunctionInPredicateIsPreEvaluated([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query = table.Where(e => e.Id == PreferClient(10));

			AssertQuery(query);

			// PreferClient(10) (ABS, PreferServerSide = false) is SQL-independent, so it is pre-evaluated to a constant
			// instead of being emitted as ABS - the comparison becomes Id == 10.
			(query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }) == null).ShouldBeTrue();
		}

		[Test]
		public void PreferClientFunctionWithRowArgInPredicateStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query = table.Where(e => PreferClient(e.Value1) == 20);

			AssertQuery(query);

			// PreferServerSide = false, but PreferClientCalculation is projection-only: in a predicate a row-dependent
			// mapped function still translates to SQL (ABS), regardless of the option.
			(query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }) != null).ShouldBeTrue();
		}

		[Test]
		public void ClientConstantFoldsInOrderByGroupByHavingAndJoin([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// The SQL-independent ClientOnlyDouble(...) is pre-evaluated server-side in every clause, not just projections.
			AssertQuery(table.OrderBy(e => ClientOnlyDouble(1)).ThenBy(e => e.Id));                                            // ORDER BY
			AssertQuery(table.GroupBy(e => ClientOnlyDouble(1)).Select(g => g.Count()));                                       // GROUP BY
			AssertQuery(table.GroupBy(e => e.Id).Where(g => g.Count() > ClientOnlyDouble(0)).Select(g => g.Key));              // HAVING
			AssertQuery(from e in table join j in table on e.Id + ClientOnlyDouble(0) equals j.Id select new { e.Id, J = j.Id }); // JOIN
		}
	}
}
