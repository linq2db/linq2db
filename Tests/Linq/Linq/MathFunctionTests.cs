using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	[TestFixture]
	public class MathFunctionTests : TestBase
	{
		[Test]
		public void Abs([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Abs(p.MoneyValue) where t > 0 select t,
				from t in from p in db.Types select Math.Abs(p.MoneyValue) where t > 0 select t);
		}

		[Test]
		public void Acos([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Asin([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Atan([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Atan2([DataSources(TestProvName.AllAccess)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Ceiling1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t,
				from t in from p in db.Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t);
		}

		[Test]
		public void Ceiling2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Ceiling(p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Ceiling(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Cos([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Cosh([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Cot([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15)!.Value * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15)!.Value * 15) where t != 0.1 select t);
		}

		[Test]
		public void Degrees1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				t => t,
				from t in from p in Types select Math.Floor(Sql.Degrees(p.MoneyValue)!.Value) where t != 0.1m select t,
				from t in from p in db.Types select Math.Floor(Sql.Degrees(p.MoneyValue)!.Value) where t != 0.1m select t,
				comparer: DecimalComparerInstance);
		}

		[Test]
		public void Degrees2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Sql.Degrees((double)p.MoneyValue)!.Value where t != 0.1 select Math.Floor(t),
				from t in from p in db.Types select Sql.Degrees((double)p.MoneyValue)!.Value where t != 0.1 select Math.Floor(t));
		}

		[Test]
		public void Degrees3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Sql.Degrees((int)p.MoneyValue)!.Value where t != 0.1 select t,
				from t in from p in db.Types select Sql.Degrees((int)p.MoneyValue)!.Value where t != 0.1 select t);
		}

		sealed class DegreesValue
		{
			[PrimaryKey] public int     Id { get; set; }
			[Column    ] public decimal M  { get; set; }
		}

		// Sql.Degrees(int) returns int, so widened back to double it has no fraction. The bound is not integral: an integral
		// constant lets the comparison drop the widening before SQL is built (#6070).
		// These providers emulate Degrees inside an integer cast pair; Access emulates it too but drops the casts (#6070).
		// Elsewhere the engine's DEGREES decides: SQLite and DuckDB keep the fraction (#6072), SQL Server returns int.
		[Test]
		public void DegreesOfIntTruncates([IncludeDataSources(true, TestProvName.AllFirebird, TestProvName.AllOracle, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllSybase)] string context)
		{
			var data = new[]
			{
				new DegreesValue { Id = 1, M = 1.5m },
				new DegreesValue { Id = 2, M = 2.5m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from d in t.Select(r => Sql.Degrees((int)r.M)!.Value)
				where d < 57.1
				select d;

			AssertQuery(query);

			// 114.59 truncates to 114 but rounds to 115
			var past =
				from d in t.Select(r => Sql.Degrees((int)r.M)!.Value)
				where d < 114.5
				select d;

			AssertQuery(past);
		}

		[Test]
		public void Exp([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Exp((double)p.MoneyValue)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Exp((double)p.MoneyValue)) where t != 0.1 select t);
		}

		[Test]
		public void Floor([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(-(p.MoneyValue + 1)) where t != 0 select t,
				from t in from p in db.Types select Math.Floor(-(p.MoneyValue + 1)) where t != 0 select t);
		}

		[Test]
		public void Log([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t);
		}

		[Test]
		public void Log2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t);
		}

		[Test]
		public void Log10([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Log10((double)p.MoneyValue)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Log10((double)p.MoneyValue)) where t != 0.1 select t);
		}

		[Test]
		public void Max([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Max(p.MoneyValue, 5.1m) where t != 0 select t,
				from t in from p in db.Types select Math.Max(p.MoneyValue, 5.1m) where t != 0 select t);
		}

		[Test]
		public void Min([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Min(p.MoneyValue, 5) where t != 0 select t,
				from t in from p in db.Types select Math.Min(p.MoneyValue, 5) where t != 0 select t);
		}

		[Test]
		public void Pow([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t,
				from t in from p in db.Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t);
		}

		// Sybase: https://stackoverflow.com/questions/25281843
		[Test]
		public void PowDecimal([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Sql.Power(p.MoneyValue, 3)!.Value) where t != 0 select t,
				from t in from p in db.Types select Math.Floor(Sql.Power(p.MoneyValue, 3)!.Value) where t != 0 select t);
		}

		[Test]
		public void Round1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round(p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Round2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round((double)p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Round((double)p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Round3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = from t in from p in db.Types select Math.Round(p.MoneyValue, 1) where t != 0 && t != 7 select t;

			if (context.IsAnyOf(ProviderName.DB2))
				q = q.AsQueryable().Select(t => Math.Round(t, 1));

			AreEqual(
				from t in from p in Types select Math.Round(p.MoneyValue, 1) where t != 0 && t != 7 select t,
				q);
		}

		[Test]
		public void Round4([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5),
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5));
		}

		[ActiveIssue(Configuration = TestProvName.AllDB2, ErrorMessage = "Assert.That(exceptExpected, Is.Zero)",
			Details = "no-issue: DB2's ROUND on a Float is not exact - ROUND(CAST(6.6 AS Float), 5) returns 6.6000000000000005, one ulp above .NET's. Two of the twelve rows differ that way. Not linq2db's banker's-rounding emulation: its midpoint branch is never taken for these values, so the emitted CASE reduces to a plain ROUND.")]
		[Test]
		public void Round4Sql([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5),
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Sql.AsSql(Math.Round(t, 5)));
		}

		[Test]
		public void Round5([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		// ClickHouse: AwayFromZero rounding supported only for decimals. Double use bankers rounding (Round5 test)
		[Test]
		public void Round6([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		[Test]
		public void Round7([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t);
		}

		[Test]
		public void Round8([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t);
		}

		[Test]
		public void Round9([DataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round(p.MoneyValue, 1, MidpointRounding.AwayFromZero) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, 1, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		[Test]
		public void Round10([DataSources] string context)
		{
			using var db = GetDataContext(context);
			var q = from t in from p in db.Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 && t != 7 select t;

			if (context.IsAnyOf(ProviderName.DB2))
				q = q.AsQueryable().Select(t => Math.Round(t, 1, MidpointRounding.ToEven));

			AreEqual(
				from t in from p in Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 && t != 7 select t,
				q);
		}

		[Test]
		public void Round11([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5),
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5));
		}

		// TODO: implement other MidpointRounding values (and remove NUnit4001 suppress)
		[Test, QueryCacheTest]
#pragma warning disable NUnit4001
		public void Round12([DataSources(TestProvName.AllSQLite)] string context, [Values(MidpointRounding.AwayFromZero, MidpointRounding.ToEven)] MidpointRounding mp, [Values(1, 2)] int iteration)
#pragma warning restore NUnit4001
		{

			using var db = GetDataContext(context);
			var q = from t in from p in db.Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 && t != 7 select t;

			if (context.IsAnyOf(ProviderName.DB2))
				q = q.AsQueryable().Select(t => Math.Round(t, 1, mp));

			var cacheMissCount = q.GetCacheMissCount();

			AreEqual(
				from t in from p in Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 && t != 7 select t,
				q);

			if (iteration > 1)
				q.GetCacheMissCount().ShouldBe(cacheMissCount);
		}

		// AssertQuery's default comparer compares a decimal's public members, Scale included, so 2.5 <> 2.50.
		static readonly IEqualityComparer<decimal>  ByValue         = EqualityComparer<decimal>.Default;
		static readonly IEqualityComparer<decimal?> ByNullableValue = EqualityComparer<decimal?>.Default;

		// Rounds server-side to more digits than the column's declared scale, a no-op that a provider emulating
		// ROUND by scaling must not overflow on: MoneyValue is Decimal(6,2) on YDB, and 11.45 * 10^5 does not fit that.
		[Test]
		public void Round13([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var query =
				from p in db.Types
				where p.MoneyValue != 0
				orderby p.ID
				select Sql.AsSql(Math.Round(p.MoneyValue, 5));

			AssertQuery(query, ByValue);
		}

		// The widened intermediate must not leak into the result type: YQL arithmetic and IF reject
		// Decimal operands of different types.
		[Test]
		public void Round14([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var sum =
				from p in db.Types
				where p.MoneyValue != 0
				orderby p.ID
				select Sql.AsSql(Math.Round(p.MoneyValue, 5) + p.MoneyValue);

			AssertQuery(sum, ByValue);

			var condition =
				from p in db.Types
				where p.MoneyValue != 0
				orderby p.ID
				select Sql.AsSql(p.ID > 2 ? Math.Round(p.MoneyValue, 5) : p.MoneyValue);

			AssertQuery(condition, ByValue);
		}

		[Test]
		public void Round15([DataSources] string context)
		{
			using var db = GetDataContext(context);

			var away =
				from p in db.Types
				where p.MoneyValue != 0
				orderby p.ID
				select Sql.AsSql(Sql.Round(p.MoneyValue, 5) + p.MoneyValue);

			AssertQuery(away, ByNullableValue);

			var even =
				from p in db.Types
				where p.MoneyValue != 0
				orderby p.ID
				select Sql.AsSql(Sql.RoundToEven(p.MoneyValue, 5) + p.MoneyValue);

			AssertQuery(even, ByNullableValue);
		}

		// A non-constant precision must not cost the value its fractional digits.
		// DuckDB rejects ROUND(DECIMAL, INTEGER) with a non-constant precision.
		[Test]
		public void Round16([DataSources(TestProvName.AllDuckDB)] string context)
		{
			using var db = GetDataContext(context);

			var atOrAboveScale =
				from p in db.Types
				where p.MoneyValue != 0
				orderby p.ID
				select Sql.AsSql(Math.Round(p.MoneyValue, p.ID % 2 + 2));

			AssertQuery(atOrAboveScale, ByValue);

			var belowScale =
				from p in db.Types
				where p.MoneyValue != 0
				orderby p.ID
				select Sql.AsSql(Math.Round(p.MoneyValue, p.ID % 2 + 1));

			AssertQuery(belowScale, ByValue);
		}

		sealed class RoundNearLimit
		{
			[PrimaryKey                       ] public int     Id    { get; set; }
			[Column(Precision = 34, Scale = 2)] public decimal D34s2 { get; set; }
			[Column(Precision = 35, Scale = 2)] public decimal D35s2 { get; set; }
			[Column(Precision = 35, Scale = 6)] public decimal D35s6 { get; set; }
			[Column(Precision =  4, Scale = 4)] public decimal D4s4  { get; set; }
		}

		// Near the 35-digit limit the widened intermediate must not cost the value its fractional digits,
		// and a type with no integer digits still needs one for 10^p.
		[Test]
		public void Round17([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundNearLimit { Id = 1, D34s2 =  1.75m, D35s2 =  1.75m, D35s6 =  1.114951m, D4s4 =  0.1234m },
				new RoundNearLimit { Id = 2, D34s2 = -2.35m, D35s2 = -2.35m, D35s6 =  1.125001m, D4s4 =  0.1250m },
				new RoundNearLimit { Id = 3, D34s2 = 11.45m, D35s2 = 11.45m, D35s6 = -1.114951m, D4s4 = -0.1250m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					R34 = Sql.AsSql(Math.Round(r.D34s2, 2, mp)),
					R35 = Sql.AsSql(Math.Round(r.D35s2, 5, mp)),
					R6  = Sql.AsSql(Math.Round(r.D35s6, 2, mp)),
					R4  = Sql.AsSql(Math.Round(r.D4s4,  2, mp)),
				};

			AssertQuery(query);
		}

		sealed class RoundNegative
		{
			[PrimaryKey                      ] public int     Id { get; set; }
			[Column(Precision = 6, Scale = 2)] public decimal D  { get; set; }
		}

		// A negative precision rounds left of the point: the intermediate keeps the integer digits and gains scale.
		[Test]
		public void Round18([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new RoundNegative { Id = 1, D =  1234.56m },
				new RoundNegative { Id = 2, D =  1234.96m },
				new RoundNegative { Id = 3, D = -1234.96m },
				new RoundNegative { Id = 4, D =  1225.00m },
				new RoundNegative { Id = 5, D = -1225.00m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var result = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Away = Sql.AsSql(Sql.Round(r.D, -1)),
					Even = Sql.AsSql(Sql.RoundToEven(r.D, -1)),
				})
				.ToArray();

			result
				.Select(r => r.Away)
				.ShouldBe(new decimal?[] { 1230m, 1230m, -1230m, 1230m, -1230m });
			result
				.Select(r => r.Even)
				.ShouldBe(new decimal?[] { 1230m, 1230m, -1230m, 1220m, -1220m });
		}

		// A precision below the scale still needs a digit more than the type has: 1234.56 * 10^1 does not fit Decimal(6,2).
		[Test]
		public void Round21([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundNegative { Id = 1, D =  1234.56m },
				new RoundNegative { Id = 2, D = -1234.55m },
				new RoundNegative { Id = 3, D =  1000.05m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					R    = Sql.AsSql(Math.Round(r.D, 1, mp)),
					Sum  = Sql.AsSql(Math.Round(r.D, 1, mp) + r.D),
					Cond = Sql.AsSql(r.Id > 2 ? Math.Round(r.D, 1, mp) : r.D),
				};

			AssertQuery(query);
		}

		sealed class RoundNegativeNearLimit
		{
			[PrimaryKey                       ] public int     Id { get; set; }
			[Column(Precision = 35, Scale = 2)] public decimal D  { get; set; }
		}

		// A negative precision on a type with no spare digits must still gain the scale 10^p needs.
		[Test]
		public void Round19([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new RoundNegativeNearLimit { Id = 1, D =  1234.56m },
				new RoundNegativeNearLimit { Id = 2, D =  1499.99m },
				new RoundNegativeNearLimit { Id = 3, D =  2500.00m },
				new RoundNegativeNearLimit { Id = 4, D = -2500.00m },
				new RoundNegativeNearLimit { Id = 5, D =  3500.00m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var result = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Away = Sql.AsSql(Sql.Round(r.D, -3)),
					Even = Sql.AsSql(Sql.RoundToEven(r.D, -3)),
				})
				.ToArray();

			result
				.Select(r => r.Away)
				.ShouldBe(new decimal?[] { 1000m, 1000m, 3000m, -3000m, 4000m });
			result
				.Select(r => r.Even)
				.ShouldBe(new decimal?[] { 1000m, 1000m, 2000m, -2000m, 4000m });
		}

		sealed class RoundNegativeDynamic
		{
			[PrimaryKey                      ] public int     Id { get; set; }
			[Column(Precision = 6, Scale = 2)] public decimal D  { get; set; }
			[Column                          ] public int     P  { get; set; }
		}

		// A negative precision that is not a constant must still get the scale 10^p needs.
		[Test]
		public void Round20([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new RoundNegativeDynamic { Id = 1, D =  1234.56m, P = -1 },
				new RoundNegativeDynamic { Id = 2, D =  1249.60m, P = -2 },
				new RoundNegativeDynamic { Id = 3, D =  1234.96m, P = -1 },
				new RoundNegativeDynamic { Id = 4, D =  1225.00m, P = -1 },
				new RoundNegativeDynamic { Id = 5, D =  1234.56m, P = -3 },
				new RoundNegativeDynamic { Id = 6, D = -2500.00m, P = -3 },
				new RoundNegativeDynamic { Id = 7, D =  4999.99m, P = -4 },
				new RoundNegativeDynamic { Id = 8, D =  9999.99m, P =  3 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var result = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Away = Sql.AsSql(Sql.Round(r.D, r.P)),
					Even = Sql.AsSql(Sql.RoundToEven(r.D, r.P)),
				})
				.ToArray();

			result
				.Select(r => r.Away)
				.ShouldBe(new decimal?[] { 1230m, 1200m, 1230m, 1230m, 1000m, -3000m, 0m, 9999.99m });
			result
				.Select(r => r.Even)
				.ShouldBe(new decimal?[] { 1230m, 1200m, 1230m, 1220m, 1000m, -2000m, 0m, 9999.99m });
		}

		sealed class RoundDynamicNoIntegerDigits
		{
			[PrimaryKey                      ] public int     Id { get; set; }
			[Column(Precision = 4, Scale = 4)] public decimal D  { get; set; }
			[Column                          ] public int     P  { get; set; }
		}

		// A non-constant precision may reach the source scale, and 10^scale needs an integer digit the type lacks.
		[Test]
		public void Round22([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new RoundDynamicNoIntegerDigits { Id = 1, D =  0.1234m, P =  4 },
				new RoundDynamicNoIntegerDigits { Id = 2, D =  0.1250m, P =  5 },
				new RoundDynamicNoIntegerDigits { Id = 3, D =  0.1250m, P =  2 },
				new RoundDynamicNoIntegerDigits { Id = 4, D = -0.1235m, P =  3 },
				new RoundDynamicNoIntegerDigits { Id = 5, D =  0.1234m, P = -1 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var result = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Away = Sql.AsSql(Sql.Round(r.D, r.P)),
					Even = Sql.AsSql(Sql.RoundToEven(r.D, r.P)),
				})
				.ToArray();

			result
				.Select(r => r.Away)
				.ShouldBe(new decimal?[] { 0.1234m, 0.125m, 0.13m, -0.124m, 0m });
			result
				.Select(r => r.Even)
				.ShouldBe(new decimal?[] { 0.1234m, 0.125m, 0.12m, -0.124m, 0m });
		}

		sealed class RoundNegativeDynamicWide
		{
			[PrimaryKey                        ] public int     Id    { get; set; }
			[Column(Precision = 22, Scale =  9)] public decimal D22s9 { get; set; }
			[Column(Precision = 35, Scale =  2)] public decimal D35s2 { get; set; }
			[Column                            ] public int     P     { get; set; }
		}

		// A wide type has no room for both the integer digits a positive precision needs and the scale a negative
		// one needs: a negative precision gets a type of its own, and one no type can hold fails instead of misrounding.
		[Test]
		public void Round23([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new RoundNegativeDynamicWide { Id = 1, D22s9 =  149999.999999999m, D35s2 = 1234.96m, P =  -5 },
				new RoundNegativeDynamicWide { Id = 2, D22s9 = -250000.000000000m, D35s2 = 1234.96m, P =  -5 },
				new RoundNegativeDynamicWide { Id = 3, D22s9 = 14999999999.5m,     D35s2 = 1234.96m, P = -10 },
				new RoundNegativeDynamicWide { Id = 4, D22s9 = 1234567.891m,       D35s2 = 1234.96m, P = -13 },
				new RoundNegativeDynamicWide { Id = 5, D22s9 = 1234.565m,          D35s2 = 1234.96m, P =   2 },
				new RoundNegativeDynamicWide { Id = 6, D22s9 = 1234.565m,          D35s2 = 1234.96m, P =   0 },
				new RoundNegativeDynamicWide { Id = 7, D22s9 = 12.5m,              D35s2 = 1234.96m, P =  12 },
				new RoundNegativeDynamicWide { Id = 8, D22s9 = 1000000000000.5m,   D35s2 = 1234.96m, P = -14 },
				new RoundNegativeDynamicWide { Id = 9, D22s9 = -9999999999999.5m,  D35s2 = 1234.96m, P = -20 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var result = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Away = Sql.AsSql(Sql.Round(r.D22s9, r.P)),
					Even = Sql.AsSql(Sql.RoundToEven(r.D22s9, r.P)),
				})
				.ToArray();

			result
				.Select(r => r.Away)
				.ShouldBe(new decimal?[] { 100000m, -300000m, 10000000000m, 0m, 1234.57m, 1235m, 12.5m, 0m, 0m });
			result
				.Select(r => r.Even)
				.ShouldBe(new decimal?[] { 100000m, -200000m, 10000000000m, 0m, 1234.56m, 1235m, 12.5m, 0m, 0m });

			var nullPrecision = t
				.Select(r => Sql.AsSql(Sql.Round(r.D22s9, r.Id > 0 ? null : r.P)))
				.ToArray();

			nullPrecision.ShouldAllBe(v => v == null);

			Action act = () => t
				.Where(r => r.P < 0)
				.Select(r => Sql.AsSql(Sql.Round(r.D35s2, r.P)))
				.ToArray();
			act
				.ShouldThrow<Exception>()
				.Message
				.ShouldContain("ROUND precision is out of range for Decimal(35,2)");

			// at exactly -(integer digits) the answer is not 0: 5·10^12 rounds to 10^13, which Decimal(22,9) cannot hold
			using var boundary = db.CreateLocalTable("RoundNegativeDynamicWideBoundary", new[]
			{
				new RoundNegativeDynamicWide { Id = 1, D22s9 = 5000000000000m, D35s2 = 0m, P = -13 },
			});

			Action atDigits = () => boundary
				.Select(r => Sql.AsSql(Sql.Round(r.D22s9, r.P)))
				.ToArray();
			atDigits
				.ShouldThrow<Exception>()
				.Message
				.ShouldContain("Value does not fit into decimal");

			// the constant path's twin: a literal precision does not take the runtime CASE
			Action atDigitsConstant = () => boundary
				.Select(r => Sql.AsSql(Sql.Round(r.D22s9, -13)))
				.ToArray();
			atDigitsConstant
				.ShouldThrow<Exception>()
				.Message
				.ShouldContain("Value does not fit into decimal");

			// below the boundary the same constant path answers 0, so the throw above is the overflow
			var belowDigitsConstant = t
				.Where(r => r.Id < 9)
				.Select(r => Sql.AsSql(Sql.Round(r.D22s9, -13)))
				.ToArray();

			belowDigitsConstant.ShouldAllBe(v => v == 0m);
		}

		sealed class RoundManyDigits
		{
			[PrimaryKey                        ] public int      Id { get; set; }
			[Column(Precision = 22, Scale =  9)] public decimal  D  { get; set; }
			[Column(Precision = 22, Scale =  9)] public decimal? N  { get; set; }
			[Column                            ] public int      P  { get; set; }
		}

		static decimal RoundTo(decimal value, int precision, MidpointRounding mp)
		{
			if (precision >= 0)
				return Math.Round(value, precision, mp);

			var factor = (decimal)Math.Pow(10, -precision);

			return Math.Round(value / factor, mp) * factor;
		}

		// A Double carries ~15-17 significant digits, so rounding must not pass a Decimal through one: these values
		// either scale past 2^53 or carry a residue (.4999999) that a Double turns into a tie.
		[Test]
		public void Round24([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundManyDigits { Id =  1, D =    900719925474.09931m,     P =  4 },
				new RoundManyDigits { Id =  2, D =    123456789012.34567m,     P =  5 },
				new RoundManyDigits { Id =  3, D =     12345678901.234999999m, P =  2 },
				new RoundManyDigits { Id =  4, D =     12345678901.225m,       P =  2 },
				new RoundManyDigits { Id =  5, D =     12345678901.235m,       P =  2 },
				new RoundManyDigits { Id =  6, D =    -12345678901.225m,       P =  2 },
				new RoundManyDigits { Id =  7, D =    -12345678901.235m,       P =  2 },
				new RoundManyDigits { Id =  8, D = 1234567849999.999999999m,   P = -5 },
				new RoundManyDigits { Id =  9, D = 1234567949999.999999999m,   P = -5 },
				new RoundManyDigits { Id = 10, D = 1234567890123.499999999m,   P =  0 },
				new RoundManyDigits { Id = 11, D = 1234567890122.5m,           P =  0 },
				new RoundManyDigits { Id = 12, D = 0m,                         P =  2 },
			};

			foreach (var r in data)
				r.N = r.Id == 12 ? null : r.D;

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var expected = new List<(string, int, decimal?)>();
			var actual   = new List<(string, int, decimal?)>();

			expected.AddRange(data
				.Where(r => r.P > 0)
				.OrderBy(r => r.Id)
				.Select(r => ("constant", r.Id, (decimal?)Math.Round(r.D, r.P, mp))));

			// literal precisions: a captured variable would be a parameter and take the runtime path
			var constant = t
				.Where(r => r.P > 0)
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					r.Id,
					R4 = Sql.AsSql(Math.Round(r.D, 4, mp)),
					R5 = Sql.AsSql(Math.Round(r.D, 5, mp)),
					R2 = Sql.AsSql(Math.Round(r.D, 2, mp)),
					r.P,
				})
				.ToArray();

			actual.AddRange(constant.Select(r => ("constant", r.Id, (decimal?)(r.P == 4 ? r.R4 : r.P == 5 ? r.R5 : r.R2))));

			expected.AddRange(data
				.Where(r => r.P == 0)
				.OrderBy(r => r.Id)
				.Select(r => ("no precision", r.Id, (decimal?)Math.Round(r.D, mp))));

			actual.AddRange(t
				.Where(r => r.P == 0)
				.OrderBy(r => r.Id)
				.Select(r => new { r.Id, R = Sql.AsSql(Math.Round(r.D, mp)) })
				.AsEnumerable()
				.Select(r => ("no precision", r.Id, (decimal?)r.R)));

			expected.AddRange(data
				.Where(r => r.Id != 12)
				.OrderBy(r => r.Id)
				.Select(r => ("runtime", r.Id, (decimal?)RoundTo(r.D, r.P, mp))));

			var runtime = t
				.Where(r => r.Id != 12)
				.OrderBy(r => r.Id);

			var runtimeRounded = mp == MidpointRounding.AwayFromZero
				? runtime
					.Select(r => new { r.Id, R = Sql.AsSql(Sql.Round(r.D, r.P)) })
					.ToArray()
				: runtime
					.Select(r => new { r.Id, R = Sql.AsSql(Sql.RoundToEven(r.D, r.P)) })
					.ToArray();

			actual.AddRange(runtimeRounded.Select(r => ("runtime", r.Id, r.R)));

			expected.AddRange(data
				.Where(r => r.P == 2)
				.OrderBy(r => r.Id)
				.Select(r => ("nullable", r.Id, r.N == null ? null : (decimal?)Math.Round(r.N.Value, 2, mp))));

			var nullable = t
				.Where(r => r.P == 2)
				.OrderBy(r => r.Id);

			var nullableRounded = mp == MidpointRounding.AwayFromZero
				? nullable
					.Select(r => new { r.Id, R = Sql.AsSql(Sql.Round(r.N, 2)) })
					.ToArray()
				: nullable
					.Select(r => new { r.Id, R = Sql.AsSql(Sql.RoundToEven(r.N, 2)) })
					.ToArray();

			actual.AddRange(nullableRounded.Select(r => ("nullable", r.Id, r.R)));

			actual.ShouldBe(expected);
		}

		sealed class RoundScaffoldedDbType
		{
			[PrimaryKey                                                   ] public int     Id { get; set; }
			[Column(DbType = "Decimal(6, 2)", Precision = 6, Scale = 2)] public decimal D  { get; set; }
		}

		// The scaffolder emits DbType beside Precision/Scale: the computed CASTs must render their own type, not the DbType.
		[Test]
		public void Round25([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundScaffoldedDbType { Id = 1, D =  1234.56m },
				new RoundScaffoldedDbType { Id = 2, D = -1234.55m },
				new RoundScaffoldedDbType { Id = 3, D =  1000.05m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var stored = t
				.OrderBy(r => r.Id)
				.Select(r => r.D)
				.ToArray();

			var expectedStored = data
				.Select(r => r.D)
				.ToArray();

			stored.ShouldBe(expectedStored, "round trip");

			var rounded = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(Math.Round(r.D, 1, mp)));

			AssertQuery(rounded, ByValue);
		}

		// YQL refuses to cast a Decimal with no integer digits to scale 0: such a value rounds to 0 by comparison, and a
		// result of +-1, which the type cannot hold, fails the query.
		[Test]
		public void Round26([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundDynamicNoIntegerDigits { Id = 1, D =  0.1234m, P = -1 },
				new RoundDynamicNoIntegerDigits { Id = 2, D = -0.4999m, P = -1 },
				new RoundDynamicNoIntegerDigits { Id = 3, D =  0.5000m, P = -1 },
				new RoundDynamicNoIntegerDigits { Id = 4, D = -0.5000m, P = -1 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			// away from zero, +-0.5 rounds to +-1, which Decimal(4,4) cannot hold
			var maxId = mp == MidpointRounding.ToEven ? 4 : 2;

			var expected = data
				.Where(r => r.Id <= maxId)
				.OrderBy(r => r.Id)
				.Select(r => (r.Id, Math.Round(r.D, mp), 0m))
				.ToArray();

			var source = t
				.Where(r => r.Id <= maxId)
				.OrderBy(r => r.Id);

			var rounded = source
				.Select(r => Sql.AsSql(Math.Round(r.D, mp)))
				.ToArray();

			var negative = mp == MidpointRounding.AwayFromZero
				? source
					.Select(r => Sql.AsSql(Sql.Round(r.D, -1)))
					.ToArray()
				: source
					.Select(r => Sql.AsSql(Sql.RoundToEven(r.D, -1)))
					.ToArray();

			rounded.Length.ShouldBe(expected.Length);
			negative.Length.ShouldBe(expected.Length);

			var actual = expected
				.Select((r, i) => (r.Id, rounded[i], negative[i]!.Value))
				.ToArray();

			actual.ShouldBe(expected);

			if (mp == MidpointRounding.AwayFromZero)
			{
				foreach (var id in new[] { 3, 4 })
				{
					Action act = () => t
						.Where(r => r.Id == id)
						.Select(r => Sql.AsSql(Math.Round(r.D, mp)))
						.ToArray();

					act
						.ShouldThrow<Exception>()
						.Message
						.ShouldContain("ROUND result is out of range for Decimal(4,4)");
				}
			}
		}

		sealed class RoundComputed
		{
			[PrimaryKey                       ] public int     Id { get; set; }
			[Column                           ] public int     G  { get; set; }
			[Column(Precision = 10, Scale = 2)] public decimal D  { get; set; }
		}

		// A computed Decimal (SUM, AVG, a product) need not carry the type linq2db infers for it, while the tie
		// arithmetic of away-from-zero demands identical Decimal types.
		[Test]
		public void Round27([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundComputed { Id = 1, G = 1, D =  1.25m },
				new RoundComputed { Id = 2, G = 1, D =  1.25m },
				new RoundComputed { Id = 3, G = 2, D = -2.25m },
				new RoundComputed { Id = 4, G = 2, D = -0.25m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var groups =
				from r in t
				group r by r.G into g
				orderby g.Key
				select new
				{
					Sum     = Sql.AsSql(Math.Round(g.Sum(x => x.D), mp)),
					Average = Sql.AsSql(Math.Round(g.Average(x => x.D), mp)),
				};

			AssertQuery(groups);

			var products = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(Math.Round(r.D * r.D, mp)));

			AssertQuery(products, ByValue);
		}

		// A precision past the integer digits rounds every value of the type to 0, which the widened type that would
		// carry the extra scale has no room left to compute.
		[Test]
		public void Round28([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new RoundManyDigits { Id = 1, D =              1.5m,             N =  1.5m },
				new RoundManyDigits { Id = 2, D =  1000000000000.5m,             N = null  },
				new RoundManyDigits { Id = 3, D =  9999999999999.999999999m,     N = -5000000000000m },
				new RoundManyDigits { Id = 4, D = -9999999999999.999999999m,     N =  999999999999.5m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			// literal precisions: a captured variable would be a parameter and take the runtime path
			var result = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					r.Id,
					Away = Sql.AsSql(Sql.Round(r.D, -14)),
					Even = Sql.AsSql(Sql.RoundToEven(r.D, -20)),
					N    = Sql.AsSql(Sql.Round(r.N, -14)),
				})
				.ToArray();

			var expected = data
				.Select(r => new { r.Id, Away = (decimal?)0m, Even = (decimal?)0m, N = r.N == null ? null : (decimal?)0m })
				.ToArray();

			result.ShouldBe(expected);

			// a computed value can carry a type other than the one inferred for it (Decimal(22,9) for this product)
			using var wide = db.CreateLocalTable(new[]
			{
				new RoundNegativeDynamicWide { Id = 1, D22s9 = 1.5m, D35s2 = 1000m             },
				new RoundNegativeDynamicWide { Id = 2, D22s9 = 2.5m, D35s2 = 1000m             },
				new RoundNegativeDynamicWide { Id = 3, D22s9 = 0m,   D35s2 = 100000000000000m },
			});

			var computed = wide
				.Where(r => r.Id < 3)
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(Sql.Round(r.D35s2 * r.D35s2, -14) + r.D22s9))
				.ToArray();

			computed.ShouldBe(new decimal?[] { 1.5m, 2.5m });

			// one that exceeds it fails rather than rounding to 0
			Action act = () => wide
				.Where(r => r.Id == 3)
				.Select(r => Sql.AsSql(Sql.Round(r.D35s2 * r.D35s2, -14)))
				.ToArray();
			act
				.ShouldThrow<Exception>()
				.Message
				.ShouldContain("ROUND value is out of range for Decimal(22,9)");
		}

		// Rounding a computed value must leave it in its inferred type, on every branch of a runtime precision's CASE and
		// on the constant path, or YQL rejects the IF and the addition.
		[ActiveIssue(6055, Configuration = TestProvName.AllYdb, SkipForNonLinqService = true,
			ErrorTypeName = "Grpc.Core.RpcException", ErrorMessage = "Failed to convert type: Decimal(35,13) to Decimal(22,13)",
			Details = "LinqServiceSerializer drops SqlBinaryExpression's precision/scale, so the server folds the non-mandatory narrowing casts")]
		[Test]
		public void Round29([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new RoundNegativeDynamicWide { Id = 1, D35s2 = 1000m, P = -14 },
				new RoundNegativeDynamicWide { Id = 2, D35s2 = 1000m, P =   2 },
				new RoundNegativeDynamicWide { Id = 3, D35s2 = 1000m, P =  -4 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var result = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(Sql.Round(r.D35s2 * r.D35s2, r.P)))
				.ToArray();

			result.ShouldBe(new decimal?[] { 0m, 1000000m, 1000000m });

			// the constant path narrows the same way, and its result meets a column of the inferred type
			var constant = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(Sql.Round(r.D35s2 * r.D35s2, 2) + r.D22s9))
				.ToArray();

			constant.ShouldBe(new decimal?[] { 1000000m, 1000000m, 1000000m });
		}

		sealed class RoundComputedScale
		{
			[PrimaryKey                        ] public int     Id  { get; set; }
			[Column                            ] public int     G   { get; set; }
			[Column(Precision = 20, Scale = 12)] public decimal H   { get; set; }
			[Column(Precision = 25, Scale = 12)] public decimal H13 { get; set; }
			[Column(Precision = 35, Scale = 20)] public decimal K   { get; set; }
			[Column(Precision = 35, Scale = 22)] public decimal W   { get; set; }
			[Column(Precision = 20, Scale = 13)] public decimal T   { get; set; }
			[Column                            ] public int     P   { get; set; }
			[Column                            ] public int     P2  { get; set; }
		}

		static readonly RoundComputedScale[] _roundComputedScale =
		{
			new() { Id = 1, G = 1, H = 1.749999999750m, H13 =  4000000000000.122499999998m, K =  0.06249999999999999998m, W =  1.7499999999999999999998m, T =  0.0624999999998m, P =  0, P2 = 2 },
			new() { Id = 2, G = 2, H = 1.249999999750m, H13 = -4000000000000.122499999998m, K =  0.06749999999999999998m, W =  1.2499999999999999999998m, T =  0.0674999999998m, P =  0, P2 = 2 },
			new() { Id = 3, G = 3, H = 0.062499999998m,                                     K = -0.06249999999999999998m, W = -1.7499999999999999999998m, T = -0.0624999999998m, P =  2, P2 = 2 },
			new() { Id = 4, G = 4, H = 0.067499999998m,                                                                                                                         P =  2, P2 = 2 },
			new() { Id = 5, G = 5, H = 7.499999999975m,                                                                                                                         P = -1, P2 = 2 },
			new() { Id = 6, G = 1, H = 1.749999999750m,                                                                                                                         P =  0, P2 = 2 },
		};

		// A computed Decimal is inferred Decimal(22,9), while YQL computes it in the operands' type (H + H is
		// Decimal(20,12)), so a work type of the inferred scale would round its extra digits first. K, W and T sit on
		// the work type's scale for their path, H13 on its integer digits.
		[ActiveIssue(6055, Configuration = TestProvName.AllYdb, SkipForNonLinqService = true,
			ErrorTypeName = "Grpc.Core.RpcException", ErrorMessage = "Value does not fit into decimal",
			Details = "LinqServiceSerializer drops SqlBinaryExpression's precision/scale, so the server folds the narrowing cast and the result keeps the work type")]
		[Test]
		public void Round30([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = _roundComputedScale;

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var ordered = data
				.OrderBy(r => r.Id)
				.ToArray();

			var result = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					None = Sql.AsSql(Math.Round(r.H + r.H, mp)),
					Zero = Sql.AsSql(Math.Round(r.H + r.H, 0, mp)),
					Two  = Sql.AsSql(Math.Round(r.H + r.H, 2, mp)),
					H13  = Sql.AsSql(Math.Round(r.H13 + r.H13, 2, mp)),
					K    = Sql.AsSql(Math.Round(r.K + r.K, 2, mp)),
					W    = Sql.AsSql(Math.Round(r.W + r.W, mp)),
				})
				.ToArray();

			var expected = ordered
				.Select(r => new
				{
					None = Math.Round(r.H + r.H, mp),
					Zero = Math.Round(r.H + r.H, 0, mp),
					Two  = Math.Round(r.H + r.H, 2, mp),
					H13  = Math.Round(r.H13 + r.H13, 2, mp),
					K    = Math.Round(r.K + r.K, 2, mp),
					W    = Math.Round(r.W + r.W, mp),
				})
				.ToArray();

			result.ShouldBe(expected);

			// literal precisions: a captured variable would be a parameter and take the runtime path
			var negative = mp == MidpointRounding.AwayFromZero
				? t
					.OrderBy(r => r.Id)
					.Select(r => Sql.AsSql(Sql.Round(r.H + r.H, -1)))
					.ToArray()
				: t
					.OrderBy(r => r.Id)
					.Select(r => Sql.AsSql(Sql.RoundToEven(r.H + r.H, -1)))
					.ToArray();

			var expectedNegative = ordered
				.Select(r => (decimal?)RoundTo(r.H + r.H, -1, mp))
				.ToArray();

			negative.ShouldBe(expectedNegative);

			var sums =
				(
					from r in t
					group r by r.G into g
					orderby g.Key
					select Sql.AsSql(Math.Round(g.Sum(x => x.H), mp))
				)
				.ToArray();

			var expectedSums = data
				.GroupBy(r => r.G)
				.OrderBy(g => g.Key)
				.Select(g => Math.Round(g.Sum(x => x.H), mp))
				.ToArray();

			sums.ShouldBe(expectedSums);
		}

		[ActiveIssue(6055, Configuration = TestProvName.AllYdb, SkipForNonLinqService = true,
			ErrorTypeName = "Grpc.Core.RpcException", ErrorMessage = "Failed to convert type: Decimal(35,13) to Decimal(22,13)",
			Details = "LinqServiceSerializer drops SqlBinaryExpression's precision/scale, so the server folds the non-mandatory narrowing casts")]
		[Test]
		public void Round31([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = _roundComputedScale;

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var ordered = data
				.OrderBy(r => r.Id)
				.ToArray();

			var result = mp == MidpointRounding.AwayFromZero
				? t
					.OrderBy(r => r.Id)
					.Select(r => new
					{
						H   = Sql.AsSql(Sql.Round(r.H + r.H, r.P)),
						H13 = Sql.AsSql(Sql.Round(r.H13 + r.H13, r.P2)),
						T   = Sql.AsSql(Sql.Round(r.T + r.T, r.P2)),
					})
					.ToArray()
				: t
					.OrderBy(r => r.Id)
					.Select(r => new
					{
						H   = Sql.AsSql(Sql.RoundToEven(r.H + r.H, r.P)),
						H13 = Sql.AsSql(Sql.RoundToEven(r.H13 + r.H13, r.P2)),
						T   = Sql.AsSql(Sql.RoundToEven(r.T + r.T, r.P2)),
					})
					.ToArray();

			var expected = ordered
				.Select(r => new
				{
					H   = (decimal?)RoundTo(r.H + r.H, r.P, mp),
					H13 = (decimal?)RoundTo(r.H13 + r.H13, r.P2, mp),
					T   = (decimal?)RoundTo(r.T + r.T, r.P2, mp),
				})
				.ToArray();

			result.ShouldBe(expected);
		}

		sealed class RoundDoubleStored
		{
			[PrimaryKey                        ] public int     Id { get; set; }
			[Column(DataType = DataType.Double)] public decimal D  { get; set; }
			[Column                            ] public int     P  { get; set; }
		}

		// A decimal stored as Double is not a YQL Decimal: it rounds in Double, not through Decimal casts.
		[Test]
		public void Round32([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundDoubleStored { Id = 1, D =  2.5m,  P = 1 },
				new RoundDoubleStored { Id = 2, D =  3.75m, P = 1 },
				new RoundDoubleStored { Id = 3, D = -2.5m,  P = 1 },
				new RoundDoubleStored { Id = 4, D =  1.25m, P = 1 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query = mp == MidpointRounding.AwayFromZero
				? t
					.OrderBy(r => r.Id)
					.Select(r => new
					{
						None    = Sql.AsSql(Math.Round(r.D, mp)),
						One     = Sql.AsSql(Math.Round(r.D, 1, mp)),
						Runtime = Sql.AsSql(Sql.Round(r.D, r.P)),
					})
				: t
					.OrderBy(r => r.Id)
					.Select(r => new
					{
						None    = Sql.AsSql(Math.Round(r.D, mp)),
						One     = Sql.AsSql(Math.Round(r.D, 1, mp)),
						Runtime = Sql.AsSql(Sql.RoundToEven(r.D, r.P)),
					});

			AssertQuery(query);
		}

		sealed class RoundDeclaredShapeFull
		{
			[PrimaryKey                       ] public int     Id { get; set; }
			[Column(Precision = 30, Scale = 9)] public decimal D  { get; set; }
			[Column(Precision = 22, Scale = 2)] public decimal S  { get; set; }
			[Column                           ] public decimal B  { get; set; }
			[Column                           ] public int     P  { get; set; }
		}

		sealed class RoundDeclaredShape
		{
			[PrimaryKey            ] public int     Id { get; set; }
			[Column(Precision = 30)] public decimal D  { get; set; }
			[Column(Scale = 2)     ] public decimal S  { get; set; }
			[Column                ] public decimal B  { get; set; }
			[Column                ] public int     P  { get; set; }
		}

		// A column declaring only one of Precision and Scale keeps its declared sizing, while a column declaring neither
		// is sized as a computed value is: all must round every digit they hold. The table is written through a fully
		// declared mapping, as a Precision-only one cannot be written over LinqService.
		[Test]
		public void Round33([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundDeclaredShapeFull { Id = 1, D =  123456789012345678.125m, S =  1234567890123456.25m, B =  9999999999999.125m, P = 2 },
				new RoundDeclaredShapeFull { Id = 2, D = -123456789012345678.135m, S = -1234567890123456.35m, B = -9999999999999.135m, P = 2 },
				new RoundDeclaredShapeFull { Id = 3, D =  123456789012345678.5m,   S =  1234567890123456.15m, B =  9999999999998.5m,   P = 2 },
			};

			using var db   = GetDataContext(context);
			using var full = db.CreateLocalTable(data);

			var t = db
				.GetTable<RoundDeclaredShape>()
				.TableName(full.TableName);

			var query = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					D    = Sql.AsSql(Math.Round(r.D, mp)),
					DTwo = Sql.AsSql(Math.Round(r.D, 2, mp)),
					SOne = Sql.AsSql(Math.Round(r.S, 1, mp)),
					B    = Sql.AsSql(Math.Round(r.B, mp)),
				});

			AssertQuery(query);
		}

		[ActiveIssue(6055, Configuration = TestProvName.AllYdb, SkipForNonLinqService = true,
			ErrorTypeName = "Grpc.Core.RpcException", ErrorMessage = "Failed to convert type: Decimal(35,13) to Decimal(22,13)",
			Details = "LinqServiceSerializer drops SqlBinaryExpression's precision/scale, so the server folds the non-mandatory narrowing casts")]
		[Test]
		public void Round34([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundDeclaredShapeFull { Id = 1, B =  9999999999999.125m, P = 2 },
				new RoundDeclaredShapeFull { Id = 2, B = -9999999999999.135m, P = 2 },
				new RoundDeclaredShapeFull { Id = 3, B =  9999999999998.5m,   P = 0 },
			};

			using var db   = GetDataContext(context);
			using var full = db.CreateLocalTable(data);

			var t = db
				.GetTable<RoundDeclaredShape>()
				.TableName(full.TableName);

			var query = mp == MidpointRounding.AwayFromZero
				? t
					.OrderBy(r => r.Id)
					.Select(r => Sql.AsSql(Sql.Round(r.B, r.P)))
				: t
					.OrderBy(r => r.Id)
					.Select(r => Sql.AsSql(Sql.RoundToEven(r.B, r.P)));

			AssertQuery(query, ByNullableValue);
		}

		[ActiveIssue(6055, Configuration = TestProvName.AllYdb, SkipForNonLinqService = true,
			ErrorTypeName = "Grpc.Core.RpcException", ErrorMessage = "Value does not fit into decimal",
			Details = "LinqServiceSerializer drops SqlBinaryExpression's precision/scale, so the server folds the narrowing cast and the result keeps the work type")]
		[Test]
		public void Round35([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundDeclaredShapeFull { Id = 1, B =  9999999999999.125m },
				new RoundDeclaredShapeFull { Id = 2, B = -9999999999999.135m },
				new RoundDeclaredShapeFull { Id = 3, B =  9999999999998.5m   },
			};

			using var db   = GetDataContext(context);
			using var full = db.CreateLocalTable(data);

			var t = db
				.GetTable<RoundDeclaredShape>()
				.TableName(full.TableName);

			var query = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(Math.Round(r.B, 2, mp)));

			AssertQuery(query, ByValue);
		}

		sealed class DecimalStoredDouble
		{
			[PrimaryKey                                                    ] public int    Id { get; set; }
			[Column(DataType = DataType.Decimal)                           ] public double A  { get; set; }
			[Column(DataType = DataType.Decimal)                           ] public double B  { get; set; }
			[Column(DataType = DataType.Decimal, Precision = 10, Scale = 2)] public double C  { get; set; }
			[Column(DataType = DataType.Decimal, Precision = 22, Scale = 9)] public double L  { get; set; }
			[Column                                                        ] public double F  { get; set; }
		}

		// A double stored as Decimal computes in Double, as the C# expression does: beside a floating operand (APlusF) its
		// Decimal side is cast; beside another Decimal operand, a column or a literal typed by one, both sides are.
		[Test]
		public void DecimalStoredDoubleArithmetic([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new DecimalStoredDouble { Id = 1, A =  1,    B = 3, C = 1.25, L = 12345678.5, F = 0.5  },
				new DecimalStoredDouble { Id = 2, A = -2.25, B = 7, C = 2.5,  L = -3.25,      F = 1.25 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					APlusC   = Sql.AsSql(r.A + r.C),
					CDivB    = Sql.AsSql(r.C / r.B),
					ADiv3    = Sql.AsSql(r.A / 3.0),
					CTimesC  = Sql.AsSql(r.C * r.C),
					LTimesL  = Sql.AsSql(r.L * r.L),
					ATimes25 = Sql.AsSql(r.A * 2.5),
					APlusF   = Sql.AsSql(r.A + r.F),
					ARounded = Sql.AsSql(Math.Round(r.A, 1)),
				});

			AssertQuery(query);
		}

		sealed class RoundDecimalStoredDouble
		{
			[PrimaryKey                                                    ] public int    Id { get; set; }
			[Column(DataType = DataType.Decimal)                           ] public double A  { get; set; }
			[Column(DataType = DataType.Decimal, Precision = 10, Scale = 2)] public double C  { get; set; }
			[Column(DataType = DataType.Decimal, Precision = 22, Scale = 9)] public double L  { get; set; }
			[Column(DataType = DataType.Decimal, Precision = 5,  Scale = 3)] public double N  { get; set; }
			[Column                                                        ] public double F  { get; set; }
		}

		// A double stored as Decimal rounds in Double: neither 10^p nor the scaled value need fit the column, and the
		// result stays a Double beside a floating column or in arithmetic with another Decimal-stored double. With no
		// precision the result keeps the column's type, so it still compares with the column. A runtime precision
		// truncates a fractional value first (row 5).
		[Test]
		public void Round38([IncludeDataSources(true, TestProvName.AllYdb)] string context, [Values(MidpointRounding.ToEven, MidpointRounding.AwayFromZero)] MidpointRounding mp)
		{
			var data = new[]
			{
				new RoundDecimalStoredDouble { Id = 1, A =  1000000000000.25, C =  99999999.25, L =  10000000000.0625, N =  1.125,  F = 1 },
				new RoundDecimalStoredDouble { Id = 2, A = -1000000000000.25, C = -99999999.25, L = -10000000000.0625, N = -1.125,  F = 1 },
				new RoundDecimalStoredDouble { Id = 3, A =  2.5,              C =  2.5,          L =  2.5,              N =  2.5,    F = 1 },
				new RoundDecimalStoredDouble { Id = 4, A =  3,                C =  3,            L =  3,                N =  3,      F = 1 },
				new RoundDecimalStoredDouble { Id = 5, A =  2.25,             C =  2.25,         L =  2.25,             N =  2.25,   F = 1.25 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					A           = Sql.AsSql(Math.Round(r.A, 1, mp)),
					C           = Sql.AsSql(Math.Round(r.C, 1, mp)),
					L           = Sql.AsSql(Math.Round(r.L, 3, mp)),
					N           = Sql.AsSql(Math.Round(r.N, 2, mp)),
					RuntimeAway = Sql.AsSql(Sql.Round(r.A, (int)r.F)),
					RuntimeEven = Sql.AsSql(Sql.RoundToEven(r.A, (int)r.F)),
					None        = Sql.AsSql(Math.Round(r.A * 10, mp)),
					NoneSame    = Sql.AsSql(Math.Round(r.C, mp) == r.C),
					GreaterF    = Sql.AsSql(Math.Round(r.A, 1, mp) > r.F),
					PlusL       = Sql.AsSql(Math.Round(r.C, 1, mp) + r.L),
				});

			AssertQuery(query);
		}

		sealed class PowScale
		{
			[PrimaryKey                       ] public int     Id { get; set; }
			[Column(Precision = 18, Scale = 4)] public decimal D  { get; set; }
		}

		// Sybase: https://stackoverflow.com/questions/25281843
		// The exponent must not take the type of a column the call is combined with: Decimal(18,4) cuts 0.00005 to 0.
		[Test]
		public void PowDecimalExponentScale([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new PowScale { Id = 1, D = 16m } });

			// 16^0.00005 + 16 = 17.000138..., 17.0001 where the result keeps the column's scale; a cut exponent gives 17
			var result = t
				.Select(r => Sql.AsSql(Sql.Power(r.D, 0.00005m) + r.D))
				.Single();

			result!.Value.ShouldBeInRange(17.00009m, 17.00015m);
		}

		sealed class PowBaseScale
		{
			[PrimaryKey                       ] public int     Id { get; set; }
			[Column(Precision = 18, Scale = 4)] public decimal D  { get; set; }
			[Column(Precision = 18, Scale = 4)] public decimal E  { get; set; }
		}

		// Sybase: https://stackoverflow.com/questions/25281843
		// The base must not take the scale of a column the call is combined with: Decimal(18,4) cuts 1.00005.
		[Test]
		public void PowDecimalBaseScale([DataSources(TestProvName.AllSybase)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new PowBaseScale { Id = 1, D = 16m, E = 2m } });

			// 1.00005^2 + 16 = 17.0001000025, 17.0001 where the result keeps the column's scale; a cut base gives 17.0000 or 17.0002
			var result = t
				.Select(r => Sql.AsSql(Sql.Power(1.00005m, r.E) + r.D))
				.Single();

			result!.Value.ShouldBeInRange(17.00009m, 17.00011m);
		}

		sealed class RoundValueScale
		{
			[PrimaryKey                        ] public int     Id { get; set; }
			[Column(Precision = 18, Scale = 4) ] public decimal D  { get; set; }
			[Column(Precision = 18, Scale = 4) ] public decimal E  { get; set; }
			[Column                            ] public int     P  { get; set; }
			[Column                            ] public int     P3 { get; set; }
		}

		sealed class RoundValueScaleWide
		{
			[PrimaryKey                        ] public int     Id { get; set; }
			[Column(Precision = 35, Scale = 30)] public decimal W  { get; set; }
			[Column                            ] public int     P  { get; set; }
		}

		// A literal value or base keeps its own scale beside a column on YDB and ClickHouse, the providers whose literals render a
		// scale. ClickHouse's Power keeps the exact result, so the Pow cells are ranged.
		[Test]
		public void Round36([IncludeDataSources(true, TestProvName.AllYdb, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new RoundValueScale { Id = 1, D = 16m, E = 2m, P = 4, P3 = 3 } });

			var result = t
				.Select(r => new
				{
					Pow      = Sql.AsSql(Sql.Power(1.00005m, r.E) + r.D),
					PowTiny  = Sql.AsSql(Sql.Power(0.00005m, r.E) + r.D),
					Away     = Sql.AsSql(Sql.Round(1.00005m, r.P) + r.D),
					Even     = Sql.AsSql(Sql.RoundToEven(1.00051m, r.P3) + r.D),
					AwayTiny = Sql.AsSql(Sql.Round(0.00005m, r.P) + r.D),
				})
				.Single();

			// 1.00005^2 + 16 = 17.0001000025 and 0.00005^2 + 16 = 16.0000000025; a cut base gives 17.0000 and 16. YDB keeps the
			// column's scale, so there PowTiny reads 16 either way and only proves the query runs.
			result.Pow!.Value.ShouldBeInRange(17.00009m, 17.00011m);

			if (context.IsAnyOf(TestProvName.AllClickHouse))
				result.PowTiny!.Value.ShouldBeInRange(16.000000002m, 16.000000003m);
			else
				result.PowTiny.ShouldBe(16m);

			result.Away.ShouldBe(17.0001m);
			result.Even.ShouldBe(17.001m);
			result.AwayTiny.ShouldBe(16.0001m);
		}

		// A scale of 28 or more cannot cut a decimal literal; such a value cannot be read back or written remotely either.
		[Test]
		public void Round37([IncludeDataSources(TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new RoundValueScaleWide { Id = 1, W = 16m, P = 4 } });

			var result = t
				.Select(r => Sql.AsSql(Sql.Round(1.00005m, r.P) + r.W == 17.0001m))
				.Single();

			result.ShouldBeTrue();
		}

		// Only YDB and ClickHouse render a decimal literal's scale, so only they can cut the value. A constant rounding scale
		// would be evaluated on the client unless wrapped.
		[Test]
		public void RoundDecimalValueScale([IncludeDataSources(true, TestProvName.AllYdb, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[] { new RoundValueScale { Id = 1, D = 16m, E = 2m, P = 4, P3 = 3 } });

			var result = t
				.Select(r => new
				{
					Away = Sql.AsSql(Sql.Round(1.00005m, Sql.AsSql(4)) + r.D),
					Even = Sql.AsSql(Sql.RoundToEven(1.00051m, Sql.AsSql(3)) + r.D),
				})
				.Single();

			result.Away.ShouldBe(17.0001m);
			result.Even.ShouldBe(17.001m);
		}

		sealed class RoundValueNarrowFamily
		{
			[PrimaryKey                                                       ] public int     Id  { get; set; }
			[Column(DataType = DataType.Decimal32, Precision = 9,  Scale = 2)] public decimal D32 { get; set; }
			[Column(DataType = DataType.Decimal64, Precision = 18, Scale = 2)] public decimal D64 { get; set; }
		}

		// A literal value keeps its own scale beside a narrow ClickHouse Decimal column: it takes Decimal128, so neither its
		// digits nor the column's integer digits are cut.
		[Test]
		public void RoundDecimalValueNarrowFamily([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			var data = new[]
			{
				new RoundValueNarrowFamily { Id = 1, D32 = 0m,       D64 = 0m                },
				new RoundValueNarrowFamily { Id = 2, D32 = 5000000m, D64 = 9000000000000000m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Away32  = Sql.AsSql(Sql.Round(1234567.125m, Sql.AsSql(1)) + r.D32),
					Even32  = Sql.AsSql(Sql.RoundToEven(1234567.125m, Sql.AsSql(2)) + r.D32),
					Scale32 = Sql.AsSql(Sql.Round(1.0000000005m, Sql.AsSql(9)) + r.D32),
					Small32 = Sql.AsSql(Sql.Round(1.125m, Sql.AsSql(1)) + r.D32),
					Away64  = Sql.AsSql(Sql.Round(1234567890123456.125m, Sql.AsSql(1)) + r.D64),
					Small64 = Sql.AsSql(Sql.Round(1.125m, Sql.AsSql(1)) + r.D64),
				});

			AssertQuery(query);

			var wide = t
				.OrderBy(r => r.Id)
				.Select(r => new
				{
					Wide32 = Sql.AsSql(Sql.Round(123456789.5m, Sql.AsSql(0)) + r.D32),
					Edge32 = Sql.AsSql(Sql.Round(12345678.5m, Sql.AsSql(0)) + r.D32),
					Wide64 = Sql.AsSql(Sql.Round(12345678901234567.5m, Sql.AsSql(0)) + r.D64),
				});

			AssertQuery(wide);
		}

		[Test]
		public void Sign([DataSources(TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Sign(p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Sign(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Sin([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Sinh([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Sqrt([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Tan([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Tanh([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Truncate1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t,
				from t in from p in db.Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t);
		}

		[Test]
		public void Truncate2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in from p in Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t,
				from t in from p in db.Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t);
		}
	}
}
