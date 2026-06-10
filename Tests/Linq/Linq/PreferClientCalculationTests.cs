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
			[Column] public int     Id     { get; set; }
			[Column] public int     Value1 { get; set; }
			[Column] public int     Value2 { get; set; }
			[Column] public string? Name   { get; set; }

			public static readonly ClientCalcEntity[] Seed =
			[
				new() { Id = 1, Value1 = 10, Value2 = 100, Name = "Alpha" },
				new() { Id = 2, Value1 = 20, Value2 = 200, Name = "Beta"  },
				new() { Id = 3, Value1 = 30, Value2 = 300, Name = "Gamma" },
			];
		}

		// Two functions mapped to SQL ABS (which has a client implementation via Math.Abs) to exercise the
		// PreferServerSide gate precisely: PreferServer stays server-side even when client calculation is
		// preferred; PreferClient is allowed to move client-side.
		[Sql.Function("ABS", PreferServerSide = true )] static int PreferServer(int value) => Math.Abs(value);
		[Sql.Function("ABS", PreferServerSide = false)] static int PreferClient(int value) => Math.Abs(value);

		[Test]
		public void BinaryArithmeticProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select new { e.Id, Calc = e.Value1 + 12345 };

			// Client calculation preferred => every projected column is a raw field; otherwise "Value1 + 12345"
			// is pushed down as a computed SqlBinaryExpression column.
			query.GetSelectQuery().Select.Columns.All(c => c.Expression is SqlField).ShouldBe(preferClient);

			AssertQuery(query);
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

			var selectQuery = query.GetSelectQuery();

			// Client calculation preferred => the nested ternary and the branch arithmetic stay client-side, so no
			// CASE/condition node is emitted and every projected column is a raw field. Otherwise both ternaries
			// become CASE columns.
			(selectQuery.Find(e => e is SqlConditionExpression or SqlCaseExpression) == null).ShouldBe(preferClient);
			selectQuery.Select.Columns.All(c => c.Expression is SqlField).ShouldBe(preferClient);

			AssertQuery(query);
		}

		[Test]
		public void UnaryNegationProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select -e.Value1;

			// The projected column is the raw field when client calculation is preferred, or a computed
			// (negation) expression otherwise.
			query.GetSelectQuery().Select.Columns.All(c => c.Expression is SqlField).ShouldBe(preferClient);

			AssertQuery(query);
		}

		[Test]
		public void ServerSidePreferredFunctionStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select PreferServer(e.Value1);

			// PreferServerSide = true keeps the function in SQL even when client calculation is preferred.
			(query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }) != null).ShouldBeTrue();

			AssertQuery(query);
		}

		[Test]
		public void ClientSideAllowedFunctionFollowsOption([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select PreferClient(e.Value1);

			// PreferServerSide = false: the function moves client-side when client calculation is preferred.
			(query.GetSelectQuery().Find(e => e is SqlFunction { Name: "ABS" }) == null).ShouldBe(preferClient);

			AssertQuery(query);
		}

		[Test]
		public void MixedServerAndClientExpression([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				select e.Value1 + PreferServer(e.Value2);

			var selectQuery = query.GetSelectQuery();

			// The server-preferring leaf (ABS) stays in SQL regardless of the option...
			(selectQuery.Find(e => e is SqlFunction { Name: "ABS" }) != null).ShouldBeTrue();
			// ...while the surrounding "+" is pushed down only when client calculation is NOT preferred.
			(selectQuery.Find(e => e is SqlBinaryExpression) != null).ShouldBe(!preferClient);

			AssertQuery(query);
		}

		[Test]
		public void SetProjectionStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				(from e in table select new { C = e.Value1 + 7777 })
				.Concat(from e in table select new { C = e.Value2 + 7777 });

			// Set projections require column alignment, so the arithmetic stays in SQL even with the option on.
			(query.GetSelectQuery().Find(e => e is SqlBinaryExpression) != null).ShouldBeTrue();

			AssertQuery(query);
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
	}
}
