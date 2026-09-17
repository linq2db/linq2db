using System;
using System.Linq;
using System.Linq.Expressions;

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
		[Sql.Function("ABS", ServerSideOnly   = true )] static int ServerOnly  (int value) => throw new ServerSideOnlyException(nameof(ServerOnly));

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
			var selectQuery = query.GetSelectQuery();

			selectQuery.Find(e => e is SqlFunction { Name: "ABS" }).ShouldNotBeNull();

			// The conditional AROUND it has to stay in SQL too. Emitting the three operands as separate columns and
			// picking a branch on the client keeps ABS in the query (so the check above still passes) but evaluates
			// the server-side-only call for every row, not just the matching ones.
			selectQuery.Find(e => e is SqlConditionExpression).ShouldNotBeNull();
			selectQuery.Select.Columns.Count.ShouldBe(1);

			_ = query.ToArray(); // must not throw
		}

		[Test]
		public void ServerSideOnlyAggregateStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				group e by e.Id > 1 into g
				select new { Names = g.StringAggregate(", ", e => e.Name).ToValue() };

			// Executing is the assertion: StringAggregate's IEnumerable overload is a throw-only stub, so a
			// client fold would throw rather than return rows. Measured: this passes with the [ServerSideOnly]
			// marker removed too - the aggregate is claimed by the translator before any fold decision, so the
			// marker is inert here. Kept as coverage that the aggregate survives both option settings.
			_ = query.ToArray();
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

		// String composition. Inside an expression tree the compiler lowers every interpolated string to string.Format
		// (its string.Concat optimisation does not apply there), which HandleStringFormat translates inline rather than
		// a member translator. `a + b` on strings is a binary node instead.

		[Table]
		sealed class StringCalcEntity
		{
			[Column, PrimaryKey] public int     Id    { get; set; }
			[Column]             public string? Name  { get; set; }
			[Column]             public string? Name2 { get; set; }
			[Column]             public int     Num   { get; set; }

			public static readonly StringCalcEntity[] Seed =
			[
				new() { Id = 1, Name = "John", Name2 = "Smith", Num = 42 },
				new() { Id = 2, Name = null,   Name2 = "Doe",   Num = 7  },
				new() { Id = 3, Name = "Ann",  Name2 = null,    Num = 0  },
			];
		}

		static void AssertComposition<T>(IQueryable<T> query, bool preferClient, bool multiPart = true)
		{
			var selectQuery = query.GetSelectQuery();

			selectQuery.Select.Columns.All(c => c.Expression is SqlField).ShouldBe(preferClient);

			if (multiPart)
				(selectQuery.Find(e => e is SqlConcatExpression) != null).ShouldBe(!preferClient);
		}

		[Test]
		public void InterpolationTwoHolesProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			var query = from e in table select $"{e.Name} {e.Name2}";

			AssertQuery(query);
			AssertComposition(query, preferClient);
		}

		[Test]
		public void InterpolationManyHolesProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			// Four holes: the compiler picks string.Format(String, Object[]) with a NewArrayInit.
			var query = from e in table select $"{e.Name}, {e.Name2} ({e.Name}/{e.Name2})";

			AssertQuery(query);
			AssertComposition(query, preferClient);
		}

		[Test]
		public void InterpolationNonStringHoleProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			var query = from e in table select $"{e.Num}: {e.Name}";

			AssertQuery(query);
			AssertComposition(query, preferClient);
		}

		[Test]
		public void InterpolationFormatSpecifierProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			var query = from e in table select $"{e.Num:D4}";

			// Single-part format, so no concat node either way - only the raw-field assertion applies.
			AssertComposition(query, preferClient, multiPart: false);

			// The value can only be asserted client-side: translated to SQL the format specifier is silently
			// dropped (linq2db#5921), so the server-side result is "42" where .NET produces "0042".
			if (preferClient)
				AssertQuery(query);
		}

		[Test]
		public void BinaryAddConcatProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			var query = from e in table select e.Name + " " + e.Name2;

			AssertQuery(query);
			AssertComposition(query, preferClient);
		}

		[Test]
		public void InterpolationOverMissedLeftJoin([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var results =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new
				 {
					 Value    = $"[{j.Value1}]",
					 Nullable = $"[{(int?)j.Value1}]",
				 })
				.ToArray();

			// The option must not change the result: a non-nullable column of the missed row reads as default(int), and a
			// column cast to a nullable type carries the NULL into the hole.
			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Value == "[0]" && r.Nullable == "[]");
		}

		[Table]
		sealed class MissedJoinEntity
		{
			[Column, PrimaryKey] public int      Id     { get; set; }
			[Column]             public int      Value1 { get; set; }
			[Column]             public DateTime Date   { get; set; }

			public static readonly MissedJoinEntity[] Seed =
			[
				new() { Id = 1, Value1 = 10, Date = new DateTime(2020, 1, 15) },
				new() { Id = 2, Value1 = 20, Date = new DateTime(2021, 2, 16) },
			];
		}

		// A non-nullable member of a missed LEFT JOIN row reads as default(T), and a calculation over it answers what C#
		// answers for default(T), whichever side computes it. A date is not among the types this covers: 0001-01-01, the
		// default of a DateTime, is a date Access, SQL CE, Sybase and ClickHouse do not have, so the rule leaves date types
		// alone and a calculation over a date keeps answering from the NULL.
		[Test]
		public void CalculationOverMissedLeftJoinReadsDefault([DataSources] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			// A captured date rather than a constructed one: new DateTime(...) inside the query is built by the provider,
			// and PostgreSQL builds it with make_timestamp, which its 9.2 and 9.3 do not have.
			var bound = new DateTime(2000, 1, 1);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					Plus    = j.Value1 + 1,
					Compare = j.Value1 < 5 ? "a" : "b",
					Abs     = Math.Abs(j.Value1 - 1),
					// A date has no default the SQL can carry, so these are answered for the missed row instead, without a
					// date ever reaching the query: the year of a missed row is 1, it is not later than 2000 and it is
					// earlier - the last one is what a plain NULL comparison gets wrong, since NULL is not earlier either.
					Year    = j.Date.Year,
					Later   = j.Date > bound ? "y" : "n",
					Earlier = j.Date < bound ? "y" : "n",
					// Compared against a column of the row instead of a constant: the missed row has the least date there
					// is, so nothing of the row is below it and everything is at or above it, and both are answered
					// without naming that date. The other four operators are not decided by that alone - they turn on
					// whether the other column is the least date itself, which only the literal could tell.
					AfterOwn  = j.Date > e.Date  ? "y" : "n",
					AtMostOwn = j.Date <= e.Date ? "y" : "n",
				};

			AssertQuery(query);
		}

		// Sql.ToNullable / Sql.AsNullable translate their own argument through ITranslationContext.Translate. The option
		// must not apply to that argument: kept client-side it is not SQL, the widener declines, the whole call is
		// calculated on the client and the missed LEFT JOIN row reads default(int) instead of null (linq2db#5923).
		// The arguments are the shapes the option moves client-side: a binary and an attributed function.

		[Test]
		public void ToNullableOverNestedBinaryReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Joined = Sql.ToNullable(j.Value1 + 1) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void ToNullableOverNestedFunctionReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Joined = Sql.ToNullable(PreferClient(j.Value1)) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void AsNullableOverNestedFunctionReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Joined = (int?)Sql.AsNullable(PreferClient(j.Value1)) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void ToNullableOverNestedBinaryOnCteReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var cte = table.AsCte();

			var query =
				from e in table
				from c in cte.LeftJoin(c => c.Id == e.Id + 1000)
				select new { e.Id, Joined = Sql.ToNullable(c.Value1 + 1) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		// A cast to a nullable type asks for the NULL just as Sql.ToNullable does, whatever the target type. The compiler
		// writes a cast to the operand's own type or a narrowing one as a single conversion, and a widening one as a numeric
		// conversion followed by the lift; neither form, nor the option, may turn the NULL of a missed LEFT JOIN row into
		// default(T).
		[Test]
		public void NullableCastOverMissedLeftJoinReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					Int           = (int?)(j.Value1 + 1),
					Short         = (short?)(j.Value1 + 1),
					Byte          = (byte?)(j.Value1 + 1),
					Long          = (long?)(j.Value1 + 1),
					Float         = (float?)(j.Value1 + 1),
					Double        = (double?)(j.Value1 + 1),
					Decimal       = (decimal?)(j.Value1 + 1),
					IntColumn     = (int?)j.Value1,
					ShortColumn   = (short?)j.Value1,
					ByteColumn    = (byte?)j.Value1,
					LongColumn    = (long?)j.Value1,
					FloatColumn   = (float?)j.Value1,
					DoubleColumn  = (double?)j.Value1,
					DecimalColumn = (decimal?)j.Value1,
				};

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);

			results.ShouldAllBe(r => r.Int           == null);
			results.ShouldAllBe(r => r.Short         == null);
			results.ShouldAllBe(r => r.Byte          == null);
			results.ShouldAllBe(r => r.Long          == null);
			results.ShouldAllBe(r => r.Float         == null);
			results.ShouldAllBe(r => r.Double        == null);
			results.ShouldAllBe(r => r.Decimal       == null);
			results.ShouldAllBe(r => r.IntColumn     == null);
			results.ShouldAllBe(r => r.ShortColumn   == null);
			results.ShouldAllBe(r => r.ByteColumn    == null);
			results.ShouldAllBe(r => r.LongColumn    == null);
			results.ShouldAllBe(r => r.FloatColumn   == null);
			results.ShouldAllBe(r => r.DoubleColumn  == null);
			results.ShouldAllBe(r => r.DecimalColumn == null);
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

			// String interpolation. Only string holes: a numeric hole would compare .NET formatting against each
			// provider's CAST, which is a difference this option accepts rather than a regression.
			AssertQuery(from e in table select new { e.Id, Cat = $"{e.Name} {e.Name}" });
		}

		[Test]
		public void NestedNullableOverMissedJoinMatchesAcrossProviders([DataSources] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// AssertQuery cannot be used here: a missed LeftJoin materializes as null in LINQ to Objects, so the in-memory
			// arm would throw. The SQL answer is NULL, and it has to survive the option on every provider and through the
			// remote context, which the [IncludeDataSources] tests in this fixture exclude.
			var results =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new { e.Id, Joined = Sql.ToNullable(j.Value1 + 1) })
				.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
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

		[Test]
		public void ToNullableOverNestedMethodReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Joined = Sql.ToNullable(Math.Abs(j.Value1)) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void AsNullableOverNestedMethodReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Joined = (int?)Sql.AsNullable(Math.Abs(j.Value1)) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void ToNullableOverCteMethodReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// The CTE projection contains an opted-in method, and a CTE is read back through a build proxy, which
			// rebuilds under BuildFlags.ResetPrevious. If InsideTranslation did not survive that reset,
			// PreferClientCalculation would re-arm underneath ToNullable and collapse the SQL NULL to default(T).
			var cte = (from e in table select new { e.Id, Col = Math.Abs(e.Value1) }).AsCte();

			var query =
				from e in table
				from c in cte.LeftJoin(c => c.Id == e.Id + 1000)
				select new { e.Id, Joined = Sql.ToNullable(c.Col) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void ToNullableOverMethodOnCteColumnReturnsNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			// The opted-in method sits in ToNullable's own argument, over a column read back through the CTE proxy -
			// so the ResetPrevious rebuild happens while that argument is being translated. Dropping
			// InsideTranslation there re-arms PreferClientCalculation under a mandatory translator, which makes
			// ToNullable decline and collapses the SQL NULL to default(int).
			var cte = table.AsCte();

			var query =
				from e in table
				from c in cte.LeftJoin(c => c.Id == e.Id + 1000)
				select new { e.Id, Joined = Sql.ToNullable(Math.Abs(c.Value1)) };

			var results = query.ToArray();

			results.Length.ShouldBe(ClientCalcEntity.Seed.Length);
			results.ShouldAllBe(r => r.Joined == null);
		}

		[Test]
		public void NoOuterJoinEmitsNoGuard([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			// Nothing here can be NULL, so the guard costs nothing: no conditional, and the same one column the
			// option-off arm selects.
			var query = from e in table where e.Id == 1 select Convert.ToString(e.Value1);

			query.ToArray().Single().ShouldBe("10");
			query.GetSelectQuery().Select.Columns.Count.ShouldBe(1);
		}

		[Test]
		public void ConvertWithoutClientBodyStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// Convert.ToInt32(DateTime) throws InvalidCastException client-side for every input, so it must stay in
			// SQL in both arms - declining it turns a query that returns a value into one that throws.
			var query =
				from e in table
				select new
				{
					e.Id,
					V = Convert.ToInt32(e.Date),
				};

			query.GetSelectQuery().Select.Columns.Any(c => c.Expression is not SqlField).ShouldBeTrue();
			query.ToArray().Length.ShouldBe(BatchCalcEntity.Seed.Length);
		}

		// TranslateMember now runs before the option is consulted, and it returns early from the translated-SQL cache
		// that HandleExtension populates for the same call elsewhere in the query. The WHERE below is not a final
		// projection, so it translates the call and fills that cache; a hit in the projection would put ABS back into
		// the SELECT. The option's answer must not depend on whether an unrelated clause used the same function.

		[Test]
		public void AttributedFunctionMovesClientSideEvenWhenTranslatedInWhere([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(true));
			using var table = db.CreateLocalTable(ClientCalcEntity.Seed);

			var query = table.Where(e => PreferClient(e.Value1) == 20).Select(e => PreferClient(e.Value1));

			// Guards the assertion below from going vacuous: if the WHERE stopped translating, the cache would be
			// empty and the projection could be client-side for the wrong reason.
			query.ToSqlQuery().Sql.ShouldContain("ABS");

			query.GetSelectQuery().Select.Columns.All(c => c.Expression is SqlField).ShouldBeTrue();
		}

		[Test]
		public void StringCompareStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// Linq/Expressions.cs rewrites string.CompareOrdinal into string.CompareTo(string) before the registry
			// is consulted, and CompareTo is culture-sensitive: on the client "Bob" sorts after "apple" where an
			// ordinal comparison puts it before, so a client-side move returns 1 for that row instead of -1.
			// The mapping itself is linq2db#5927; until that is fixed the registration must stay mandatory.
			var query =
				from e in table
				select new
				{
					e.Id,
					Cmp = string.CompareOrdinal(e.Name, "apple"),
				};

			query.ToArray().ShouldAllBe(r => r.Cmp < 0);
			query.GetSelectQuery().Select.Columns.Any(c => c.Expression is not SqlField).ShouldBeTrue();
		}

		[Sql.Expression("{0} > 0", IsPredicate = true)]                        static bool AttributedPredicate(int value) => value > 0;
		[Sql.Expression("{0} > 0", IsPredicate = true, ServerSideOnly = true)] static bool ServerOnlyPredicate(int value) => throw new ServerSideOnlyException(nameof(ServerOnlyPredicate));

		[ExpressionMethod(nameof(IsPositiveImpl))]
		static bool IsPositive(int value) => value > 0;
		static Expression<Func<int, bool>> IsPositiveImpl() => v => v > 0;

		[Test]
		public void BooleanPredicateRoutesUnderOption([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(true));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// Measured routing for bool-returning members, pinned so a change in any of them is visible. The last
			// two rows are inconsistent with the rest and are tracked as linq2db#5925; they are asserted as they
			// behave today, not as they arguably should.
			static bool AllRaw<T>(IQueryable<T> q) => q.GetSelectQuery().Select.Columns.All(c => c.Expression is SqlField);

			// [Sql.Expression] -> HandleExtension, which the option gates.
			AllRaw(from e in table select new { e.Id, V = AttributedPredicate(e.Num) }).ShouldBeTrue();

			// ServerSideOnly excludes the node from the option entirely.
			AllRaw(from e in table select new { e.Id, V = ServerOnlyPredicate(e.Num) }).ShouldBeFalse();

			// [ExpressionMethod] expands before the gate; the expansion is a binary, which the option gates.
			AllRaw(from e in table select new { e.Id, V = IsPositive(e.Num) }).ShouldBeTrue();

			// The built-in predicate path declines under the option, so this moves too.
			AllRaw(from e in table select new { e.Id, V = e.Name.Contains("o") }).ShouldBeTrue();

			// #5925: expands to `p == null || p.Length == 0`; the comparison moves but Length is a member, and
			// members always translate, so a computed Length(...) column remains.
			AllRaw(from e in table select new { e.Id, V = string.IsNullOrEmpty(e.Name) }).ShouldBeFalse();

			// #5925: registered in the same optional scope as Replace / PadLeft / Trim*, which do move - this one
			// does not, and the whole predicate stays in SQL.
			AllRaw(from e in table select new { e.Id, V = string.IsNullOrWhiteSpace(e.Name) }).ShouldBeFalse();
		}

		[Test]
		public void BooleanMethodStaysInSqlDespiteOptionalRegistration([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// string.IsNullOrWhiteSpace is registered optional but does not move client-side, unlike the other
			// members of the same scope. Kept as its own case rather than silently dropped from the string batch:
			// the divergence is real and tracked as linq2db#5925, where the mechanism is still open. Results are
			// correct either way, so AssertQuery holds in both modes - only the SQL shape differs.
			var query = from e in table select new { e.Id, Ws = string.IsNullOrWhiteSpace(e.Name) };

			AssertQuery(query);

			query.GetSelectQuery().Select.Columns.Any(c => c.Expression is not SqlField).ShouldBeTrue();
		}

		[Test]
		public void MandatoryMembersStayInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// The mirror of the batches above: Sql.* marks intent to compute server-side, so none of these may be
			// pulled client-side whatever the option says. The DateAdd increment is a column, not a constant - with
			// a constant the translator declines in any projection and that column would pin nothing.
			var query =
				from e in table
				select new
				{
					e.Id,
					Added = Sql.DateAdd(Sql.DateParts.Day, e.Num, e.Date),
					Part  = Sql.DatePart(Sql.DateParts.Year, e.Date),
					Cat   = Sql.Concat(e.Name, "!"),
				};

			var selectQuery = query.GetSelectQuery();

			// Counted, not Any(): one computed column would otherwise satisfy the assertion for all three, so
			// making any single one of these registrations optional by mistake would leave the test green.
			selectQuery.Select.Columns.Count(c => c.Expression is not SqlField).ShouldBe(3);
			(selectQuery.Find(e => e is SqlConcatExpression) != null).ShouldBeTrue();
		}

		[Test]
		public void ServerSideOnlyMembersStayInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			// Members that must never move: SQL-only constructs, the sequence-taking concat overloads that share a
			// delegate with aggregate concat, and string.CompareTo(object), whose client body throws. Guid.NewGuid
			// is deliberately absent - measured, it produces no column in a projection in either arm, so there is
			// nothing here for a pin to hold.
			var query =
				from e in table
				select new
				{
					e.Id,
					Row  = Sql.Ext.RowNumber().Over().OrderBy(e.Id).ToValue(),
					Cats = Sql.ConcatStrings(",", e.Name, e.Name2),
					Cat  = string.Concat(new[] { e.Name, e.Name2 }),
					Now  = Sql.GetDate(),
					Cmp  = e.Name!.CompareTo((object)1),
				};

			// Counted, not Any(): one computed column would otherwise cover all five.
			query.GetSelectQuery().Select.Columns.Count(c => c.Expression is not SqlField).ShouldBe(5);
		}

		[Test]
		public void GroupedCompositionStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// string.Concat / string.Join over a grouping share their delegate with aggregate concat, which has no
			// client-side equivalent - declining them would leave the grouping in the projection.
			var concat = from e in table group e by e.Id > 1 into g select string.Concat(g.Select(x => x.Name));
			var join   = from e in table group e by e.Id > 1 into g select string.Join(", ", g.Select(x => x.Name));

			_ = concat.ToArray();
			_ = join.ToArray();
		}



		[Table]
		sealed class BatchCalcEntity
		{
			[Column, PrimaryKey] public int      Id   { get; set; }
			[Column]             public int      Num  { get; set; }
			[Column]             public double   Dbl  { get; set; }
			[Column]             public decimal  Dec  { get; set; }
			[Column]             public string   Name { get; set; } = null!;
			[Column]             public DateTime Date { get; set; }

			// Values deliberately avoid rounding midpoints: client and server disagree on midpoint rules, which
			// would make the option-off arm fail for a reason unrelated to this change.
			public static readonly BatchCalcEntity[] Seed =
			[
				new() { Id = 1, Num =  42, Dbl =  1.4, Dec =  10.2m, Name = "  John  ", Date = new DateTime(2020, 1, 15) },
				new() { Id = 2, Num =  -7, Dbl = -2.6, Dec =  -3.7m, Name = "Ann",      Date = new DateTime(2021, 6, 30) },
				new() { Id = 3, Num =   0, Dbl =  3.3, Dec =   5.1m, Name = "Bob",      Date = new DateTime(2022, 12, 1) },
			];
		}
	}
}
