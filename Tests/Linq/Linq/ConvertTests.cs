using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ConvertTests : TestBase
	{
		[Test]
		public void Test1([DataSources(TestProvName.AllSQLite)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from t in db.Types where t.MoneyValue * t.ID == 1.11m  select t).Single().ID);
		}

		#region Int

		[Test]
		public void ToInt1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.ConvertTo<int>.From(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.ConvertTo<int>.From(t.MoneyValue)));
		}

		[Test]
		public void ToInt2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Convert<int,decimal>(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.Convert<int,decimal>(t.MoneyValue)));
		}

		[Test]
		public void ToBigInt([DataSources(TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Convert(Sql.BigInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.BigInt, t.MoneyValue));
		}

		[Test]
		public void ToInt64([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in Types select (Int64)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int64)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt64([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToInt([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Int, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.Int, t.MoneyValue));
		}

		[Test]
		public void ToInt32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int32)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int32)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt32([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToSmallInt([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.SmallInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.SmallInt, t.MoneyValue));
		}

		[Test]
		public void ToInt16([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int16)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int16)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt16([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToTinyInt([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.TinyInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.TinyInt, t.MoneyValue));
		}

		[Test]
		public void ToSByte([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (sbyte)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (sbyte)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToSByte([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region UInts

		[Test]
		public void ToUInt1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.ConvertTo<uint>.From(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.ConvertTo<uint>.From(t.MoneyValue)));
		}

		[Test]
		public void ToUInt2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Convert<uint, decimal>(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.Convert<uint, decimal>(t.MoneyValue)));
		}

		[Test]
		public void ToUInt64([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in Types select (UInt64)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt64)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt64([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToUInt32([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in Types select (UInt32)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt32)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt32([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p);
		}


		[Test]
		public void ToUInt16([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (UInt16)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt16)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt16([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToByte([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (byte)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (byte)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToByte([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToByte(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region Floats

		[Test]
		public void ToDefaultDecimal([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultDecimal, t.MoneyValue * 1000),
					from t in db.Types select Sql.Convert(Sql.DefaultDecimal, t.MoneyValue * 1000));
		}

		[Test]
		public void ToDecimal1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Decimal(10), t.MoneyValue * 1000),
					from t in db.Types select Sql.Convert(Sql.Decimal(10), t.MoneyValue * 1000));
		}

		[Test]
		public void ToDecimal2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue));
		}

		[Test]
		public void ToDecimal3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (decimal)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (decimal)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToDecimal([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToMoney([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Money, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Money, t.MoneyValue));
		}

		[Test]
		public void ToSmallMoney([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue),
					from t in db.Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue));
		}

		[Test]
		public void ToSqlFloat([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Float, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Float, t.MoneyValue));
		}

		[Test]
		public void ToDouble([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (int)(Double)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (int)(Double)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToDouble([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p,
					from p in from t in db.Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p);
		}

		[Test]
		public void ToSqlReal([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Real, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Real, t.MoneyValue));
		}

		[Test]
		public void ToSingle([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Single)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Single)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToSingle([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p,
					from p in from t in db.Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p);
		}

		#endregion

		#region DateTime

		[Test]
		public void ToSqlDateTime([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlDateTime2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlSmallDateTime([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlDate([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Date, t.DateTimeValue.Year + "-01-01"),
					from t in db.Types select Sql.Convert(Sql.Date, t.DateTimeValue.Year + "-01-01"));
		}

		// needs debugging, but suspect it fails due to issue 730
		[ActiveIssue(730, Configuration = TestProvName.AllSybase, SkipForNonLinqService = true)]
		[Test]
		public void ToSqlTime([DataSources(TestProvName.AllSQLite, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Time, t.DateTimeValue.Hour + ":01:01"),
					from t in db.Types select Sql.Convert(Sql.Time, t.DateTimeValue.Hour + ":01:01"));
		}

		DateTime ToDateTime(DateTimeOffset dto)
		{
			return new DateTime(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second);
		}

		[Test]
		public void ToSqlDateTimeOffset([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select ToDateTime(Sql.Convert(Sql.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")),
					from t in db.Types select ToDateTime(Sql.Convert(Sql.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")));
		}

		[Test]
		public void ToDateTime([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
					from p in from t in db.Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		[Test]
		public void ConvertToDateTime([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
					from p in from t in db.Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		#endregion

		#region String

		[Test]
		public void ToChar([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Char(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.Char(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToVarChar([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.VarChar(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.VarChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultVarChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultVarChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultVarChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToNChar([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.NChar(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.NChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultNChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultNChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultNChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToNVarChar([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.NVarChar(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.NVarChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultNVarChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultNVarChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultNVarChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void DecimalToString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd('0', '.'),
					from p in from t in db.Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd('0', '.'));
		}

		[Test, Category("WindowsOnly")]
		public void ByteToString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select ((byte)t.ID).ToString() where p.Length > 0 select p,
					from p in from t in db.Types select ((byte)t.ID).ToString() where p.Length > 0 select p);
		}

		[Test]
		public void GuidToString([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where Sql.ConvertTo<string>.From(t.GuidValue) == "febe3eca-cb5f-40b2-ad39-2979d312afca" select t.GuidValue,
					from t in db.Types where Sql.ConvertTo<string>.From(t.GuidValue) == "febe3eca-cb5f-40b2-ad39-2979d312afca" select t.GuidValue);
		}

		#endregion

		#region Boolean

		[Test]
		public void ToBit1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in
						from t in GetTypes(context)
						where Sql.Convert(Sql.Bit, t.MoneyValue)
						select t
					select t,
					from t in
						from t in db.Types
						where Sql.Convert(Sql.Bit, t.MoneyValue)
						select t
					select t);
		}

		[Test]
		public void ToBit2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in
						from t in GetTypes(context)
						where !Sql.Convert(Sql.Bit, t.MoneyValue - 4.5m)
						select t
					select t
					,
					from t in
						from t in db.Types
						where !Sql.Convert(Sql.Bit, t.MoneyValue - 4.5m)
						select t
					select t);
		}

		[Test]
		public void ConvertToBoolean1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToBoolean(t.MoneyValue) where p == true select p,
					from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue) where p == true select p);
		}

		[Test]
		public void ConvertToBoolean2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p,
					from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p);
		}

		#endregion

		[Test]
		public void ConvertFromOneToAnother([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var decimalValue = 6579.64648m;
				var floatValue   = 6579.64648f;
				var doubleValue  = 6579.64648d;

				AssertConvert(db, decimalValue, decimalValue);
				AssertConvert(db, decimalValue, floatValue);
				AssertConvert(db, decimalValue, doubleValue);

				AssertConvert(db, floatValue, decimalValue);
				AssertConvert(db, floatValue, floatValue);
				AssertConvert(db, floatValue, doubleValue);

				AssertConvert(db, doubleValue, decimalValue);
				AssertConvert(db, doubleValue, floatValue);
				AssertConvert(db, doubleValue, doubleValue);
			}
		}

		static void AssertConvert<TTo, TFrom>(Model.ITestDataContext db, TTo expected, TFrom value)
		{
			var r = db.Types.Select(_ => ServerConvert<TTo, TFrom>(value)).First();

#if !APPVEYOR
			Console.WriteLine($"Expected {expected} result {r}");
#endif

			Assert.GreaterOrEqual(0.01m,
				Math.Abs(LinqToDB.Common.Convert<TTo, decimal>.From(expected) - LinqToDB.Common.Convert<TTo, decimal>.From(r)));
		}

		//[CLSCompliant(false)]
		[Sql.Function("$Convert$", 1, 2, 0, ServerSideOnly = true)]
		public static TTo ServerConvert<TTo, TFrom>(TFrom obj)
		{
			throw new NotImplementedException();
		}

		[Test]
		public void ConvertDataToDecimal([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var actual = (from od in db.OrderDetail
							  select
							  Sql.AsSql(od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount))).ToArray();

				var expected = (from od in db.OrderDetail
								select
								od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)).ToArray();

				Assert.AreEqual(actual.Length, expected.Length);

				for (var i = 0; i < actual.Length; i++)
				{
					Assert.GreaterOrEqual(0.01m, Math.Abs(actual[i] - expected[i]));
				}
			}
		}

		[Test]
		public void ConvertDataToDecimalNoConvert([NorthwindDataContext] string context)
		{
			using (var db = new NorthwindDB(context))
			{
				var qActual =
					from od in db.OrderDetail
					select
						Sql.NoConvert(od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount));

				var qExpected =
					from od in db.OrderDetail
					select
						Sql.AsSql(od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount));

				var sqlActual   = qActual.  ToString();
				var sqlExpected = qExpected.ToString();

				Assert.That(sqlActual,   Is.Not.Contains   ("Convert").Or.Contains("Cast"));
				Assert.That(sqlExpected, Contains.Substring("Convert").Or.Contains("Cast"));

				var actual   = qActual.  ToArray();
				var expected = qExpected.ToArray();

				Assert.AreEqual(actual.Length, expected.Length);

				for (var i = 0; i < actual.Length; i++)
				{
					Assert.GreaterOrEqual(0.01m, Math.Abs(actual[i] - expected[i]));
				}
			}
		}

		#region redundant convert https://github.com/linq2db/linq2db/issues/2039

		[Table]
		public class IntegerConverts
		{
			[Column] public int Id { get; set; }

			[Column] public byte    Byte    { get; set; }
			[Column] public sbyte   SByte   { get; set; }
			[Column] public short   Int16   { get; set; }
			[Column] public ushort  UInt16  { get; set; }
			[Column] public int     Int32   { get; set; }
			[Column] public uint    UInt32  { get; set; }
			[Column] public long    Int64   { get; set; }
			[Column] public ulong   UInt64  { get; set; }

			[Column] public byte?   ByteN   { get; set; }
			[Column] public sbyte?  SByteN  { get; set; }
			[Column] public short?  Int16N  { get; set; }
			[Column] public ushort? UInt16N { get; set; }
			[Column] public int?    Int32N  { get; set; }
			[Column] public uint?   UInt32N { get; set; }
			[Column] public long?   Int64N  { get; set; }
			[Column] public ulong?  UInt64N { get; set; }

			public static IntegerConverts[] Seed { get; }
				= new[]
				{
					new IntegerConverts() { Id = 1 },
				};
		}

		[Test]
		public void TestNoConvert_Byte([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Byte equals y.Byte
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Byte([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Byte == x.Byte)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_SByte([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.SByte equals y.SByte
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_SByte([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.SByte == x.SByte)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_Int16([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int16 equals y.Int16
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int16([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int16 == x.Int16)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_UInt16([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt16 equals y.UInt16
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt16([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt16 == x.UInt16)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_Int32([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int32 equals y.Int32
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int32([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int32 == x.Int32)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_UInt32([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt32 equals y.UInt32
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt32([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt32 == x.UInt32)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_Int64([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int64 equals y.Int64
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int64([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int64 == x.Int64)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_UInt64([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt64 equals y.UInt64
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt64([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt64 == x.UInt64)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_ByteN([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.ByteN equals y.ByteN
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_ByteN([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.ByteN == x.ByteN)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_SByteN([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.SByteN equals y.SByteN
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_SByteN([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.SByteN == x.SByteN)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_Int16N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int16N equals y.Int16N
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int16N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int16N == x.Int16N)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_UInt16N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt16N equals y.UInt16N
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt16N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt16N == x.UInt16N)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_Int32N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int32N equals y.Int32N
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int32N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int32N == x.Int32N)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_UInt32N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt32N equals y.UInt32N
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt32N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt32N == x.UInt32N)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_Int64N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int64N equals y.Int64N
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int64N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int64N == x.Int64N)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvert_UInt64N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt64N equals y.UInt64N
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt64N([IncludeDataSources(false, TestProvName.AllSqlServer2005Plus)] string context)
		{
			using (var db = new TestDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt64N == x.UInt64N)
							select x;

				var res = query.Single();

				Assert.AreEqual(1, res.Id);
				Assert.False(db.LastQuery.Contains(" Convert("));
			}
		}
		#endregion
	}
}
