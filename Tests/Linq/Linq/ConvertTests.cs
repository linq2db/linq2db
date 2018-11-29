using System;
using System.Data.SqlTypes;
using System.Linq;

using LinqToDB;

using NUnit.Framework;
using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ConvertTests : TestBase
	{
		[Test, DataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void Test1(string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from t in db.Types where t.MoneyValue * t.ID == 1.11m  select t).Single().ID);
		}

		#region Int

		[Test, DataContextSource]
		public void ToInt1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.ConvertTo<int>.From(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.ConvertTo<int>.From(t.MoneyValue)));
		}

		[Test, DataContextSource]
		public void ToInt2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Convert<int,decimal>(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.Convert<int,decimal>(t.MoneyValue)));
		}

		[Test, DataContextSource(ProviderName.MySql, TestProvName.MariaDB, TestProvName.MySql57)]
		public void ToBigInt(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.BigInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.BigInt, t.MoneyValue));
		}

		[Test, DataContextSource(ProviderName.MySql)]
		public void ToInt64(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int64)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int64)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.MySql, ProviderName.SQLiteMS)]
		public void ConvertToInt64(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ToInt(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Int, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.Int, t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToInt32(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int32)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int32)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.SQLiteMS)]
		public void ConvertToInt32(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ToSmallInt(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.SmallInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.SmallInt, t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToInt16(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int16)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int16)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.SQLiteMS)]
		public void ConvertToInt16(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ToTinyInt(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.TinyInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.TinyInt, t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToSByte(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (sbyte)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (sbyte)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.SQLiteMS)]
		public void ConvertToSByte(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region UInts

		[Test, DataContextSource(ProviderName.MySql)]
		public void ToUInt1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.ConvertTo<uint>.From(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.ConvertTo<uint>.From(t.MoneyValue)));
		}

		[Test, DataContextSource(ProviderName.MySql)]
		public void ToUInt2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Convert<uint,decimal>(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.Convert<uint,decimal>(t.MoneyValue)));
		}

		[Test, DataContextSource(ProviderName.MySql)]
		public void ToUInt64(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (UInt64)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt64)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.MySql, ProviderName.SQLiteMS)]
		public void ConvertToUInt64(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.MySql)]
		public void ToUInt32(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (UInt32)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt32)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.MySql, ProviderName.SQLiteMS)]
		public void ConvertToUInt32(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ToUInt16(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (UInt16)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt16)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.SQLiteMS)]
		public void ConvertToUInt16(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ToByte(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (byte)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (byte)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource(ProviderName.SQLiteMS)]
		public void ConvertToByte(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToByte(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region Floats

		[Test, DataContextSource]
		public void ToDefaultDecimal(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultDecimal, t.MoneyValue * 1000),
					from t in db.Types select Sql.Convert(Sql.DefaultDecimal, t.MoneyValue * 1000));
		}

		[Test, DataContextSource]
		public void ToDecimal1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Decimal(10), t.MoneyValue * 1000),
					from t in db.Types select Sql.Convert(Sql.Decimal(10), t.MoneyValue * 1000));
		}

		[Test, DataContextSource]
		public void ToDecimal2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToDecimal3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Decimal)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Decimal)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ConvertToDecimal(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ToMoney(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Money, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Money, t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToSmallMoney(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue),
					from t in db.Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToSqlFloat(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Float, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Float, t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToDouble(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (int)(Double)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (int)(Double)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ConvertToDouble(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p,
					from p in from t in db.Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p);
		}

		[Test, DataContextSource]
		public void ToSqlReal(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Real, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Real, t.MoneyValue));
		}

		[Test, DataContextSource]
		public void ToSingle(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Single)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Single)t.MoneyValue where p > 0 select p);
		}

		[Test, DataContextSource]
		public void ConvertToSingle(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p,
					from p in from t in db.Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p);
		}

		#endregion

		#region DateTime

		[Test, DataContextSource]
		public void ToSqlDateTime(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test, DataContextSource]
		public void ToSqlDateTime2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test, DataContextSource]
		public void ToSqlSmallDateTime(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test, DataContextSource]
		public void ToSqlDate(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Date, t.DateTimeValue.Year + "-01-01"),
					from t in db.Types select Sql.Convert(Sql.Date, t.DateTimeValue.Year + "-01-01"));
		}

		// needs debugging, but suspect it fails due to issue 730
		[ActiveIssue(730, Configuration = ProviderName.Sybase, SkipForNonLinqService = true)]
		[ActiveIssue(730, Configuration = ProviderName.SybaseManaged, SkipForNonLinqService = true)]
		[Test, DataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS
			, ProviderName.Access
			)]
		public void ToSqlTime(string context)
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

		[Test, DataContextSource]
		public void ToSqlDateTimeOffset(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select ToDateTime(Sql.Convert(Sql.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")),
					from t in db.Types select ToDateTime(Sql.Convert(Sql.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")));
		}

		[Test, DataContextSource]
		public void ToDateTime(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
					from p in from t in db.Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		[Test, DataContextSource]
		public void ConvertToDateTime(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
					from p in from t in db.Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		#endregion

		#region String

		[Test, DataContextSource]
		public void ToChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Char(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.Char(20), t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource(ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Firebird, TestProvName.Firebird3,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93)]
		public void ToDefaultChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultChar, t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource]
		public void ToVarChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.VarChar(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.VarChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource(ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.PostgreSQL)]
		public void ToDefaultVarChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultVarChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultVarChar, t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource]
		public void ToNChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.NChar(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.NChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource(ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Firebird, TestProvName.Firebird3, TestProvName.Firebird3,
			ProviderName.PostgreSQL, ProviderName.PostgreSQL92, ProviderName.PostgreSQL93)]
		public void ToDefaultNChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultNChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultNChar, t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource]
		public void ToNVarChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.NVarChar(20), t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.NVarChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource(ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.PostgreSQL)]
		public void ToDefaultNVarChar(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultNVarChar, t.MoneyValue).ToInvariantString(),
					from t in db.Types select Sql.Convert(Sql.DefaultNVarChar, t.MoneyValue).ToInvariantString());
		}

		[Test, DataContextSource]
		public void DecimalToString(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd('0', '.'),
					from p in from t in db.Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd('0', '.'));
		}

		[Test, DataContextSource, Category("WindowsOnly")]
		public void ByteToString(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select ((byte)t.ID).ToString() where p.Length > 0 select p,
					from p in from t in db.Types select ((byte)t.ID).ToString() where p.Length > 0 select p);
		}

		[Test, DataContextSource]
		public void GuidToString(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where Sql.ConvertTo<string>.From(t.GuidValue) == "febe3eca-cb5f-40b2-ad39-2979d312afca" select t.GuidValue,
					from t in db.Types where Sql.ConvertTo<string>.From(t.GuidValue) == "febe3eca-cb5f-40b2-ad39-2979d312afca" select t.GuidValue);
		}

		#endregion

		#region Boolean

		[Test, DataContextSource]
		public void ToBit1(string context)
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

		[Test, DataContextSource]
		public void ToBit2(string context)
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

		[Test, DataContextSource]
		public void ConvertToBoolean1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToBoolean(t.MoneyValue) where p == true select p,
					from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue) where p == true select p);
		}

		[Test, DataContextSource]
		public void ConvertToBoolean2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p,
					from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p);
		}

		#endregion

		[Test, DataContextSource]
		public void ConvertFromOneToAnother(string context)
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

		[Test, NorthwindDataContext]
		public void ConvertDataToDecimal(string context)
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

		[Test, NorthwindDataContext]
		public void ConvertDataToDecimalNoConvert(string context)
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
	}
}
