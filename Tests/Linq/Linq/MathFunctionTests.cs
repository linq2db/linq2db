using System;
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

		// Rounds server-side to more digits than the column's declared scale, a no-op that a provider emulating
		// ROUND by scaling must not overflow on: MoneyValue is Decimal(6,2) on YDB, and 11.45 * 10^5 does not fit that.
		[Test]
		public void Round13([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in    Types where p.MoneyValue != 0 select Math.Round(p.MoneyValue, 5),
				from p in db.Types where p.MoneyValue != 0 select Sql.AsSql(Math.Round(p.MoneyValue, 5)));
		}

		// The widened intermediate must not leak into the result type: YQL arithmetic and IF reject
		// Decimal operands of different types.
		[Test]
		public void Round14([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in    Types where p.MoneyValue != 0 select Math.Round(p.MoneyValue, 5) + p.MoneyValue,
				from p in db.Types where p.MoneyValue != 0 select Sql.AsSql(Math.Round(p.MoneyValue, 5) + p.MoneyValue));
			AreEqual(
				from p in    Types where p.MoneyValue != 0 select p.ID > 2 ? Math.Round(p.MoneyValue, 5) : p.MoneyValue,
				from p in db.Types where p.MoneyValue != 0 select Sql.AsSql(p.ID > 2 ? Math.Round(p.MoneyValue, 5) : p.MoneyValue));
		}

		[Test]
		public void Round15([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in    Types where p.MoneyValue != 0 select Sql.Round(p.MoneyValue, 5) + p.MoneyValue,
				from p in db.Types where p.MoneyValue != 0 select Sql.AsSql(Sql.Round(p.MoneyValue, 5) + p.MoneyValue));
			AreEqual(
				from p in    Types where p.MoneyValue != 0 select Sql.RoundToEven(p.MoneyValue, 5) + p.MoneyValue,
				from p in db.Types where p.MoneyValue != 0 select Sql.AsSql(Sql.RoundToEven(p.MoneyValue, 5) + p.MoneyValue));
		}

		// A non-constant precision must not cost the value its fractional digits.
		[Test]
		public void Round16([DataSources(TestProvName.AllDuckDB)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in    Types where p.MoneyValue != 0 select Math.Round(p.MoneyValue, p.ID % 2 + 2),
				from p in db.Types where p.MoneyValue != 0 select Sql.AsSql(Math.Round(p.MoneyValue, p.ID % 2 + 2)));
			AreEqual(
				from p in    Types where p.MoneyValue != 0 select Math.Round(p.MoneyValue, p.ID % 2 + 1),
				from p in db.Types where p.MoneyValue != 0 select Sql.AsSql(Math.Round(p.MoneyValue, p.ID % 2 + 1)));
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

			AreEqual(
				from r in data orderby r.Id select new { r.Id, R34 = Math.Round(r.D34s2, 2, mp), R35 = Math.Round(r.D35s2, 5, mp), R6 = Math.Round(r.D35s6, 2, mp), R4 = Math.Round(r.D4s4, 2, mp) },
				from r in t    orderby r.Id select new { r.Id, R34 = Sql.AsSql(Math.Round(r.D34s2, 2, mp)), R35 = Sql.AsSql(Math.Round(r.D35s2, 5, mp)), R6 = Sql.AsSql(Math.Round(r.D35s6, 2, mp)), R4 = Sql.AsSql(Math.Round(r.D4s4, 2, mp)) });
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

			var result = t.OrderBy(r => r.Id).Select(r => new { Away = Sql.AsSql(Sql.Round(r.D, -1)), Even = Sql.AsSql(Sql.RoundToEven(r.D, -1)) }).ToArray();

			result.Select(r => r.Away).ShouldBe(new decimal?[] { 1230m, 1230m, -1230m, 1230m, -1230m });
			result.Select(r => r.Even).ShouldBe(new decimal?[] { 1230m, 1230m, -1230m, 1220m, -1220m });
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

			AreEqual(
				from r in data orderby r.Id select new { r.Id, R = Math.Round(r.D, 1, mp), Sum = Math.Round(r.D, 1, mp) + r.D, Cond = r.Id > 2 ? Math.Round(r.D, 1, mp) : r.D },
				from r in t    orderby r.Id select new { r.Id, R = Sql.AsSql(Math.Round(r.D, 1, mp)), Sum = Sql.AsSql(Math.Round(r.D, 1, mp) + r.D), Cond = Sql.AsSql(r.Id > 2 ? Math.Round(r.D, 1, mp) : r.D) });
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

			var result = t.OrderBy(r => r.Id).Select(r => new { Away = Sql.AsSql(Sql.Round(r.D, -3)), Even = Sql.AsSql(Sql.RoundToEven(r.D, -3)) }).ToArray();

			result.Select(r => r.Away).ShouldBe(new decimal?[] { 1000m, 1000m, 3000m, -3000m, 4000m });
			result.Select(r => r.Even).ShouldBe(new decimal?[] { 1000m, 1000m, 2000m, -2000m, 4000m });
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

			var result = t.OrderBy(r => r.Id).Select(r => new { Away = Sql.AsSql(Sql.Round(r.D, r.P)), Even = Sql.AsSql(Sql.RoundToEven(r.D, r.P)) }).ToArray();

			result.Select(r => r.Away).ShouldBe(new decimal?[] { 1230m, 1200m, 1230m, 1230m, 1000m, -3000m, 0m, 9999.99m });
			result.Select(r => r.Even).ShouldBe(new decimal?[] { 1230m, 1200m, 1230m, 1220m, 1000m, -2000m, 0m, 9999.99m });
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

			var result = t.OrderBy(r => r.Id).Select(r => new { Away = Sql.AsSql(Sql.Round(r.D, r.P)), Even = Sql.AsSql(Sql.RoundToEven(r.D, r.P)) }).ToArray();

			result.Select(r => r.Away).ShouldBe(new decimal?[] { 0.1234m, 0.125m, 0.13m, -0.124m, 0m });
			result.Select(r => r.Even).ShouldBe(new decimal?[] { 0.1234m, 0.125m, 0.12m, -0.124m, 0m });
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
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var result = t.OrderBy(r => r.Id).Select(r => new { Away = Sql.AsSql(Sql.Round(r.D22s9, r.P)), Even = Sql.AsSql(Sql.RoundToEven(r.D22s9, r.P)) }).ToArray();

			result.Select(r => r.Away).ShouldBe(new decimal?[] { 100000m, -300000m, 10000000000m, 0m, 1234.57m, 1235m, 12.5m });
			result.Select(r => r.Even).ShouldBe(new decimal?[] { 100000m, -200000m, 10000000000m, 0m, 1234.56m, 1235m, 12.5m });

			t.Select(r => Sql.AsSql(Sql.Round(r.D22s9, r.Id > 0 ? null : r.P))).ToArray().ShouldAllBe(v => v == null);

			Action act = () => t.Where(r => r.P < 0).Select(r => Sql.AsSql(Sql.Round(r.D35s2, r.P))).ToArray();
			act.ShouldThrow<Exception>().Message.ShouldContain("ROUND precision is out of range for Decimal(35,2)");
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
