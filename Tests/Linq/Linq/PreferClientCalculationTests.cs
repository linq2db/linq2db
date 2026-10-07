using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Text;

using LinqToDB;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.DataProvider.Sybase;
using LinqToDB.DataProvider.Ydb;
using LinqToDB.Internal.DataProvider;
using LinqToDB.Internal.SqlProvider;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Internal.SqlQuery.Visitors;
using LinqToDB.Mapping;
using LinqToDB.Remote;

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

		// AssertQuery compares results by their public members, which a scalar does not have (a string compares only its length).
		void AssertScalarQuery<T>(IQueryable<T> query)
		{
			AssertQuery(query, EqualityComparer<T>.Default);
		}

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

			AssertScalarQuery(query);

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

			AssertScalarQuery(query);

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

			AssertScalarQuery(query);

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

			AssertScalarQuery(query);

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

			AssertScalarQuery(query);
			AssertComposition(query, preferClient);
		}

		[Test]
		public void InterpolationManyHolesProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			// Four holes: the compiler picks string.Format(String, Object[]) with a NewArrayInit.
			var query = from e in table select $"{e.Name}, {e.Name2} ({e.Name}/{e.Name2})";

			AssertScalarQuery(query);
			AssertComposition(query, preferClient);
		}

		[Test]
		public void InterpolationNonStringHoleProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			var query = from e in table select $"{e.Num}: {e.Name}";

			AssertScalarQuery(query);
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
				AssertScalarQuery(query);
		}

		[Test]
		public void BinaryAddConcatProjection([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(StringCalcEntity.Seed);

			var query = from e in table select e.Name + " " + e.Name2;

			AssertScalarQuery(query);
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
			[Column]             public bool     Flag   { get; set; }
			[Column]             public string?  Name   { get; set; }

			public static readonly MissedJoinEntity[] Seed =
			[
				new() { Id = 1, Value1 = 10, Date = new DateTime(2020, 1, 15), Flag = true,  Name = "one" },
				new() { Id = 2, Value1 = 20, Date = new DateTime(2021, 2, 16), Flag = false, Name = "two" },
			];
		}

		[Table]
		sealed class PartialJoinEntity
		{
			[Column, PrimaryKey] public int Id     { get; set; }
			[Column]             public int Value1 { get; set; }
			[Column]             public int Grp    { get; set; }

			// Joined on j.Id == e.Id + 1, the rows read the joined Value1 as 0, -5 and a missed row: the matched 0 is what
			// a defaulted missed row would be mistaken for.
			public static readonly PartialJoinEntity[] Seed =
			[
				new() { Id = 1, Value1 =  5, Grp = 1 },
				new() { Id = 2, Value1 =  0, Grp = 1 },
				new() { Id = 3, Value1 = -5, Grp = 1 },
			];
		}

		[Table]
		sealed class KeyedEntity
		{
			[Column, PrimaryKey] public int     Id   { get; set; }
			[Column]             public string? Name { get; set; }

			// The key 0: a missed row read as default(T) would claim it.
			public static readonly KeyedEntity[] Seed =
			[
				new() { Id = 0, Name = "zero" },
				new() { Id = 1, Name = "one"  },
			];
		}

		[Table]
		sealed class KeyedChildEntity
		{
			[Column, PrimaryKey] public int Id       { get; set; }
			[Column]             public int ParentId { get; set; }

			[Association(ThisKey = nameof(ParentId), OtherKey = nameof(KeyedEntity.Id), CanBeNull = true)]
			public KeyedEntity? Parent { get; set; }

			public static readonly KeyedChildEntity[] Seed =
			[
				new() { Id = 1, ParentId = 1 },
				new() { Id = 2, ParentId = 1 },
			];
		}

		sealed class CalculatedDto
		{
			public int Id { get; set; }
			public int X  { get; set; }
		}

		[Sql.Function("COUNT", ServerSideOnly = true, IsAggregate = true, ArgIndices = new[] { 1 })]
		static int CountValues<TSource>(IEnumerable<TSource> source, Expression<Func<TSource, int>> value)
			=> throw new ServerSideOnlyException(nameof(CountValues));

		[Sql.Extension("COUNT({value})", IsAggregate = true, ServerSideOnly = true)]
		static int CountValuesExtension<TSource>(IEnumerable<TSource> source, [ExprParameter] Expression<Func<TSource, int>> value)
			=> throw new ServerSideOnlyException(nameof(CountValuesExtension));

		[Table]
		sealed class CountedEntity
		{
			[Column, PrimaryKey] public int Id       { get; set; }
			[Column]             public int ParentId { get; set; }

			// A child of the key 0: a missed row read as default(T) would claim it.
			public static readonly CountedEntity[] Seed =
			[
				new() { Id = 1, ParentId = 0 },
				new() { Id = 2, ParentId = 1 },
			];
		}

		// A subquery correlated to a missed LEFT JOIN row filters by the row's key, and a filter reads the missed row as NULL:
		// it matches nothing, whether or not the count is then calculated with. Read as default(T), the key would be 0 and the
		// missed row would claim the child of the key 0.
		[Test]
		public void CorrelatedSubqueryOverMissedLeftJoinFiltersByNull([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table   = db.CreateLocalTable(MissedJoinEntity.Seed);
			using var counted = db.CreateLocalTable(CountedEntity.Seed);

			var results =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new
				 {
					 e.Id,
					 Alone    = counted.Count(c => c.ParentId == j.Id),
					 WithCalc = j.Value1 + counted.Count(c => c.ParentId == j.Id),
				 })
				.ToArray();

			results.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			results.ShouldAllBe(r => r.Alone == 0 && r.WithCalc == 0);
		}

		// A non-nullable member of a missed LEFT JOIN row reads as default(T) when the query calculates with it, and the
		// calculation answers what C# answers for default(T), whichever side computes it. A date is the one type whose default
		// not every engine has: where 0001-01-01 is below the least date the type holds, that least date is written instead,
		// which keeps every comparison right and moves only the year a date part reads.
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
					Plus      = j.Value1 + 1,
					Compare   = j.Value1 < 5 ? "a" : "b",
					Abs       = Math.Abs(j.Value1 - 1),
					Later     = j.Date > bound ? "y" : "n",
					Earlier   = j.Date < bound ? "y" : "n",
					AfterOwn  = j.Date > e.Date  ? "y" : "n",
					AtMostOwn = j.Date <= e.Date ? "y" : "n",
				};

			AssertQuery(query);

			// The year is read off the date written for the missed row - the least date the engine has where it has no
			// 0001-01-01.
			var years =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select j.Date.Year)
				.ToArray();

			years.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			years.ShouldAllBe(year => year == MissedRowYear(context));
		}

		static int MissedRowYear(string context)
		{
			if (context.IsAnyOf(TestProvName.AllAccess))
				return 100;

			if (context.IsAnyOf(TestProvName.AllSqlServer2005, TestProvName.AllSqlCe, TestProvName.AllSybase))
				return 1753;

			// ClickHouse moves a date below its DateTime64 range to the start of the range itself.
			if (context.IsAnyOf(TestProvName.AllClickHouse))
				return 1900;

			if (context.IsAnyOf(TestProvName.AllYdb))
				return 1970;

			return 1;
		}

		[Table]
		sealed class TranslatedMemberEntity
		{
			[Column, PrimaryKey] public int      Id     { get; set; }
			[Column]             public int      Value1 { get; set; }
			[Column]             public DateTime Date   { get; set; }
			[Column]             public Guid     Key    { get; set; }
			[Column]             public string?  Name   { get; set; }

			public static readonly TranslatedMemberEntity[] Seed =
			[
				new() { Id = 1, Value1 = 10, Date = new DateTime(2020, 1, 15), Key = new Guid("3f2504e0-4f89-11d3-9a0c-0305e82c3301"), Name = "one" },
				new() { Id = 2, Value1 = 20, Date = new DateTime(2021, 2, 16), Key = new Guid("7c9e6679-7425-40de-944b-e07fc1f90ae7"), Name = null  },
			];
		}

		// A member with a translator of its own reads a missed row's non-nullable member as default(T), as an operator does:
		// conversions to string, Math.Max / Math.Min (GREATEST / LEAST where the engine has them), string.Concat over boxed
		// values and a date shift. A nullable string is not defaulted: the concat reads its NULL as empty.
		[Test]
		public void TranslatedMemberOverMissedLeftJoinReadsDefault([DataSources] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(TranslatedMemberEntity.Seed);

			var rows =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new
				 {
					 e.Value1,
					 Text    = Convert.ToString(j.Value1),
					 Key     = j.Key.ToString(),
					 Max     = Math.Max(j.Value1, 5),
					 Min     = Math.Min(j.Value1, -5),
					 Boxed   = string.Concat((object)j.Value1, (object)"!"),
					 Name    = string.Concat(j.Name, "!"),
					 Shifted = j.Date.AddDays(e.Value1),
					 Year    = j.Date.AddDays(e.Value1).Year,
					 Day     = j.Date.AddDays(e.Value1).Day,
				 })
				.ToArray();

			// Asserted apart: the in-memory arm of AssertQuery does not null-guard an instance call on a value type.
			rows.Length.ShouldBe(TranslatedMemberEntity.Seed.Length);
			rows.ShouldAllBe(r => r.Text  == "0");
			rows.ShouldAllBe(r => r.Key   == "00000000-0000-0000-0000-000000000000");
			rows.ShouldAllBe(r => r.Max   == 5);
			rows.ShouldAllBe(r => r.Min   == -5);
			rows.ShouldAllBe(r => r.Boxed == "0!");
			rows.ShouldAllBe(r => r.Name  == "!");

			// The shifted date is the one written for the missed row: the least date the engine has where it has no 0001-01-01.
			rows.ShouldAllBe(r => r.Year == MissedRowYear(context) && r.Day == 1 + r.Value1);

			// Access reads a date before 1900 back into .NET shifted (0100-01-01 arrives as 0098-11-26), so there only the parts
			// it calculates itself are compared.
			if (!context.IsAnyOf(TestProvName.AllAccess))
				rows.ShouldAllBe(r => r.Shifted == new DateTime(MissedRowYear(context), 1, 1).AddDays(r.Value1));
		}

		// The group-join form reaches the missed row through DefaultIfEmpty rather than LeftJoin, and reads it alike.
		[Test]
		public void GroupJoinOverMissedRowReadsDefault([DataSources] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(TranslatedMemberEntity.Seed);

			var query =
				from e in table
				join j in table on e.Id + 1000 equals j.Id into g
				from j in g.DefaultIfEmpty()
				select new
				{
					e.Id,
					Text = Convert.ToString(j.Value1),
					Plus = j.Value1 + 1,
				};

			AssertQuery(query);
		}

		// DateOnly and DateTimeOffset: of the providers that create them, only ClickHouse and YDB map them to a type without
		// 0001-01-01.
		static int MissedRowNativeYear(string context)
		{
			if (context.IsAnyOf(TestProvName.AllClickHouse))
				return 1900;

			if (context.IsAnyOf(TestProvName.AllYdb))
				return 1970;

			return 1;
		}

