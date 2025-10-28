using System;
using System.Linq;

using LinqToDB;

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
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Abs(p.MoneyValue) where t > 0 select t,
					from t in from p in db.Types select Math.Abs(p.MoneyValue) where t > 0 select t);
		}

		[Test]
		public void Acos([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Asin([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Atan([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Atan2([DataSources(TestProvName.AllAccess)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Ceiling1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t,
					from t in from p in db.Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t);
		}

		[Test]
		public void Ceiling2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Ceiling(p.MoneyValue) where t != 0 select t,
					from t in from p in db.Types select Math.Ceiling(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Cos([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Cosh([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Cot([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15)!.Value * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15)!.Value * 15) where t != 0.1 select t);
		}

		[Test]
		public void Degrees1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					t => t,
					from t in from p in    Types select Math.Floor(Sql.Degrees(p.MoneyValue)!.Value) where t != 0.1m select t,
					from t in from p in db.Types select Math.Floor(Sql.Degrees(p.MoneyValue)!.Value) where t != 0.1m select t,
					comparer: DecimalComparerInstance);
		}

		[Test]
		public void Degrees2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.Degrees((double)p.MoneyValue)!.Value where t != 0.1 select Math.Floor(t),
					from t in from p in db.Types select Sql.Degrees((double)p.MoneyValue)!.Value where t != 0.1 select Math.Floor(t));
		}

		[Test]
		public void Degrees3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.Degrees((int)p.MoneyValue)!.Value where t != 0.1 select t,
					from t in from p in db.Types select Sql.Degrees((int)p.MoneyValue)!.Value where t != 0.1 select t);
		}

		[Test]
		public void Exp([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Exp((double)p.MoneyValue)) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Exp((double)p.MoneyValue)) where t != 0.1 select t);
		}

		[Test]
		public void Floor([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(-(p.MoneyValue + 1)) where t != 0 select t,
					from t in from p in db.Types select Math.Floor(-(p.MoneyValue + 1)) where t != 0 select t);
		}

		[Test]
		public void Log([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t);
		}

		[Test]
		public void Log2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t);
		}

		[Test]
		public void Log10([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Log10((double)p.MoneyValue)) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Log10((double)p.MoneyValue)) where t != 0.1 select t);
		}

		[Test]
		public void Max([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Max(p.MoneyValue, 5.1m) where t != 0 select t,
					from t in from p in db.Types select Math.Max(p.MoneyValue, 5.1m) where t != 0 select t);
		}

		[Test]
		public void Min([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Min(p.MoneyValue, 5) where t != 0 select t,
					from t in from p in db.Types select Math.Min(p.MoneyValue, 5) where t != 0 select t);
		}

		[Test]
		public void Pow([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t,
					from t in from p in db.Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t);
		}

		// Sybase: https://stackoverflow.com/questions/25281843
		[Test]
		public void PowDecimal([DataSources(TestProvName.AllSybase)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in Types select Math.Floor(Sql.Power(p.MoneyValue, 3)!.Value) where t != 0 select t,
					from t in from p in db.Types select Math.Floor(Sql.Power(p.MoneyValue, 3)!.Value) where t != 0 select t);
		}

		[Test]
		public void Round1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue) where t != 0 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Round2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue) where t != 0 select t,
					from t in from p in db.Types select Math.Round((double)p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Round3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from t in from p in db.Types select Math.Round(p.MoneyValue, 1) where t != 0 && t != 7 select t;

				if (context.IsAnyOf(ProviderName.DB2))
					q = q.AsQueryable().Select(t => Math.Round(t, 1));

				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, 1) where t != 0 && t != 7 select t,
					q);
			}
		}

#if AZURE
		[ActiveIssue("Fails on CI", Configuration = ProviderName.DB2)]
#endif
		[Test]
		public void Round4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5),
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5));
		}

		[ActiveIssue("Wrong Firebird, DB2 implementation", Configurations = [TestProvName.AllFirebird, TestProvName.AllDB2])]
		[Test]
		public void Round4Sql([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in Types select Math.Round((double)p.MoneyValue, 1) where t    != 0 select Math.Round(t, 5),
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Sql.AsSql(Math.Round(t, 5)));
		}

		[Test]
		public void Round5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		// ClickHouse: AwayFromZero rounding supported only for decimals. Double use bankers rounding (Round5 test)
		[Test]
		public void Round6([DataSources(TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		[Test]
		public void Round7([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t);
		}

		[Test]
		public void Round8([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t);
		}

		[Test]
		public void Round9([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, 1, MidpointRounding.AwayFromZero) where t != 0 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, 1, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		[Test]
		public void Round10([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from t in from p in db.Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 && t != 7 select t;

				if (context.IsAnyOf(ProviderName.DB2))
					q = q.AsQueryable().Select(t => Math.Round(t, 1, MidpointRounding.ToEven));

				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 && t != 7 select t,
					q);
			}
		}

#if AZURE
		[ActiveIssue("Fails on CI", Configuration = ProviderName.DB2)]
#endif
		[Test]
		public void Round11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5),
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5));
		}

		// TODO: implement other MidpointRounding values (and remove NUnit4001 suppress)
		[Test]
#pragma warning disable NUnit4001
		public void Round12([DataSources(TestProvName.AllSQLite)] string context, [Values(MidpointRounding.AwayFromZero, MidpointRounding.ToEven)] MidpointRounding mp, [Values(1, 2)] int iteration)
#pragma warning restore NUnit4001
		{

			using (var db = GetDataContext(context))
			{
				var q = from t in from p in db.Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 && t != 7 select t;

				if (context.IsAnyOf(ProviderName.DB2))
					q = q.AsQueryable().Select(t => Math.Round(t, 1, mp));

				var cacheMissCount = q.GetCacheMissCount();

				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 && t != 7 select t,
					q);

				if (iteration > 1)
					q.GetCacheMissCount().ShouldBe(cacheMissCount);
			}
		}

		[Test]
		public void Sign([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Sign(p.MoneyValue) where t != 0 select t,
					from t in from p in db.Types select Math.Sign(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Sin([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Sinh([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Sqrt([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Tan([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Tanh([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Truncate1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t,
					from t in from p in db.Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t);
		}

		[Test]
		public void Truncate2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t,
					from t in from p in db.Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t);
		}
	}
}
