using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

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
		public void Acos([DataSources(ProviderName.Access, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Asin([DataSources(ProviderName.Access, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Atan([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Atan2([DataSources(ProviderName.Access, ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Ceiling1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t,
					from t in from p in db.Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t);
		}

		[Test]
		public void Ceiling2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Ceiling(p.MoneyValue) where t != 0 select t,
					from t in from p in db.Types select Math.Ceiling(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Cos([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Cosh([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Cot([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15).Value * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15).Value * 15) where t != 0.1 select t);
		}

		[Test]
		public void Degrees1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Sql.Degrees(p.MoneyValue).Value) where t != 0.1m select t,
					from t in from p in db.Types select Math.Floor(Sql.Degrees(p.MoneyValue).Value) where t != 0.1m select t);
		}

		[Test]
		public void Degrees2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.Degrees((double)p.MoneyValue).Value where t != 0.1 select Math.Floor(t),
					from t in from p in db.Types select Sql.Degrees((double)p.MoneyValue).Value where t != 0.1 select Math.Floor(t));
		}

		[Test]
		public void Degrees3([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.Degrees((int)p.MoneyValue).Value where t != 0.1 select t,
					from t in from p in db.Types select Sql.Degrees((int)p.MoneyValue).Value where t != 0.1 select t);
		}

		[Test]
		public void Exp([DataSources(ProviderName.SQLiteMS)] string context)
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
		public void Log([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t);
		}

		[Test]
		public void Log2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t);
		}

		[Test]
		public void Log10([DataSources(ProviderName.SQLiteMS)] string context)
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
		public void Pow([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t,
					from t in from p in db.Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t);
		}

		[Test]
		public void Round1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue) where t != 0 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Round2([DataSources(ProviderName.SQLiteMS)] string context)
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
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, 1) where t != 0 && t != 7 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, 1) where t != 0 && t != 7 select t);
		}

		[Test]
		public void Round4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5),
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5));
		}

		[Test]
		public void Round5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		[Test]
		public void Round6([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t);
		}

		[Test]
		public void Round7([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t);
		}

		[Test]
		public void Round8([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t);
		}

		[Test]
		public void Round9([DataSources(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)] string context)
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
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 && t != 7 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 && t != 7 select t);
		}

		[Test]
		public void Round11([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5),
					from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5));
		}

		[Test]
		public void Round12([DataSources(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)] string context)
		{
			var mp = MidpointRounding.AwayFromZero;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 && t != 7 select t,
					from t in from p in db.Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 && t != 7 select t);
		}

		[Test]
		public void Sign([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Sign(p.MoneyValue) where t != 0 select t,
					from t in from p in db.Types select Math.Sign(p.MoneyValue) where t != 0 select t);
		}

		[Test]
		public void Sin([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Sinh([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Sqrt([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Tan([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Tanh([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
					from t in from p in db.Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t);
		}

		[Test]
		public void Truncate1([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t,
					from t in from p in db.Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t);
		}

		[Test]
		public void Truncate2([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t,
					from t in from p in db.Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t);
		}
	}
}