#if SUPPORTS_DATEONLY
		[Table]
		sealed class MissedDayEntity
		{
			[Column, PrimaryKey] public int      Id  { get; set; }
			[Column]             public DateOnly Day { get; set; }

			public static readonly MissedDayEntity[] Seed =
			[
				new() { Id = 1, Day = new DateOnly(2020, 1, 15) },
				new() { Id = 2, Day = new DateOnly(2021, 2, 16) },
			];
		}

		[Test]
		public void DateOnlyOverMissedLeftJoinReadsDefault([DataSources(TestProvName.AllAccess, TestProvName.AllSqlCe, TestProvName.AllSqlServer2005, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedDayEntity.Seed);

			var bound = new DateOnly(2000, 1, 1);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					Later     = j.Day > bound  ? "y" : "n",
					Earlier   = j.Day < bound  ? "y" : "n",
					AfterOwn  = j.Day > e.Day  ? "y" : "n",
					AtMostOwn = j.Day <= e.Day ? "y" : "n",
				};

			AssertQuery(query);

			var years =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select j.Day.Year)
				.ToArray();

			years.Length.ShouldBe(MissedDayEntity.Seed.Length);
			years.ShouldAllBe(year => year == MissedRowNativeYear(context));
		}
#endif

		[Table]
		sealed class MissedMomentEntity
		{
			[Column, PrimaryKey] public int            Id     { get; set; }
			[Column]             public DateTimeOffset Moment { get; set; }

			public static readonly MissedMomentEntity[] Seed =
			[
				new() { Id = 1, Moment = new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero) },
				new() { Id = 2, Moment = new DateTimeOffset(2021, 2, 16, 0, 0, 0, TimeSpan.Zero) },
			];
		}

		// Comparisons only against a column of the member's own type, so that no stand-in type's range decides them.
		[Test]
		public void OffsetOverMissedLeftJoinReadsDefault([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse, TestProvName.AllYdb, TestProvName.AllOracle, TestProvName.AllDuckDB)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedMomentEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					AfterOwn  = j.Moment > e.Moment  ? "y" : "n",
					AtMostOwn = j.Moment <= e.Moment ? "y" : "n",
				};

			AssertQuery(query);

			var years =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select j.Moment.Year)
				.ToArray();

			years.Length.ShouldBe(MissedMomentEntity.Seed.Length);
			years.ShouldAllBe(year => year == MissedRowNativeYear(context));
		}

		[Table]
		sealed class MappedMomentEntity
		{
			[Column, PrimaryKey]                   public int            Id     { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTimeOffset Moment { get; set; }

			public static readonly MappedMomentEntity[] Seed =
			[
				new() { Id = 1, Moment = new DateTimeOffset(2020, 1, 15, 0, 0, 0, TimeSpan.Zero) },
				new() { Id = 2, Moment = new DateTimeOffset(2021, 2, 16, 0, 0, 0, TimeSpan.Zero) },
			];
		}

		// A DateTimeOffset stored as a plain date and time, where the provider has no offset type or the mapping names another.
		// Not over LinqService: the remote path cannot insert a DateTimeOffset into a column mapped as DataType.DateTime on Sybase.
		[Test]
		public void MappedOffsetOverMissedLeftJoinReadsDefault([IncludeDataSources(TestProvName.AllSybase, TestProvName.AllAccess, TestProvName.AllSqlServer)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MappedMomentEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					AfterOwn  = j.Moment > e.Moment  ? "y" : "n",
					AtMostOwn = j.Moment <= e.Moment ? "y" : "n",
				};

			AssertQuery(query);

			var years =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select j.Moment.Year)
				.ToArray();

			years.Length.ShouldBe(MappedMomentEntity.Seed.Length);
			years.ShouldAllBe(year => year == (context.IsAnyOf(TestProvName.AllAccess) ? 100 : 1753));

			// The same table read with the member mapped by default, as a table created elsewhere is: Sybase reads such a member
			// through datetime, and SQL Server 2008+ writes the default as a datetimeoffset, which holds 0001-01-01.
			var defaultMapped = db.GetTable<DefaultMomentEntity>();

			var defaultYears =
				(from e in defaultMapped
				 from j in defaultMapped.LeftJoin(j => j.Id == e.Id + 1000)
				 select j.Moment.Year)
				.ToArray();

			var defaultYear =
				context.IsAnyOf(TestProvName.AllAccess)                                ? 100  :
				context.IsAnyOf(TestProvName.AllSybase, TestProvName.AllSqlServer2005) ? 1753 :
				                                                                         1;

			defaultYears.Length.ShouldBe(MappedMomentEntity.Seed.Length);
			defaultYears.ShouldAllBe(year => year == defaultYear);
		}

		[Table(nameof(MappedMomentEntity))]
		sealed class DefaultMomentEntity
		{
			[Column, PrimaryKey] public int            Id     { get; set; }
			[Column]             public DateTimeOffset Moment { get; set; }
		}

		[Table(nameof(MissedMomentEntity))]
		sealed class MomentAsDateTimeEntity
		{
			[Column, PrimaryKey]                         public int      Id     { get; set; }
			[Column(DataType = DataType.DateTimeOffset)] public DateTime Moment { get; set; }
		}

		// A DateTime member mapped as an offset type is written by SQL Server's converter as datetime, so its default is clamped
		// to datetime's least date over the native offset column the table holds.
		[Test]
		public void DateTimeMappedAsOffsetOverMissedLeftJoinReadsDefault([IncludeDataSources(TestProvName.AllSqlServer2008Plus)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedMomentEntity.Seed);

			var mapped = db.GetTable<MomentAsDateTimeEntity>();

			var years =
				(from e in mapped
				 from j in mapped.LeftJoin(j => j.Id == e.Id + 1000)
				 select j.Moment.Year)
				.ToArray();

			years.Length.ShouldBe(MissedMomentEntity.Seed.Length);
			years.ShouldAllBe(year => year == 1753);
		}

		[Table]
		sealed class PhysicalDateEntity
		{
			[Column, PrimaryKey]                                                 public int      Id         { get; set; }
			[Column(DbType = "smalldatetime", DataType = DataType.SmallDateTime)] public DateTime SmallTyped { get; set; }
			[Column(DbType = "smalldatetime")]                                   public DateTime SmallPlain { get; set; }
			[Column(DbType = "datetime", DataType = DataType.DateTime)]           public DateTime DtTyped    { get; set; }
			[Column(DbType = "datetime")]                                        public DateTime DtPlain    { get; set; }
			[Column(DbType = "date", DataType = DataType.Date)]                   public DateTime DateTyped  { get; set; }
			[Column(DbType = "date")]                                            public DateTime DatePlain  { get; set; }
#if SUPPORTS_DATEONLY
			[Column(DataType = DataType.Date)]                                   public DateOnly OnlyDate     { get; set; }
			[Column(DataType = DataType.DateTime)]                               public DateOnly OnlyDateTime { get; set; }
#endif

			public static readonly PhysicalDateEntity[] Seed =
			[
				new()
				{
					Id           = 1,
					SmallTyped   = new DateTime(2020, 1, 15),
					SmallPlain   = new DateTime(2020, 1, 15),
					DtTyped      = new DateTime(2020, 1, 15),
					DtPlain      = new DateTime(2020, 1, 15),
					DateTyped    = new DateTime(2020, 1, 15),
					DatePlain    = new DateTime(2020, 1, 15),
#if SUPPORTS_DATEONLY
					OnlyDate     = new DateOnly(2020, 1, 15),
					OnlyDateTime = new DateOnly(2020, 1, 15),
#endif
				},
			];
		}

		// The default is written as the column's mapped type, whatever the physical type: a mapped type narrower than
		// 0001-01-01 is clamped to its own least date, and a physical column narrower than its mapping is promoted to the
		// mapped type rather than handed a date it cannot hold.
		// Not over LinqService: the remote path cannot insert a DateOnly into a column mapped as DataType.DateTime on SQL Server.
		[Test]
		public void PhysicalDateTypesOverMissedLeftJoinReadDefault([IncludeDataSources(TestProvName.AllSybase, TestProvName.AllSqlServer2008Plus)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(PhysicalDateEntity.Seed);

			var row =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new
				 {
					 SmallTyped   = j.SmallTyped.Year,
					 SmallPlain   = j.SmallPlain.Year,
					 DtTyped      = j.DtTyped.Year,
					 DtPlain      = j.DtPlain.Year,
					 DateTyped    = j.DateTyped.Year,
					 DatePlain    = j.DatePlain.Year,
#if SUPPORTS_DATEONLY
					 OnlyDate     = j.OnlyDate.Year,
					 OnlyDateTime = j.OnlyDateTime.Year,
#endif
				 })
				.Single();

			// Sybase maps a plain DateTime to its datetime, from 1753; SQL Server to datetime2, from 0001.
			var plainYear = context.IsAnyOf(TestProvName.AllSybase) ? 1753 : 1;

			row.SmallTyped.ShouldBe(1900);
			row.SmallPlain.ShouldBe(plainYear);
			row.DtTyped   .ShouldBe(1753);
			row.DtPlain   .ShouldBe(plainYear);
			// A mapped Date is not clamped; Sybase's default DateTime is 1753-01-01 already.
			row.DateTyped .ShouldBe(plainYear);
			row.DatePlain .ShouldBe(plainYear);
#if SUPPORTS_DATEONLY
			row.OnlyDate    .ShouldBe(1);
			row.OnlyDateTime.ShouldBe(1753);
#endif
		}

		// The DateTimeOffset default each provider writes, read off the lowered value rather than through a time zone: SQL Server
		// writes a DateTimeOffset through LocalDateTime on a datetime path, so it is given a DateTime; the others read it at
		// offset zero.
		[Test]
		public void DateTimeOffsetDefaultIsLoweredInTheProviderCoordinate()
		{
			var sqlServer = LowerDefault(SqlServerTools.GetDataProvider(SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient), typeof(DateTimeOffset), DataType.DateTime);
			sqlServer.ShouldBeOfType<SqlValue>().Value.ShouldBeOfType<DateTime>().ShouldBe(new DateTime(1753, 1, 1));

			AssertAtZeroOffset(LowerDefault(ClickHouseTools.GetDataProvider(ClickHouseProvider.ClickHouseDriver), typeof(DateTimeOffset), DataType.DateTime64), new DateTime(1900, 1, 1));
#if !NETFRAMEWORK
			AssertAtZeroOffset(LowerDefault(YdbTools.GetDataProvider(), typeof(DateTimeOffset), DataType.DateTime2), new DateTime(1970, 1, 1));
#endif

			// Sybase writes the default as a cast to the mapped type.
			var sybase = LowerDefault(SybaseTools.GetDataProvider(SybaseProvider.DataAction), typeof(DateTimeOffset), DataType.DateTime);
			AssertAtZeroOffset(sybase.ShouldBeOfType<SqlCastExpression>().Expression, new DateTime(1753, 1, 1));

			AssertAtZeroOffset(LowerDefault(AccessTools.GetDataProvider(AccessVersion.Ace, AccessProvider.ODBC), typeof(DateTimeOffset), DataType.DateTime), new DateTime(100, 1, 1));

			static void AssertAtZeroOffset(ISqlExpression lowered, DateTime least)
			{
				var value = lowered.ShouldBeOfType<SqlValue>().Value.ShouldBeOfType<DateTimeOffset>();

				value.Offset.ShouldBe(TimeSpan.Zero);
				value.DateTime.ShouldBe(least);
			}
		}

		// A date default is clamped wherever the provider's converter writes it as a narrow date, the converter's default arm
		// included, and left alone where the written type holds 0001-01-01.
		[Test]
		public void DateDefaultIsClampedAsTheConverterWritesIt()
		{
			var sqlServer2019 = SqlServerTools.GetDataProvider(SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient);

			LoweredYear(LowerDefault(sqlServer2019, typeof(DateTime),       DataType.Time          )).ShouldBe(1753);
			LoweredYear(LowerDefault(sqlServer2019, typeof(DateTime),       DataType.Timestamp     )).ShouldBe(1753);
			LoweredYear(LowerDefault(sqlServer2019, typeof(DateTime),       DataType.DateTimeOffset)).ShouldBe(1753);
			LoweredYear(LowerDefault(sqlServer2019, typeof(DateTime),       DataType.SmallDateTime )).ShouldBe(1900);
			LoweredYear(LowerDefault(sqlServer2019, typeof(DateTime),       DataType.DateTime2     )).ShouldBe(1);
			LoweredYear(LowerDefault(sqlServer2019, typeof(DateTimeOffset), DataType.DateTimeOffset)).ShouldBe(1);

			var sqlServer2005 = SqlServerTools.GetDataProvider(SqlServerVersion.v2005, SqlServerProvider.MicrosoftDataSqlClient);

			LoweredYear(LowerDefault(sqlServer2005, typeof(DateTimeOffset), DataType.DateTimeOffset)).ShouldBe(1753);

#if !NETFRAMEWORK
			var ydb = YdbTools.GetDataProvider();

			LoweredYear(LowerDefault(ydb, typeof(DateTime), DataType.SmallDateTime)).ShouldBe(1970);
			LoweredYear(LowerDefault(ydb, typeof(DateTime), DataType.Timestamp    )).ShouldBe(1970);
			LoweredYear(LowerDefault(ydb, typeof(DateTime), DataType.DateTimeTz   )).ShouldBe(1970);
			LoweredYear(LowerDefault(ydb, typeof(DateTime), DataType.DateTime64   )).ShouldBe(1);
			LoweredYear(LowerDefault(ydb, typeof(DateTime), DataType.Timestamp64  )).ShouldBe(1);
#endif

			var clickHouse = ClickHouseTools.GetDataProvider(ClickHouseProvider.ClickHouseDriver);

			LoweredYear(LowerDefault(clickHouse, typeof(DateTimeOffset), DataType.DateTime2     )).ShouldBe(1900);
			LoweredYear(LowerDefault(clickHouse, typeof(DateTimeOffset), DataType.SmallDateTime )).ShouldBe(1900);
			LoweredYear(LowerDefault(clickHouse, typeof(DateTimeOffset), DataType.DateTimeOffset)).ShouldBe(1900);

			// Sybase casts to the mapped type only where ASE spells it.
			var sybase = SybaseTools.GetDataProvider(SybaseProvider.DataAction);

			LowerDefault(sybase, typeof(DateTimeOffset), DataType.DateTimeOffset).ShouldBeOfType<SqlCastExpression>().ToType.DataType.ShouldBe(DataType.DateTime);
			LowerDefault(sybase, typeof(DateTime),       DataType.Timestamp     ).ShouldBeOfType<SqlCastExpression>().ToType.DataType.ShouldBe(DataType.DateTime);
			LowerDefault(sybase, typeof(DateTime),       DataType.SmallDateTime ).ShouldBeOfType<SqlCastExpression>().ToType.DataType.ShouldBe(DataType.SmallDateTime);

			static int LoweredYear(ISqlExpression lowered)
			{
				return (lowered is SqlCastExpression cast ? cast.Expression : lowered).ShouldBeOfType<SqlValue>().Value switch
				{
					DateTime       dateTime       => dateTime.Year,
					DateTimeOffset dateTimeOffset => dateTimeOffset.Year,
					var value                     => throw new InvalidOperationException($"Not a date: {value}"),
				};
			}
		}

		static ISqlExpression LowerDefault(IDataProvider dataProvider, Type systemType, DataType dataType)
		{
			var dataOptions  = new DataOptions();
			var sqlOptimizer = dataProvider.GetSqlOptimizer(dataOptions);

			var optimizationContext = new OptimizationContext(
				new EvaluationContext(),
				dataOptions,
				dataProvider.SqlProviderFlags,
				dataProvider.MappingSchema,
				sqlOptimizer.CreateOptimizerVisitor(false),
				sqlOptimizer.CreateConvertVisitor(false),
				sqlOptimizer.CreateSqlExpressionFactory(dataProvider.MappingSchema, dataOptions),
				isParameterOrderDepended      : false,
				isAlreadyOptimizedAndConverted: false,
				parametersNormalizerFactory   : static () => NoopQueryParametersNormalizer.Instance);

			var defaultValue = new SqlDefaultValueExpression(new DbDataType(systemType, dataType), systemType == typeof(DateTime) ? (object)default(DateTime) : default(DateTimeOffset));

			return optimizationContext.OptimizeAndConvertAll<ISqlExpression>(defaultValue, NullabilityContext.NonQuery);
		}

		// A default no provider has lowered is written as the value it holds, typed as the node.
		[Test]
		public void UnloweredDefaultIsWrittenAsItsValue()
		{
			var dataProvider = SqlServerTools.GetDataProvider(SqlServerVersion.v2019, SqlServerProvider.MicrosoftDataSqlClient);

			var date      = new DbDataType(typeof(DateTime), DataType.Date);
			var dateTime2 = new DbDataType(typeof(DateTime), DataType.DateTime2);

			var writtenDate = Write(new SqlDefaultValueExpression(date, default(DateTime)));

			writtenDate.ShouldBe(Write(new SqlValue(date, default(DateTime))));
			Write(new SqlDefaultValueExpression(dateTime2, default(DateTime))).ShouldBe(Write(new SqlValue(dateTime2, default(DateTime))));

			// The two types are written apart, so the comparisons above see the node's type.
			writtenDate.ShouldNotBe(Write(new SqlValue(dateTime2, default(DateTime))));

			string Write(ISqlExpression expression)
			{
				var sql = new StringBuilder();

				dataProvider.CreateSqlBuilder(dataProvider.MappingSchema, new DataOptions()).BuildExpression(sql, expression, false);

				return sql.ToString();
			}
		}

		// The default is a leaf that every visitor passes over as it is; a clone shares it as it shares a value.
		[Test]
		public void DefaultSurvivesVisitorsUnchanged()
		{
			var defaultValue = new SqlDefaultValueExpression(new DbDataType(typeof(int), DataType.Int32), 0);
			var coalesce     = new SqlCoalesceExpression(new SqlValue(typeof(int?), null), defaultValue);

			foreach (var visitMode in new[] { VisitMode.ReadOnly, VisitMode.Modify, VisitMode.Transform })
			{
				new PassingVisitor(visitMode).Visit(coalesce).ShouldBeSameAs(coalesce);
				coalesce.Expressions[1].ShouldBeSameAs(defaultValue);
			}

			var clone = new SqlQueryCloneVisitor().Clone(coalesce, null).ShouldBeOfType<SqlCoalesceExpression>();

			clone.ShouldNotBeSameAs(coalesce);
			clone.Expressions[1].ShouldBeSameAs(defaultValue);
		}

		sealed class PassingVisitor : QueryElementVisitor
		{
			public PassingVisitor(VisitMode visitMode) : base(visitMode)
			{
			}
		}

		// A remote query carries the default through the serializer unchanged: the column's type, and the value of its own type.
		[Test]
		public void DefaultSurvivesRemoteSerialization()
		{
			// The serializer is internal and the test assembly sees no internals, so it is reached by reflection.
			var assembly            = typeof(DataOptions).Assembly;
			var serializer          = assembly.GetType("LinqToDB.Internal.Remote.LinqServiceSerializer", throwOnError: true)!;
			var serializationSchema = (MappingSchema)assembly.GetType("LinqToDB.Internal.Remote.SerializationMappingSchema", throwOnError: true)!.GetField("Instance")!.GetValue(null)!;
			var serialize           = serializer.GetMethod("Serialize",   [typeof(MappingSchema), typeof(SqlStatement), typeof(IReadOnlyParameterValues), typeof(IReadOnlyCollection<string>), typeof(DataOptions)])!;
			var deserialize         = serializer.GetMethod("Deserialize", [typeof(MappingSchema), typeof(MappingSchema), typeof(DataOptions), typeof(string)])!;

			var defaults = new List<SqlDefaultValueExpression>
			{
				new(new DbDataType(typeof(bool)),                                         false),
				new(new DbDataType(typeof(byte)),                                         (byte)0),
				new(new DbDataType(typeof(sbyte)),                                        (sbyte)0),
				new(new DbDataType(typeof(short)),                                        (short)0),
				new(new DbDataType(typeof(ushort)),                                       (ushort)0),
				new(new DbDataType(typeof(int), DataType.Int32, "int"),                   0),
				new(new DbDataType(typeof(uint)),                                         0u),
				new(new DbDataType(typeof(long)),                                         0L),
				new(new DbDataType(typeof(ulong)),                                        0UL),
				new(new DbDataType(typeof(float)),                                        0f),
				new(new DbDataType(typeof(double)),                                       0d),
				new(new DbDataType(typeof(decimal), DataType.Decimal, null, null, 18, 4), 0m),
				new(new DbDataType(typeof(decimal)),                                      1.25m),
				new(new DbDataType(typeof(DateTime), DataType.DateTime),                  DateTime.MinValue),
				new(new DbDataType(typeof(DateTime), DataType.DateTime2),                 DateTime.MinValue),
				new(new DbDataType(typeof(DateTime), DataType.Date),                      new DateTime(2020, 5, 6, 7, 8, 9, DateTimeKind.Utc)),
				new(new DbDataType(typeof(DateTimeOffset), DataType.DateTimeOffset),      DateTimeOffset.MinValue),
				new(new DbDataType(typeof(DateTimeOffset)),                               new DateTimeOffset(2020, 5, 6, 7, 8, 9, TimeSpan.FromHours(2))),
				new(new DbDataType(typeof(TimeSpan)),                                     TimeSpan.Zero),
				new(new DbDataType(typeof(Guid)),                                         Guid.Empty),
				new(new DbDataType(typeof(BigInteger)),                                   BigInteger.Zero),
#if SUPPORTS_DATEONLY
				new(new DbDataType(typeof(DateOnly), DataType.Date),                      DateOnly.MinValue),
				new(new DbDataType(typeof(TimeOnly), DataType.Time),                      TimeOnly.MinValue),
#endif
			};

			var query = new SelectQuery();

			foreach (var defaultValue in defaults)
			{
				query.Select.AddNew(new SqlCoalesceExpression(new SqlValue(defaultValue.Type, null), defaultValue));
			}

			var dataOptions = new DataOptions();
			var text        = (string)serialize.Invoke(null, [serializationSchema, new SqlSelectStatement(query), null, null, dataOptions])!;
			var statement   = ((LinqServiceQuery)deserialize.Invoke(null, [serializationSchema, MappingSchema.Default, dataOptions, text])!).Statement;
			var columns     = statement.SelectQuery!.Select.Columns;

			columns.Count.ShouldBe(defaults.Count);

			for (var i = 0; i < defaults.Count; i++)
			{
				var expected = defaults[i];
				var actual   = ((SqlCoalesceExpression)columns[i].Expression).Expressions[1].ShouldBeOfType<SqlDefaultValueExpression>();

				actual.Type.ShouldBe(expected.Type);
				actual.Value.ShouldNotBeNull().GetType().ShouldBe(expected.Value!.GetType());
				actual.Value.ShouldBe(expected.Value);

				// Equality ignores a DateTime's kind and a DateTimeOffset's offset.
				if (expected.Value is DateTime dateTime)
					((DateTime)actual.Value).Kind.ShouldBe(dateTime.Kind);

				if (expected.Value is DateTimeOffset dateTimeOffset)
					((DateTimeOffset)actual.Value).Offset.ShouldBe(dateTimeOffset.Offset);
			}
		}

		// A date the user writes is written as before, even below the least date of the column's type: only the default a
		// calculation reads is clamped.
		[Test]
		public void UserDateLiteralIsNotClamped([IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);

			var sql = context.IsAnyOf(TestProvName.AllAccess)
				? db.GetTable<MissedJoinEntity>().Where(e => e.Date > Sql.ToSql(DateTime.MinValue)).ToSqlQuery().Sql
				: db.GetTable<PhysicalDateEntity>().Where(e => e.DtTyped > Sql.ToSql(DateTime.MinValue)).ToSqlQuery().Sql;

			// SQL Server 2012+ spells the literal DATETIMEFROMPARTS(1, 1, 1, ...), the others as a string.
			(sql.Contains("0001-01-01") || sql.Contains("FROMPARTS(1, 1, 1,")).ShouldBeTrue();
		}

		// The check of a missed row still sees it missing while the calculation beside it reads the row's default, and the same
		// column read bare and calculated with answers each way in one query.
		[Test]
		public void NullCheckAndCalculationOverMissedLeftJoin([DataSources] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					Guarded = j == null ? -1 : j.Value1 + 1,
					Bare    = j.Value1,
					Plus    = j.Value1 + 1,
				};

			AssertQuery(query);

			var rows =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select j)
				.ToArray();

			rows.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			rows.ShouldAllBe(j => j == null);
		}

		// Every shape the projection calculates with reads a missed row's non-nullable member as default(T), directly, through
		// a numeric cast, a conditional's branch or an identity function, and whether the option moves the calculation or not.
		[Test]
		public void EveryCalculationOverMissedLeftJoinReadsDefault([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					Plus       = j.Value1 + 1,
					Bitwise    = j.Value1 | 1,
					Compare    = j.Value1 < 5 ? "a" : "b",
					InList     = new[] { 0 }.Contains(j.Value1) ? "y" : "n",
					Negated    = -j.Value1 + 1,
					NotFlag    = !j.Flag ? "t" : "f",
					Logical    = !(j.Flag || e.Id < 0) ? "t" : "f",
					AbsPlus    = Math.Abs(j.Value1) + 1,
					Year       = j.Date.Year,
					Concat     = "x" + j.Value1,
					Function   = PreferServer(j.Value1) + 1,
					Cast       = (double)j.Value1 + 1,
					Branch     = (e.Id > 0 ? j.Value1 : 5) + 1,
					Identity   = Sql.AsSql(j.Value1) + 1,
					AsNullable = Sql.AsNullable(j.Value1) + 1,
				};

			AssertQuery(query);

			query.ToSqlQuery().Sql.ShouldNotContain("DefaultValue");

			// Asserted apart: the in-memory arm of AssertQuery does not null-guard an instance call on a value type.
			var texts =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select j.Value1.ToString())
				.ToArray();

			texts.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			texts.ShouldAllBe(text => text == "0");
		}

		// A value projected as it is, not calculated with, keeps the NULL the reader turns into default(T): no COALESCE, and
		// DISTINCT still tells the missed row from a matched 0.
		[Test]
		public void BareValueOverMissedLeftJoinIsNotDefaulted([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table   = db.CreateLocalTable(MissedJoinEntity.Seed);
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, j.Value1, j.Date, V = Sql.AsSql(j.Value1) };

			AssertQuery(query);

			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");

			var distinct =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 select j.Value1)
				.Distinct()
				.ToArray();

			distinct.Length.ShouldBe(3);
		}

		// A predicate over a missed row keeps the SQL NULL: the missed row is not the matched row whose Value1 is 0.
		[Test]
		public void PredicateOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var ids =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 where j.Value1 + 1 == 1
				 select e.Id)
				.ToArray();

			ids.ShouldBe([1]);
		}

		// An association navigated from a missed row joins on the row's NULL key, not on the key 0.
		[Test]
		public void AssociationOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db       = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var parents  = db.CreateLocalTable(KeyedEntity.Seed);
			using var children = db.CreateLocalTable(KeyedChildEntity.Seed);

			var query =
				from e in children
				from j in children.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, Name = j.Parent!.Name };

			AssertQuery(query);
		}

		// A group key and a sort key are query structure: the missed row keeps the SQL NULL there, apart from the matched row
		// whose Value1 is 0.
		[Test]
		public void StructureOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var groupQuery =
				from e in partial
				from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				group e by j.Value1 + 1 into g
				select new { g.Key, Count = g.Count() };

			var groups = groupQuery.ToArray();

			groups.Length.ShouldBe(3);
			groups.ShouldAllBe(g => g.Count == 1);
			// The missed row's key is NULL, read as 0; a key calculated as a value would read 1 and render COALESCE.
			groups.Select(g => g.Key).OrderBy(k => k).ShouldBe([-4, 0, 1]);
			groupQuery.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");

			var sorted =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 orderby j.Value1 + 1, e.Id
				 select e.Id)
				.ToArray();

			sorted.ShouldBe([3, 2, 1]);
		}

		// A grouping set's key is structure: the projection reads it as the grouping made it. ROLLUP adds the grand total, whose
		// key is NULL as well; the missed row's key and the total's both read 0.
		[Test]
		public void GroupingSetKeyOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL95Plus, TestProvName.AllMySql, TestProvName.AllDuckDB)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var query =
				from e in partial
				from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				group e by Sql.GroupBy.Rollup(new { K = j.Value1 + 1 }) into g
				select new { g.Key.K, Count = g.Count() };

			var rows = query
				.ToArray()
				.OrderBy(r => r.K)
				.ThenBy(r => r.Count)
				.Select(r => (r.K, r.Count))
				.ToArray();

			rows.ShouldBe([(-4, 1), (0, 1), (0, 3), (1, 1)]);
			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
		}

		// A calculation of an inner projection that the final projection reads through a wrapper is read as the reader reads it,
		// as the option reads it on the client. A DISTINCT keeps it in SQL in both arms: its list is built with the sequence.
		[Test]
		public void CalculationReadThroughWrapperOverMissedLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var inner =
				from e in partial
				from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				select new { e.Id, X = j.Value1 + 1 };

			inner
				.OrderBy(y => y.Id)
				.Select(y => y.X)
				.ToArray()
				.ShouldBe([1, -4, 1]);

			inner
				.OrderBy(y => y.Id)
				.Take(10)
				.Select(y => y.X)
				.ToArray()
				.ShouldBe([1, -4, 1]);

			inner
				.OrderBy(y => y.Id)
				.Skip(0)
				.Select(y => y.X)
				.ToArray()
				.ShouldBe([1, -4, 1]);

			inner
				.AsSubQuery()
				.OrderBy(y => y.Id)
				.Select(y => y.X)
				.ToArray()
				.ShouldBe([1, -4, 1]);

			inner
				.Distinct()
				.OrderBy(y => y.Id)
				.Select(y => y.X)
				.ToArray()
				.ShouldBe([1, -4, 0]);

			// Over a DISTINCT's column a calculation reads the column as the reader does, 0 for the missed row.
			inner
				.Distinct()
				.OrderBy(y => y.Id)
				.Select(y => y.X + 1)
				.ToArray()
				.ShouldBe([2, -3, 1]);
		}

		// A value handed through a wrapper is read both by a calculation and by the filter of a correlated subquery: the
		// calculation reads the reader's default, the filter keeps the SQL NULL, whichever of them is built first.
		[Test]
		public void CalculationAndCorrelatedFilterThroughWrapperOverMissedLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);
			using var keyed   = db.CreateLocalTable(KeyedEntity.Seed);

			var inner =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 select new { e.Id, X = j.Value1 + 1 })
				.AsSubQuery();

			var calculationFirst =
				(from y in inner
				 orderby y.Id
				 select new { A = y.X + 1, N = keyed.Count(k => k.Id == y.X) })
				.ToArray();

			var filterFirst =
				(from y in inner
				 orderby y.Id
				 select new { N = keyed.Count(k => k.Id == y.X), A = y.X + 1 })
				.ToArray();

			calculationFirst.Select(r => r.A).ShouldBe([2, -3, 2]);
			calculationFirst.Select(r => r.N).ShouldBe([1, 0, 0]);
			filterFirst     .Select(r => r.A).ShouldBe([2, -3, 2]);
			filterFirst     .Select(r => r.N).ShouldBe([1, 0, 0]);
		}

		// A projected calculation that equals the sort key reuses the key's column and keeps its value; the ORDER BY is
		// rendered as it is without the projection.
		[Test]
		public void CalculationEqualToSortKeyOverMissedLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var query =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 orderby j.Value1 + 1
				 select j.Value1 + 1)
				.Distinct();

			query.ToArray().Length.ShouldBe(3);
			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
		}

		// A predicate and the projection translate the same member: the predicate keeps the SQL NULL, the projection reads the
		// default, and neither is served the other's translation.
		[Test]
		public void TranslationSharedByPredicateAndProjectionOverMissedLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var rows =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 where j.Date.Year > 0 || e.Id == 1
				 select new { e.Id, Year = j.Date.Year })
				.ToArray();

			rows.Length.ShouldBe(1);
			rows[0].Id.ShouldBe(1);
			rows[0].Year.ShouldBe(MissedRowYear(context));
		}

		// A member translated inside a conversion to a nullable type and the same member calculated with are two translations:
		// the conversion keeps the NULL, the calculation reads the default, whichever of them is built first.
		[Test]
		public void NullableConversionAndCalculationOfOneMemberOverMissedLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var conversionFirst =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new { N = (int?)j.Date.Year, P = j.Date.Year + 1 })
				.ToArray();

			var calculationFirst =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new { P = j.Date.Year + 1, N = (int?)j.Date.Year })
				.ToArray();

			var year = MissedRowYear(context);

			conversionFirst .Length.ShouldBe(MissedJoinEntity.Seed.Length);
			conversionFirst .ShouldAllBe(r => r.N == null && r.P == year + 1);
			calculationFirst.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			calculationFirst.ShouldAllBe(r => r.N == null && r.P == year + 1);
		}

		// A calculation a predicate and the projection share, over no outer join, is translated once: the projection reads the
		// predicate's column in both arms. Math.Round declines a translation of its own for the projection, so it would move to
		// .NET if the projection translated it apart.
		[Test]
		public void CalculationSharedWithPredicateKeepsOneTranslation([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var query =
				from t in (from e in table select Math.Round(e.Value1 / 4.0))
				where t != 0
				select t;

			AssertScalarQuery(query);

			var sql = query.ToSqlQuery().Sql.ToUpperInvariant();

			sql.IndexOf("ROUND", StringComparison.Ordinal).ShouldBeLessThan(sql.IndexOf("WHERE", StringComparison.Ordinal));
		}

		// The shared calculation holds a correlated subquery whose filter reads the missed row. The filter is structure, which
		// the reader's rule never reads, so the calculation is shared as it is when the filter reads the matched row instead.
		[Test]
		public void CalculationWithCorrelatedSubquerySharedWithPredicate([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			// Joined on a column that is not a key, so the control keeps the join it does not read.
			var query =
				from t in
					from e in table
					from j in table.LeftJoin(j => j.Value1 == e.Value1 + 1000)
					select Math.Round((e.Value1 + (table.Any(c => c.Id == j.Id) ? 1 : 0)) / 4.0)
				where t != 0
				select t;

			var overMatchedRow =
				from t in
					from e in table
					from j in table.LeftJoin(j => j.Value1 == e.Value1 + 1000)
					select Math.Round((e.Value1 + (table.Any(c => c.Id == e.Id) ? 1 : 0)) / 4.0)
				where t != 0
				select t;

			AssertScalarQuery(query);

			query.ToSqlQuery().Sql.ShouldBe(overMatchedRow.ToSqlQuery().Sql.Replace("[c_1].[Id] = [e].[Id]", "[c_1].[Id] = [j].[Id]"));
		}

		// A correlated count is joined as a source that always has its one row, and a count is never NULL: the calculation
		// reads it as it is, so the projection shares the predicate's translation.
		[Test]
		public void CalculationWithCorrelatedCountSharedWithPredicate([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var query =
				from t in
					from e in table
					from j in table.LeftJoin(j => j.Id == e.Id + 1000)
					select Math.Round((e.Value1 + table.Count(c => c.Id == j.Id)) / 4.0)
				where t != 0
				select t;

			AssertScalarQuery(query);

			var sql = query.ToSqlQuery().Sql.ToUpperInvariant();

			sql.IndexOf("ROUND", StringComparison.Ordinal).ShouldBeLessThan(sql.IndexOf("WHERE", StringComparison.Ordinal));
		}

		// A correlated sum over no rows is NULL in SQL and 0 in .NET. Its source always has its one row, but the sum stays
		// nullable, so the default it is read as survives.
		[Test]
		public void CalculationWithCorrelatedSumOverMissedLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, X = e.Value1 + table.Where(c => c.Id == j.Id).Sum(c => c.Value1) };

			AssertQuery(query);
		}

		// A wrapper's column that cannot be NULL, although its calculation reads a missed row: a predicate reads the column first,
		// and the projection's calculation still reads the missed row's default rather than the predicate's column.
		[Test]
		public void CalculationOverNonNullableWrapperColumnOverMissedLeftJoin([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var inner =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new { e.Id, X = j.Value1 < 5 ? 1 : 2 })
				.AsSubQuery();

			var values =
				(from y in inner
				 where y.X > 0
				 orderby y.Id
				 select y.X + 1)
				.ToArray();

			values.ShouldBe([2, 2]);
		}

		// A LEFT JOIN inside a scalar subquery: the subquery's calculation and a calculation over the subquery's value both read
		// the missed row as the reader does.
		[Test]
		public void ScalarSubqueryOverMissedLeftJoinReadsDefault([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var query =
				from e in table
				select new
				{
					e.Id,
					Inner = (from t in table from k in table.LeftJoin(k => k.Id == t.Id + 1000) where t.Id == e.Id select k.Value1 + 1).FirstOrDefault(),
					Outer = (from t in table from k in table.LeftJoin(k => k.Id == t.Id + 1000) where t.Id == e.Id select k.Value1).FirstOrDefault() + 1,
				};

			AssertQuery(query);
		}

		// Membership in a subquery is calculated with as membership in a list is: the missed row's value reads as default(int),
		// which the subquery holds. As a filter the membership keeps the SQL NULL and drops the missed row.
		[Test]
		public void SubqueryContainsOverMissedLeftJoinReadsDefault([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var query =
				from e in partial
				from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				select new { e.Id, Held = partial.Select(p => p.Value1).Contains(j.Value1) ? "y" : "n" };

			AssertQuery(query);

			var filtered =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 where partial.Select(p => p.Value1).Contains(j.Value1)
				 orderby e.Id
				 select e.Id)
				.ToArray();

			filtered.ShouldBe([1, 2]);
		}

		// A setter's value is written, not read by the reader: an update from a missed row writes the NULL SQL computes, whether
		// the value comes from the joined row or from a scalar subquery that finds no row.
		[Test]
		public void UpdateSettersOverMissedLeftJoinKeepNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var joined =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e, j };

			joined.Update(q => q.e, q => new MissedJoinEntity { Name = q.j.Value1.ToString() });

			table.Select(e => e.Name).ToArray().ShouldAllBe(name => name == null);

			table
				.Set(e => e.Name, e => "x")
				.Update();

			table
				.Set(e => e.Name, e => (table.Where(t => t.Id == e.Id + 1000).Select(t => t.Value1).FirstOrDefault() + 1).ToString())
				.Update();

			table.Select(e => e.Name).ToArray().ShouldAllBe(name => name == null);
		}

		[Table]
		sealed class CharJoinEntity
		{
			[Column, PrimaryKey] public int  Id   { get; set; }
			[Column]             public char Code { get; set; }

			public static readonly CharJoinEntity[] Seed =
			[
				new() { Id = 1, Code = 'A' },
				new() { Id = 2, Code = 'B' },
			];
		}

		// A char has no default every engine holds - PostgreSQL keeps no NUL in a string - so a missed row's char is not read as
		// default(char), and a calculation with it keeps the SQL NULL.
		[Test]
		public void CharOverMissedLeftJoinIsNotDefaulted([IncludeDataSources(true, TestProvName.AllSQLite, TestProvName.AllPostgreSQL)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(CharJoinEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new { e.Id, IsA = j.Code == 'A' ? 1 : 0 };

			AssertQuery(query);

			query.ToSqlQuery().Sql.ToUpperInvariant().ShouldNotContain("COALESCE");
		}

		// An entity comparison compares identities: a missed row is not the row keyed 0.
		[Test]
		public void EntityComparisonOverMissedLeftJoinComparesIdentities([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(KeyedEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				from k in table.LeftJoin(k => k.Id == e.Id - 1)
				select new { e.Id, Same = j == k ? 1 : 0 };

			AssertQuery(query);
		}

		// An operand written as a nullable type keeps the NULL: a lifted operator and a lifted comparison.
		[Test]
		public void NullableOperandOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var query =
				from e in table
				from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				select new
				{
					e.Id,
					Lifted        = (int?)j.Value1 + 1,
					LiftedCompare = (int?)j.Value1 < 5 ? "a" : "b",
				};

			AssertQuery(query);
		}

		// An aggregate learns from the NULL that a row has no value: the missed row is not counted and not averaged as 0.
		[Test]
		public void AggregateOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table   = db.CreateLocalTable(MissedJoinEntity.Seed);
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var average =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 group j by e.Grp into g
				 select g.Average(x => x.Value1 + 0))
				.Single();

			average.ShouldBe(-2.5);

			var counts =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 group j by e.Id into g
				 select new
				 {
					 Function  = CountValues(g, x => x.Value1 + 1),
					 Extension = CountValuesExtension(g, x => x.Value1 + 1),
					 SumPlus   = g.Sum(x => x.Value1) + 1,
					 AvgPlus   = g.Average(x => x.Value1) + 1,
				 })
				.ToArray();

			counts.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			counts.ShouldAllBe(c => c.Function == 0 && c.Extension == 0 && c.SumPlus == 1 && c.AvgPlus == 1);
		}

		// A window function's argument and partition keep the NULL.
		[Test]
		public void WindowFunctionOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db      = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table   = db.CreateLocalTable(MissedJoinEntity.Seed);
			using var partial = db.CreateLocalTable(PartialJoinEntity.Seed);

			var counts =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select Sql.Window.Count(j.Value1 + 1, w => w.PartitionBy(e.Id)))
				.ToArray();

			counts.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			counts.ShouldAllBe(c => c == 0);

			var numbers =
				(from e in partial
				 from j in partial.LeftJoin(j => j.Id == e.Id + 1)
				 select Sql.Window.RowNumber(w => w.PartitionBy(j.Value1 + 1).OrderBy(e.Id)))
				.ToArray();

			numbers.Length.ShouldBe(PartialJoinEntity.Seed.Length);
			numbers.ShouldAllBe(n => n == 1);
		}

		[Test]
		public void CalculationInClassProjectionOverMissedLeftJoinReadsDefault([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var dtos =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new CalculatedDto { Id = e.Id, X = j.Value1 + 1 })
				.ToArray();

			dtos.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			dtos.ShouldAllBe(d => d.X == 1);

			var nested =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new { e.Id, Inner = new { X = j.Value1 + 1 } })
				.ToArray();

			nested.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			nested.ShouldAllBe(n => n.Inner.X == 1);
		}

		// An aggregate over an inline array is a set function: SQL's MIN skips the missed row's NULL, which C# would read as 0.
		[Test]
		public void InlineArrayAggregateOverMissedLeftJoinKeepsNull([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var rows =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new
				 {
					 e.Value1,
					 e.Name,
					 Min    = new[] { j.Value1, e.Value1 }.Min(),
					 Joined = string.Join(",", new[] { j.Name, e.Name }),
				 })
				.ToArray();

			rows.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			rows.ShouldAllBe(r => r.Min == r.Value1 && r.Joined == "," + r.Name);
		}

		// An inline array passed to a params parameter is a list of row values, not a set: the string functions read its items
		// as the reader reads them.
		[Test]
		public void InlineParamsListOverMissedLeftJoinReadsDefault([IncludeDataSources(true, TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(MissedJoinEntity.Seed);

			var rows =
				(from e in table
				 from j in table.LeftJoin(j => j.Id == e.Id + 1000)
				 select new
				 {
					 e.Value1,
					 e.Name,
					 Calculated = string.Join(",", new[] { (j.Value1 + 1).ToString(), e.Name }),
					 Joined     = string.Join(",", new object[] { j.Value1, e.Value1 }),
					 Concat     = string.Concat(new object[] { "<", j.Value1, ",", e.Value1, ">" }),
				 })
				.ToArray();

			rows.Length.ShouldBe(MissedJoinEntity.Seed.Length);
			rows.ShouldAllBe(r => r.Calculated == "1," + r.Name);
			rows.ShouldAllBe(r => r.Joined     == "0," + r.Value1);
			rows.ShouldAllBe(r => r.Concat     == "<0," + r.Value1 + ">");
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
			AssertScalarQuery(from e in table select e.Id > 1 ? e.Value1 : e.Value2);
			AssertScalarQuery(from e in table select -e.Value1);
			AssertScalarQuery(from e in table select e.Value1 + PreferServer(e.Value2));

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
			AssertScalarQuery(table.GroupBy(e => ClientOnlyDouble(1)).Select(g => g.Count()));                                 // GROUP BY
			AssertScalarQuery(table.GroupBy(e => e.Id).Where(g => g.Count() > ClientOnlyDouble(0)).Select(g => g.Key));        // HAVING
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

			// Nothing here can be NULL, so nothing is defaulted: the same one column, read as it is, in both arms.
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
			// SQL in both arms - moving it to the client turns a query that returns a value into one that throws.
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
			// The mapping itself is linq2db#5927; until that is fixed the comparison must stay in SQL.
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

			// #5925: a member translation, which the option never moves, so the whole predicate stays in SQL.
			AllRaw(from e in table select new { e.Id, V = string.IsNullOrWhiteSpace(e.Name) }).ShouldBeFalse();
		}

		[Test]
		public void BooleanMethodStaysInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// string.IsNullOrWhiteSpace is a member translation, which the option does not move (linq2db#5925 tracks how
			// bool-returning members are routed). Results are correct either way, so AssertQuery holds in both modes - only
			// the SQL shape differs.
			var query = from e in table select new { e.Id, Ws = string.IsNullOrWhiteSpace(e.Name) };

			AssertQuery(query);

			query.GetSelectQuery().Select.Columns.Any(c => c.Expression is not SqlField).ShouldBeTrue();
		}

		[Test]
		public void MandatoryMembersStayInSql([IncludeDataSources(TestProvName.AllSQLite)] string context, [Values] bool preferClient)
		{
			using var db    = GetDataContext(context, o => o.UsePreferClientCalculation(preferClient));
			using var table = db.CreateLocalTable(BatchCalcEntity.Seed);

			// Sql.* marks intent to compute server-side, so none of these may be pulled client-side whatever the option
			// says. The DateAdd increment is a column, not a constant - with
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
			// moving any single one of them to the client by mistake would leave the test green.
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
			// client-side equivalent - moving them to the client would leave the grouping in the projection.
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
