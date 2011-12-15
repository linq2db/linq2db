using System;
using System.Linq;
using LinqToDB.Data.Linq;
using NUnit.Framework;

using LinqToDB.Data.DataProvider;

namespace Data.Linq
{
	[TestFixture]
	public class MathFunctions : TestBase
	{
		[Test]
		public void Abs()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Abs(p.MoneyValue) where t > 0 select t,
				from t in from p in db.Types select Math.Abs(p.MoneyValue) where t > 0 select t));
		}

		[Test]
		public void Acos()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Acos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Asin()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Asin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Atan()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Atan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Atan2()
		{
			ForEachProvider(new[] { ProviderName.Access }, db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Atan2((double)p.MoneyValue / 15, 0) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Ceiling1()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t,
				from t in from p in db.Types select Math.Ceiling(-(p.MoneyValue + 1)) where t != 0 select t));
		}

		[Test]
		public void Ceiling2()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Ceiling(p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Ceiling(p.MoneyValue) where t != 0 select t));
		}

		[Test]
		public void Cos()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Cos((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Cosh()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Cosh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Cot()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15).Value * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Sql.Cot((double)p.MoneyValue / 15).Value * 15) where t != 0.1 select t));
		}

		[Test]
		public void Deegrees1()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Sql.Degrees(p.MoneyValue).Value) where t != 0.1m select t,
				from t in from p in db.Types select Math.Floor(Sql.Degrees(p.MoneyValue).Value) where t != 0.1m select t));
		}

		[Test]
		public void Deegrees2()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Sql.Degrees((double)p.MoneyValue).Value where t != 0.1 select Math.Floor(t),
				from t in from p in db.Types select Sql.Degrees((double)p.MoneyValue).Value where t != 0.1 select Math.Floor(t)));
		}

		[Test]
		public void Deegrees3()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Sql.Degrees((int)p.MoneyValue).Value where t != 0.1 select t,
				from t in from p in db.Types select Sql.Degrees((int)p.MoneyValue).Value where t != 0.1 select t));
		}

		[Test]
		public void Exp()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Exp((double)p.MoneyValue)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Exp((double)p.MoneyValue)) where t != 0.1 select t));
		}

		[Test]
		public void Floor()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(-(p.MoneyValue + 1)) where t != 0 select t,
				from t in from p in db.Types select Math.Floor(-(p.MoneyValue + 1)) where t != 0 select t));
		}

		[Test]
		public void Log()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue)) where t != 0.1 select t));
		}

		[Test]
		public void Log2()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Log((double)p.MoneyValue, 2)) where t != 0.1 select t));
		}

		[Test]
		public void Log10()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Log10((double)p.MoneyValue)) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Log10((double)p.MoneyValue)) where t != 0.1 select t));
		}

		[Test]
		public void Max()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Max(p.MoneyValue, 5) where t != 0 select t,
				from t in from p in db.Types select Math.Max(p.MoneyValue, 5) where t != 0 select t));
		}

		[Test]
		public void Min()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Min(p.MoneyValue, 5) where t != 0 select t,
				from t in from p in db.Types select Math.Min(p.MoneyValue, 5) where t != 0 select t));
		}

		[Test]
		public void Pow()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t,
				from t in from p in db.Types select Math.Floor(Math.Pow((double)p.MoneyValue, 3)) where t != 0 select t));
		}

		[Test]
		public void Round1()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round(p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue) where t != 0 select t));
		}

		[Test]
		public void Round2()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round((double)p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Round((double)p.MoneyValue) where t != 0 select t));
		}

		[Test]
		public void Round3()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round(p.MoneyValue, 1) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, 1) where t != 0 select t));
		}

		[Test]
		public void Round4()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5),
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1) where t != 0 select Math.Round(t, 5)));
		}

		[Test]
		public void Round5()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t));
		}

		[Test]
		public void Round6()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t,
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.AwayFromZero) where t != 0 select t));
		}

		[Test]
		public void Round7()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t));
		}

		[Test]
		public void Round8()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t,
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, MidpointRounding.ToEven) where t != 0 select t));
		}

		[Test]
		public void Round9()
		{
			ForEachProvider(new[] { ProviderName.SQLite }, db => AreEqual(
				from t in from p in    Types select Math.Round(p.MoneyValue, 1, MidpointRounding.AwayFromZero) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, 1, MidpointRounding.AwayFromZero) where t != 0 select t));
		}

		[Test]
		public void Round10()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select t));
		}

		[Test]
		public void Round11()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5),
				from t in from p in db.Types select Math.Round((double)p.MoneyValue, 1, MidpointRounding.ToEven) where t != 0 select Math.Round(t, 5)));
		}

		[Test]
		public void Round12()
		{
			var mp = MidpointRounding.AwayFromZero;

			ForEachProvider(new[] { ProviderName.SQLite }, db => AreEqual(
				from t in from p in    Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 select t,
				from t in from p in db.Types select Math.Round(p.MoneyValue, 1, mp) where t != 0 select t));
		}

		[Test]
		public void Sign()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Sign(p.MoneyValue) where t != 0 select t,
				from t in from p in db.Types select Math.Sign(p.MoneyValue) where t != 0 select t));
		}

		[Test]
		public void Sin()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Sin((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Sinh()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Sinh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Sqrt()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Sqrt((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Tan()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Tan((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Tanh()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t,
				from t in from p in db.Types select Math.Floor(Math.Tanh((double)p.MoneyValue / 15) * 15) where t != 0.1 select t));
		}

		[Test]
		public void Truncate1()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t,
				from t in from p in db.Types select Math.Truncate(p.MoneyValue) where t != 0.1m select t));
		}

		[Test]
		public void Truncate2()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t,
				from t in from p in db.Types select Math.Truncate((double)-p.MoneyValue) where t != 0.1 select t));
		}
	}
}
