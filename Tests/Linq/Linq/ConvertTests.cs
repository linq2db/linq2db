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

		#region issue 2166
		class Issue2166Table
		{
			public sbyte   SByte  { get; set; }
			public byte    Byte   { get; set; }
			public short   Int16  { get; set; }
			public ushort  UInt16 { get; set; }
			public int     Int32  { get; set; }
			public uint    UInt32 { get; set; }
			public long    Int64  { get; set; }
			public ulong   UInt64 { get; set; }

			public sbyte?  SByteN  { get; set; }
			public byte?   ByteN   { get; set; }
			public short?  Int16N  { get; set; }
			public ushort? UInt16N { get; set; }
			public int?    Int32N  { get; set; }
			public uint?   UInt32N { get; set; }
			public long?   Int64N  { get; set; }
			public ulong?  UInt64N { get; set; }
		}

		enum EnumByte   : byte   { NotParsed = 4 }
		enum EnumSByte  : sbyte  { NotParsed = 4 }
		enum EnumInt16  : short  { NotParsed = 4 }
		enum EnumUInt16 : ushort { NotParsed = 4 }
		enum EnumInt32           { NotParsed = 4 }
		enum EnumUInt32 : uint   { NotParsed = 4 }
		enum EnumInt64  : long   { NotParsed = 4 }
		enum EnumUInt64 : ulong  { NotParsed = 4 }

		[Test]
		public void Issue2166TestByte([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumByte.NotParsed
							 || x.SByte   == (sbyte )EnumByte.NotParsed
							 || x.Int16   == (short )EnumByte.NotParsed
							 || x.UInt16  == (ushort)EnumByte.NotParsed
							 || x.Int32   == (int   )EnumByte.NotParsed
							 || x.UInt32  == (uint  )EnumByte.NotParsed
							 || x.Int64   == (long  )EnumByte.NotParsed
							 || x.UInt64  == (ulong )EnumByte.NotParsed
							 || x.ByteN   == (byte  )EnumByte.NotParsed
							 || x.SByteN  == (sbyte )EnumByte.NotParsed
							 || x.Int16N  == (short )EnumByte.NotParsed
							 || x.UInt16N == (ushort)EnumByte.NotParsed
							 || x.Int32N  == (int   )EnumByte.NotParsed
							 || x.UInt32N == (uint  )EnumByte.NotParsed
							 || x.Int64N  == (long  )EnumByte.NotParsed
							 || x.UInt64N == (ulong )EnumByte.NotParsed

							 || (byte  )EnumByte.NotParsed == x.Byte
							 || (sbyte )EnumByte.NotParsed == x.SByte
							 || (short )EnumByte.NotParsed == x.Int16
							 || (ushort)EnumByte.NotParsed == x.UInt16
							 || (int   )EnumByte.NotParsed == x.Int32
							 || (uint  )EnumByte.NotParsed == x.UInt32
							 || (long  )EnumByte.NotParsed == x.Int64
							 || (ulong )EnumByte.NotParsed == x.UInt64
							 || (byte  )EnumByte.NotParsed == x.ByteN
							 || (sbyte )EnumByte.NotParsed == x.SByteN
							 || (short )EnumByte.NotParsed == x.Int16N
							 || (ushort)EnumByte.NotParsed == x.UInt16N
							 || (int   )EnumByte.NotParsed == x.Int32N
							 || (uint  )EnumByte.NotParsed == x.UInt32N
							 || (long  )EnumByte.NotParsed == x.Int64N
							 || (ulong )EnumByte.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		[Test]
		public void Issue2166TestSByte([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumSByte.NotParsed
							 || x.SByte   == (sbyte )EnumSByte.NotParsed
							 || x.Int16   == (short )EnumSByte.NotParsed
							 || x.UInt16  == (ushort)EnumSByte.NotParsed
							 || x.Int32   == (int   )EnumSByte.NotParsed
							 || x.UInt32  == (uint  )EnumSByte.NotParsed
							 || x.Int64   == (long  )EnumSByte.NotParsed
							 || x.UInt64  == (ulong )EnumSByte.NotParsed
							 || x.ByteN   == (byte  )EnumSByte.NotParsed
							 || x.SByteN  == (sbyte )EnumSByte.NotParsed
							 || x.Int16N  == (short )EnumSByte.NotParsed
							 || x.UInt16N == (ushort)EnumSByte.NotParsed
							 || x.Int32N  == (int   )EnumSByte.NotParsed
							 || x.UInt32N == (uint  )EnumSByte.NotParsed
							 || x.Int64N  == (long  )EnumSByte.NotParsed
							 || x.UInt64N == (ulong )EnumSByte.NotParsed

							 || (byte  )EnumSByte.NotParsed == x.Byte
							 || (sbyte )EnumSByte.NotParsed == x.SByte
							 || (short )EnumSByte.NotParsed == x.Int16
							 || (ushort)EnumSByte.NotParsed == x.UInt16
							 || (int   )EnumSByte.NotParsed == x.Int32
							 || (uint  )EnumSByte.NotParsed == x.UInt32
							 || (long  )EnumSByte.NotParsed == x.Int64
							 || (ulong )EnumSByte.NotParsed == x.UInt64
							 || (byte  )EnumSByte.NotParsed == x.ByteN
							 || (sbyte )EnumSByte.NotParsed == x.SByteN
							 || (short )EnumSByte.NotParsed == x.Int16N
							 || (ushort)EnumSByte.NotParsed == x.UInt16N
							 || (int   )EnumSByte.NotParsed == x.Int32N
							 || (uint  )EnumSByte.NotParsed == x.UInt32N
							 || (long  )EnumSByte.NotParsed == x.Int64N
							 || (ulong )EnumSByte.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		[Test]
		public void Issue2166TestInt16([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumInt16.NotParsed
							 || x.SByte   == (sbyte )EnumInt16.NotParsed
							 || x.Int16   == (short )EnumInt16.NotParsed
							 || x.UInt16  == (ushort)EnumInt16.NotParsed
							 || x.Int32   == (int   )EnumInt16.NotParsed
							 || x.UInt32  == (uint  )EnumInt16.NotParsed
							 || x.Int64   == (long  )EnumInt16.NotParsed
							 || x.UInt64  == (ulong )EnumInt16.NotParsed
							 || x.ByteN   == (byte  )EnumInt16.NotParsed
							 || x.SByteN  == (sbyte )EnumInt16.NotParsed
							 || x.Int16N  == (short )EnumInt16.NotParsed
							 || x.UInt16N == (ushort)EnumInt16.NotParsed
							 || x.Int32N  == (int   )EnumInt16.NotParsed
							 || x.UInt32N == (uint  )EnumInt16.NotParsed
							 || x.Int64N  == (long  )EnumInt16.NotParsed
							 || x.UInt64N == (ulong )EnumInt16.NotParsed

							 || (byte  )EnumInt16.NotParsed == x.Byte
							 || (sbyte )EnumInt16.NotParsed == x.SByte
							 || (short )EnumInt16.NotParsed == x.Int16
							 || (ushort)EnumInt16.NotParsed == x.UInt16
							 || (int   )EnumInt16.NotParsed == x.Int32
							 || (uint  )EnumInt16.NotParsed == x.UInt32
							 || (long  )EnumInt16.NotParsed == x.Int64
							 || (ulong )EnumInt16.NotParsed == x.UInt64
							 || (byte  )EnumInt16.NotParsed == x.ByteN
							 || (sbyte )EnumInt16.NotParsed == x.SByteN
							 || (short )EnumInt16.NotParsed == x.Int16N
							 || (ushort)EnumInt16.NotParsed == x.UInt16N
							 || (int   )EnumInt16.NotParsed == x.Int32N
							 || (uint  )EnumInt16.NotParsed == x.UInt32N
							 || (long  )EnumInt16.NotParsed == x.Int64N
							 || (ulong )EnumInt16.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		[Test]
		public void Issue2166TestUInt16([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumUInt16.NotParsed
							 || x.SByte   == (sbyte )EnumUInt16.NotParsed
							 || x.Int16   == (short )EnumUInt16.NotParsed
							 || x.UInt16  == (ushort)EnumUInt16.NotParsed
							 || x.Int32   == (int   )EnumUInt16.NotParsed
							 || x.UInt32  == (uint  )EnumUInt16.NotParsed
							 || x.Int64   == (long  )EnumUInt16.NotParsed
							 || x.UInt64  == (ulong )EnumUInt16.NotParsed
							 || x.ByteN   == (byte  )EnumUInt16.NotParsed
							 || x.SByteN  == (sbyte )EnumUInt16.NotParsed
							 || x.Int16N  == (short )EnumUInt16.NotParsed
							 || x.UInt16N == (ushort)EnumUInt16.NotParsed
							 || x.Int32N  == (int   )EnumUInt16.NotParsed
							 || x.UInt32N == (uint  )EnumUInt16.NotParsed
							 || x.Int64N  == (long  )EnumUInt16.NotParsed
							 || x.UInt64N == (ulong )EnumUInt16.NotParsed

							 || (byte  )EnumUInt16.NotParsed == x.Byte
							 || (sbyte )EnumUInt16.NotParsed == x.SByte
							 || (short )EnumUInt16.NotParsed == x.Int16
							 || (ushort)EnumUInt16.NotParsed == x.UInt16
							 || (int   )EnumUInt16.NotParsed == x.Int32
							 || (uint  )EnumUInt16.NotParsed == x.UInt32
							 || (long  )EnumUInt16.NotParsed == x.Int64
							 || (ulong )EnumUInt16.NotParsed == x.UInt64
							 || (byte  )EnumUInt16.NotParsed == x.ByteN
							 || (sbyte )EnumUInt16.NotParsed == x.SByteN
							 || (short )EnumUInt16.NotParsed == x.Int16N
							 || (ushort)EnumUInt16.NotParsed == x.UInt16N
							 || (int   )EnumUInt16.NotParsed == x.Int32N
							 || (uint  )EnumUInt16.NotParsed == x.UInt32N
							 || (long  )EnumUInt16.NotParsed == x.Int64N
							 || (ulong )EnumUInt16.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		[Test]
		public void Issue2166TestInt32([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumInt32.NotParsed
							 || x.SByte   == (sbyte )EnumInt32.NotParsed
							 || x.Int16   == (short )EnumInt32.NotParsed
							 || x.UInt16  == (ushort)EnumInt32.NotParsed
							 || x.Int32   == (int   )EnumInt32.NotParsed
							 || x.UInt32  == (uint  )EnumInt32.NotParsed
							 || x.Int64   == (long  )EnumInt32.NotParsed
							 || x.UInt64  == (ulong )EnumInt32.NotParsed
							 || x.ByteN   == (byte  )EnumInt32.NotParsed
							 || x.SByteN  == (sbyte )EnumInt32.NotParsed
							 || x.Int16N  == (short )EnumInt32.NotParsed
							 || x.UInt16N == (ushort)EnumInt32.NotParsed
							 || x.Int32N  == (int   )EnumInt32.NotParsed
							 || x.UInt32N == (uint  )EnumInt32.NotParsed
							 || x.Int64N  == (long  )EnumInt32.NotParsed
							 || x.UInt64N == (ulong )EnumInt32.NotParsed

							 || (byte  )EnumInt32.NotParsed == x.Byte
							 || (sbyte )EnumInt32.NotParsed == x.SByte
							 || (short )EnumInt32.NotParsed == x.Int16
							 || (ushort)EnumInt32.NotParsed == x.UInt16
							 || (int   )EnumInt32.NotParsed == x.Int32
							 || (uint  )EnumInt32.NotParsed == x.UInt32
							 || (long  )EnumInt32.NotParsed == x.Int64
							 || (ulong )EnumInt32.NotParsed == x.UInt64
							 || (byte  )EnumInt32.NotParsed == x.ByteN
							 || (sbyte )EnumInt32.NotParsed == x.SByteN
							 || (short )EnumInt32.NotParsed == x.Int16N
							 || (ushort)EnumInt32.NotParsed == x.UInt16N
							 || (int   )EnumInt32.NotParsed == x.Int32N
							 || (uint  )EnumInt32.NotParsed == x.UInt32N
							 || (long  )EnumInt32.NotParsed == x.Int64N
							 || (ulong )EnumInt32.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		[Test]
		public void Issue2166TestUInt32([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumUInt32.NotParsed
							 || x.SByte   == (sbyte )EnumUInt32.NotParsed
							 || x.Int16   == (short )EnumUInt32.NotParsed
							 || x.UInt16  == (ushort)EnumUInt32.NotParsed
							 || x.Int32   == (int   )EnumUInt32.NotParsed
							 || x.UInt32  == (uint  )EnumUInt32.NotParsed
							 || x.Int64   == (long  )EnumUInt32.NotParsed
							 || x.UInt64  == (ulong )EnumUInt32.NotParsed
							 || x.ByteN   == (byte  )EnumUInt32.NotParsed
							 || x.SByteN  == (sbyte )EnumUInt32.NotParsed
							 || x.Int16N  == (short )EnumUInt32.NotParsed
							 || x.UInt16N == (ushort)EnumUInt32.NotParsed
							 || x.Int32N  == (int   )EnumUInt32.NotParsed
							 || x.UInt32N == (uint  )EnumUInt32.NotParsed
							 || x.Int64N  == (long  )EnumUInt32.NotParsed
							 || x.UInt64N == (ulong )EnumUInt32.NotParsed

							 || (byte  )EnumUInt32.NotParsed == x.Byte
							 || (sbyte )EnumUInt32.NotParsed == x.SByte
							 || (short )EnumUInt32.NotParsed == x.Int16
							 || (ushort)EnumUInt32.NotParsed == x.UInt16
							 || (int   )EnumUInt32.NotParsed == x.Int32
							 || (uint  )EnumUInt32.NotParsed == x.UInt32
							 || (long  )EnumUInt32.NotParsed == x.Int64
							 || (ulong )EnumUInt32.NotParsed == x.UInt64
							 || (byte  )EnumUInt32.NotParsed == x.ByteN
							 || (sbyte )EnumUInt32.NotParsed == x.SByteN
							 || (short )EnumUInt32.NotParsed == x.Int16N
							 || (ushort)EnumUInt32.NotParsed == x.UInt16N
							 || (int   )EnumUInt32.NotParsed == x.Int32N
							 || (uint  )EnumUInt32.NotParsed == x.UInt32N
							 || (long  )EnumUInt32.NotParsed == x.Int64N
							 || (ulong )EnumUInt32.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		[Test]
		public void Issue2166TestInt64([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumInt64.NotParsed
							 || x.SByte   == (sbyte )EnumInt64.NotParsed
							 || x.Int16   == (short )EnumInt64.NotParsed
							 || x.UInt16  == (ushort)EnumInt64.NotParsed
							 || x.Int32   == (int   )EnumInt64.NotParsed
							 || x.UInt32  == (uint  )EnumInt64.NotParsed
							 || x.Int64   == (long  )EnumInt64.NotParsed
							 || x.UInt64  == (ulong )EnumInt64.NotParsed
							 || x.ByteN   == (byte  )EnumInt64.NotParsed
							 || x.SByteN  == (sbyte )EnumInt64.NotParsed
							 || x.Int16N  == (short )EnumInt64.NotParsed
							 || x.UInt16N == (ushort)EnumInt64.NotParsed
							 || x.Int32N  == (int   )EnumInt64.NotParsed
							 || x.UInt32N == (uint  )EnumInt64.NotParsed
							 || x.Int64N  == (long  )EnumInt64.NotParsed
							 || x.UInt64N == (ulong )EnumInt64.NotParsed

							 || (byte  )EnumInt64.NotParsed == x.Byte
							 || (sbyte )EnumInt64.NotParsed == x.SByte
							 || (short )EnumInt64.NotParsed == x.Int16
							 || (ushort)EnumInt64.NotParsed == x.UInt16
							 || (int   )EnumInt64.NotParsed == x.Int32
							 || (uint  )EnumInt64.NotParsed == x.UInt32
							 || (long  )EnumInt64.NotParsed == x.Int64
							 || (ulong )EnumInt64.NotParsed == x.UInt64
							 || (byte  )EnumInt64.NotParsed == x.ByteN
							 || (sbyte )EnumInt64.NotParsed == x.SByteN
							 || (short )EnumInt64.NotParsed == x.Int16N
							 || (ushort)EnumInt64.NotParsed == x.UInt16N
							 || (int   )EnumInt64.NotParsed == x.Int32N
							 || (uint  )EnumInt64.NotParsed == x.UInt32N
							 || (long  )EnumInt64.NotParsed == x.Int64N
							 || (ulong )EnumInt64.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		[Test]
		public void Issue2166TestUInt64([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db    = new TestDataConnection(context))
			using (var table = db.CreateLocalTable<Issue2166Table>())
			{
				table
					.Where(x => x.Byte    == (byte  )EnumUInt64.NotParsed
							 || x.SByte   == (sbyte )EnumUInt64.NotParsed
							 || x.Int16   == (short )EnumUInt64.NotParsed
							 || x.UInt16  == (ushort)EnumUInt64.NotParsed
							 || x.Int32   == (int   )EnumUInt64.NotParsed
							 || x.UInt32  == (uint  )EnumUInt64.NotParsed
							 || x.Int64   == (long  )EnumUInt64.NotParsed
							 || x.UInt64  == (ulong )EnumUInt64.NotParsed
							 || x.ByteN   == (byte  )EnumUInt64.NotParsed
							 || x.SByteN  == (sbyte )EnumUInt64.NotParsed
							 || x.Int16N  == (short )EnumUInt64.NotParsed
							 || x.UInt16N == (ushort)EnumUInt64.NotParsed
							 || x.Int32N  == (int   )EnumUInt64.NotParsed
							 || x.UInt32N == (uint  )EnumUInt64.NotParsed
							 || x.Int64N  == (long  )EnumUInt64.NotParsed
							 || x.UInt64N == (ulong )EnumUInt64.NotParsed

							 || (byte  )EnumUInt64.NotParsed == x.Byte
							 || (sbyte )EnumUInt64.NotParsed == x.SByte
							 || (short )EnumUInt64.NotParsed == x.Int16
							 || (ushort)EnumUInt64.NotParsed == x.UInt16
							 || (int   )EnumUInt64.NotParsed == x.Int32
							 || (uint  )EnumUInt64.NotParsed == x.UInt32
							 || (long  )EnumUInt64.NotParsed == x.Int64
							 || (ulong )EnumUInt64.NotParsed == x.UInt64
							 || (byte  )EnumUInt64.NotParsed == x.ByteN
							 || (sbyte )EnumUInt64.NotParsed == x.SByteN
							 || (short )EnumUInt64.NotParsed == x.Int16N
							 || (ushort)EnumUInt64.NotParsed == x.UInt16N
							 || (int   )EnumUInt64.NotParsed == x.Int32N
							 || (uint  )EnumUInt64.NotParsed == x.UInt32N
							 || (long  )EnumUInt64.NotParsed == x.Int64N
							 || (ulong )EnumUInt64.NotParsed == x.UInt64N)
					.ToList();

				Assert.False(db.LastQuery.ToLower().Contains("convert"));
			}
		}

		#endregion
	}
}
