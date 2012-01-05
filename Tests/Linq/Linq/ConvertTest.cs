using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class ConvertTest : TestBase
	{
		[Test]
		public void Test1([DataContexts(ProviderName.SQLite)] string context)
		{
			using (var db = GetDataContext(context))
				Assert.AreEqual(1, (from t in db.Types where t.MoneyValue * t.ID == 1.11m  select t).Single().ID);
		}

		#region Int

		[Test]
		public void ToInt1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select              Sql.ConvertTo<int>.From(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.ConvertTo<int>.From(t.MoneyValue)));
		}

		[Test]
		public void ToInt2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select              Sql.Convert<int,decimal>(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.Convert<int,decimal>(t.MoneyValue)));
		}

		[Test]
		public void ToBigInt([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.BigInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.BigInt, t.MoneyValue));
		}

		[Test]
		public void ToInt64([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int64)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int64)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt64([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToInt([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Int, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.Int, t.MoneyValue));
		}

		[Test]
		public void ToInt32([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int32)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int32)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt32([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToSmallInt([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.SmallInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.SmallInt, t.MoneyValue));
		}

		[Test]
		public void ToInt16([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Int16)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Int16)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt16([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToTinyInt([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.TinyInt, t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.TinyInt, t.MoneyValue));
		}

		[Test]
		public void ToSByte([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (sbyte)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (sbyte)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToSByte([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region UInts

		[Test]
		public void ToUInt1([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select              Sql.ConvertTo<uint>.From(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.ConvertTo<uint>.From(t.MoneyValue)));
		}

		[Test]
		public void ToUInt2([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select              Sql.Convert<uint,decimal>(t.MoneyValue),
					from t in db.Types select Sql.AsSql(Sql.Convert<uint,decimal>(t.MoneyValue)));
		}

		[Test]
		public void ToUInt64([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (UInt64)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt64)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt64([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToUInt32([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (UInt32)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt32)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt32([DataContexts(ProviderName.MySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToUInt16([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (UInt16)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (UInt16)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt16([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToByte([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (byte)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (byte)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToByte([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToByte(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region Floats

		[Test]
		public void ToDefaultDecimal([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultDecimal, t.MoneyValue * 1000),
					from t in db.Types select Sql.Convert(Sql.DefaultDecimal, t.MoneyValue * 1000));
		}

		[Test]
		public void ToDecimal1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Decimal(10), t.MoneyValue * 1000),
					from t in db.Types select Sql.Convert(Sql.Decimal(10), t.MoneyValue * 1000));
		}

		[Test]
		public void ToDecimal2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue),
					from t in db.Types select Sql.Convert(Sql.Decimal(10,4), t.MoneyValue));
		}

		[Test]
		public void ToDecimal3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Decimal)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Decimal)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToDecimal([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p,
					from p in from t in db.Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToMoney([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Money, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Money, t.MoneyValue));
		}

		[Test]
		public void ToSmallMoney([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue),
					from t in db.Types select (decimal)Sql.Convert(Sql.SmallMoney, t.MoneyValue));
		}

		[Test]
		public void ToSqlFloat([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Float, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Float, t.MoneyValue));
		}

		[Test]
		public void ToDouble([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (int)(Double)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (int)(Double)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToDouble([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p,
					from p in from t in db.Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p);
		}

		[Test]
		public void ToSqlReal([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select (int)Sql.Convert(Sql.Real, t.MoneyValue),
					from t in db.Types select (int)Sql.Convert(Sql.Real, t.MoneyValue));
		}

		[Test]
		public void ToSingle([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select (Single)t.MoneyValue where p > 0 select p,
					from p in from t in db.Types select (Single)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToSingle([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p,
					from p in from t in db.Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p);
		}

		#endregion

		#region DateTime

		[Test]
		public void ToSqlDateTime([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlDateTime2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlSmallDateTime([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
					from t in db.Types select Sql.Convert(Sql.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlDate([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Date, t.DateTimeValue.Year + "-01-01"),
					from t in db.Types select Sql.Convert(Sql.Date, t.DateTimeValue.Year + "-01-01"));
		}

		[Test]
		public void ToSqlTime([DataContexts(ProviderName.SQLite)] string context)
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
		public void ToSqlDateTimeOffset([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select ToDateTime(Sql.Convert(Sql.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")),
					from t in db.Types select ToDateTime(Sql.Convert(Sql.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")));
		}

		[Test]
		public void ToDateTime([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
					from p in from t in db.Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		[Test]
		public void ConvertToDateTime([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
					from p in from t in db.Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		#endregion

		#region String

		[Test]
		public void ToChar([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.Char(20), t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.Char(20), t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void ToDefaultChar([DataContexts(
			ProviderName.Oracle, ProviderName.Firebird, ProviderName.PostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultChar, t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.DefaultChar, t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void ToVarChar([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.VarChar(20), t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.VarChar(20), t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void ToDefaultVarChar([DataContexts(
			ProviderName.Oracle, ProviderName.Firebird, ProviderName.PostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultVarChar, t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.DefaultVarChar, t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void ToNChar([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.NChar(20), t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.NChar(20), t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void ToDefaultNChar([DataContexts(ProviderName.Oracle, ProviderName.Firebird, ProviderName.PostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultNChar, t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.DefaultNChar, t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void ToNVarChar([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.NVarChar(20), t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.NVarChar(20), t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void ToDefaultNVarChar([DataContexts(ProviderName.Oracle, ProviderName.Firebird, ProviderName.PostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.Convert(Sql.DefaultNVarChar, t.MoneyValue).Trim(' ', '0', '.'),
					from t in db.Types select Sql.Convert(Sql.DefaultNVarChar, t.MoneyValue).Trim(' ', '0', '.'));
		}

		[Test]
		public void DecimalToString([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd('0', '.'),
					from p in from t in db.Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd('0', '.'));
		}

		[Test]
		public void ByteToString([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select ((byte)t.ID).ToString() where p.Length > 0 select p,
					from p in from t in db.Types select ((byte)t.ID).ToString() where p.Length > 0 select p);
		}

		[Test]
		public void GuidToString([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types where Sql.ConvertTo<string>.From(t.GuidValue) == "febe3eca-cb5f-40b2-ad39-2979d312afca" select t.GuidValue,
					from t in db.Types where Sql.ConvertTo<string>.From(t.GuidValue) == "febe3eca-cb5f-40b2-ad39-2979d312afca" select t.GuidValue);
		}

		#endregion

		#region Boolean

		[Test]
		public void ToBit1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from t in    Types where Sql.Convert(Sql.Bit, t.MoneyValue) select t select t,
					from t in from t in db.Types where Sql.Convert(Sql.Bit, t.MoneyValue) select t select t);
		}

		[Test]
		public void ToBit2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from t in    Types where !Sql.Convert(Sql.Bit, t.MoneyValue - 4.5m) select t select t,
					from t in from t in db.Types where !Sql.Convert(Sql.Bit, t.MoneyValue - 4.5m) select t select t);
		}

		[Test]
		public void ConvertToBoolean1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToBoolean(t.MoneyValue) where p == true select p,
					from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue) where p == true select p);
		}

		[Test]
		public void ConvertToBoolean2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in from t in    Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p,
					from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p);
		}

		#endregion
	}
}
