using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class ConvertTests : TestBase
	{
		[Test]
		public void Test1([DataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context);
			Assert.That((from t in db.Types where t.MoneyValue * t.ID == 1.11m select t).Single().ID, Is.EqualTo(1));
		}

		#region Int

		[Test]
		public void ToInt1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.ConvertTo<int>.From(t.MoneyValue),
				from t in db.Types select Sql.AsSql(Sql.ConvertTo<int>.From(t.MoneyValue)));
		}

		[Test]
		public void ToInt2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert<int, decimal>(t.MoneyValue),
				from t in db.Types select Sql.AsSql(Sql.Convert<int, decimal>(t.MoneyValue)));
		}

		[Test]
		public void ToBigInt([DataSources(TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.BigInt, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.Types.BigInt, t.MoneyValue));
		}

		[Test]
		public void ToBigInt2([DataSources(TestProvName.AllMySql)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.BigInt, t.MoneyValue),
				from t in db.Types select Sql.AsSql(Sql.Convert(Sql.Types.BigInt, t.MoneyValue)));
		}

		[Test]
		public void ToInt64([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (long)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (long)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt64([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToInt64(t.MoneyValue) where p > 0 select p);
		}

		sealed class WideDecimal
		{
			[PrimaryKey                                                  ] public int      Id { get; set; }
			[Column(Precision = 35, Scale = 2)                           ] public decimal  D  { get; set; }
			[Column(DbType = "Decimal(35, 2)", Precision = 35, Scale = 2)] public decimal  S  { get; set; }
			[Column(Precision = 35, Scale = 2)                           ] public decimal? N  { get; set; }
			[Column(Precision = 10, Scale = 2)                           ] public decimal  E  { get; set; }
			[Column(Precision =  4, Scale = 4)                           ] public decimal  F  { get; set; }
			[Column(Precision = 20, Scale = 12)                          ] public decimal  H  { get; set; }
		}

		// Past 2^53 a Double no longer holds every integer, so a Decimal->integer conversion must not pass through one.
		[Test]
		public void DecimalToInt64PastDoublePrecision([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new WideDecimal { Id = 1, D =  9007199254740993.25m, S =  9007199254740993.25m, N =  9007199254740993.25m, E =  3.50m, F =  0.1234m, H =  1.499999999999m },
				new WideDecimal { Id = 2, D =  9007199254740993.75m, S =  9007199254740993.75m, N =  9007199254740993.75m, E =  1.75m, F = -0.5000m, H = -1.499999999999m },
				new WideDecimal { Id = 3, D = -9007199254740993.75m, S = -9007199254740993.75m, N = -9007199254740993.75m, E = -2.25m, F =  0.9999m, H =  1.5m            },
				new WideDecimal { Id = 4, D = 12345678901234567.50m, S = 12345678901234567.50m, N = 12345678901234567.50m, E =  3.10m, F = -0.9999m, H =  0.25m           },
				new WideDecimal { Id = 5, D =  2.50m,                S =  2.50m,                N = null,                  E =  1.95m, F =  0.5000m, H = -0.75m           },
				new WideDecimal { Id = 6, D = -2.50m,                S = -2.50m,                N = -2.50m,                E = -0.50m, F =  0m,      H =  0m              },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					D        = Sql.AsSql((long)r.D),
					DRounded = Sql.AsSql(Convert.ToInt64(r.D)),
					S        = Sql.AsSql((long)r.S),
					SRounded = Sql.AsSql(Convert.ToInt64(r.S)),
					N        = Sql.AsSql((long?)r.N),
					Product  = Sql.AsSql((long)(r.E * r.E)),
					F        = Sql.AsSql((long)r.F),
					Sum      = Sql.AsSql((long)(r.H + r.H)),
					FProduct = Sql.AsSql((long)(r.F * r.F)),
				};

			AssertQuery(query);
		}

		sealed class DoubleStoredDecimal
		{
			[PrimaryKey                        ] public int     Id { get; set; }
			[Column(DataType = DataType.Double)] public decimal D  { get; set; }
		}

		// A decimal stored as Double is not a YQL Decimal: it truncates in Double, not through Decimal casts.
		[Test]
		public void DoubleStoredDecimalToInt64([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new DoubleStoredDecimal { Id = 1, D =  2.5m  },
				new DoubleStoredDecimal { Id = 2, D =  3.75m },
				new DoubleStoredDecimal { Id = 3, D = -2.5m  },
				new DoubleStoredDecimal { Id = 4, D = -3.75m },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					D        = Sql.AsSql((long)r.D),
					DRounded = Sql.AsSql(Convert.ToInt64(r.D)),
				};

			AssertQuery(query);
		}

		sealed class DecimalStoredDouble
		{
			[PrimaryKey                                                    ] public int    Id { get; set; }
			[Column(DataType = DataType.Decimal)                           ] public double A  { get; set; }
			[Column(DataType = DataType.Decimal, Precision = 22, Scale = 9)] public double L  { get; set; }
		}

		// A double stored as Decimal truncates exactly in Decimal: (long) and (int) cast it directly rather than through Text,
		// while Convert.ToInt64 rounds in Double first and narrows that result through Text.
		[Test]
		public void DecimalStoredDoubleToInt64([IncludeDataSources(true, TestProvName.AllYdb)] string context)
		{
			var data = new[]
			{
				new DecimalStoredDouble { Id = 1, A =  3.75, L =  3.75 },
				new DecimalStoredDouble { Id = 2, A = -3.75, L = -3.75 },
				new DecimalStoredDouble { Id = 3, A =  2.5,  L =  2.5  },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					A        = Sql.AsSql((long)r.A),
					ARounded = Sql.AsSql(Convert.ToInt64(r.A)),
					L        = Sql.AsSql((int)r.L),
				};

			AssertQuery(query);

			var truncation = t
				.Select(r => new
				{
					A = Sql.AsSql((long)r.A),
					L = Sql.AsSql((int)r.L),
				});

			truncation
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("AS Text");
		}

		sealed class DoubleValue
		{
			[PrimaryKey] public int    Id { get; set; }
			[Column    ] public double F  { get; set; }
		}

		// An integer cast between two floating values truncates, so the pair of casts cannot fold away. The rows stay positive:
		// several providers floor a negative fraction.
		// Access drops every numeric cast, so the value comes back untruncated.
		[ActiveIssue(6070, Configuration = TestProvName.AllAccess, ErrorMessage = "Truncated = 1 } <> { Id = 1, Truncated = 1.25 }")]
		[Test]
		public void DoubleThroughIntToDouble([DataSources] string context)
		{
			var data = new[]
			{
				new DoubleValue { Id = 1, F = 1.25 },
				new DoubleValue { Id = 2, F = 2.25 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					Truncated = Sql.AsSql((double)(int)r.F),
				};

			AssertQuery(query);
		}

		sealed class NumberValue
		{
			[PrimaryKey] public int    Id { get; set; }
			[Column    ] public int    I  { get; set; }
			[Column    ] public double F  { get; set; }
			[Column    ] public float  S  { get; set; }
		}

		// (decimal) is typed with the mapping's default decimal, which can hold fewer integer digits than the value: Decimal(18,10)
		// on SQL Server and Sybase, Decimal(22,9) on YDB. F and S have at most 15 / 7 significant digits, where C#'s (decimal) is
		// exact.
		[Test]
		public void NumberThroughDecimal([DataSources] string context)
		{
			var data = new[]
			{
				new NumberValue { Id = 1, I = 7,         F = 123456789.25, S = 1e8f },
				new NumberValue { Id = 2, I = 123456789, F = 987654321.75, S = 2e8f },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					I = Sql.AsSql((int)(decimal)r.I),
					F = Sql.AsSql((double)(decimal)r.F),
					S = Sql.AsSql((float)(decimal)r.S),
				};

			AssertQuery(query);
		}

		sealed class TextRoundTripValue
		{
			[PrimaryKey] public int    Id { get; set; }
			[Column    ] public double F  { get; set; }
			[Column    ] public float  S  { get; set; }
			[Column    ] public bool   B  { get; set; }
		}

		// SQL Server writes a float or real as text with 6 digits.
		[Test]
		public void ScalarThroughText([DataSources] string context)
		{
			var data = new[]
			{
				new TextRoundTripValue { Id = 1, F = 1.23456789, S = 1.234568f, B = true  },
				new TextRoundTripValue { Id = 2, F = 9.87654321, S = 9.876543f, B = false },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					F = Sql.AsSql(double.Parse(Sql.ConvertTo<string>.From(r.F))),
					S = Sql.AsSql(float.Parse(Sql.ConvertTo<string>.From(r.S))),
					B = Sql.AsSql(bool.Parse(Sql.ConvertTo<string>.From(r.B))),
				};

			AssertQuery(query);

			var same =
				from r in t
				where double.Parse(Sql.ConvertTo<string>.From(r.F)) == r.F
				select r.Id;

			AssertQuery(same);
		}

		sealed class Int64Value
		{
			[PrimaryKey] public int  Id { get; set; }
			[Column    ] public long L  { get; set; }
		}

		// The Int64 cells of NumberThroughDecimal and ScalarThroughText: 17 digits outgrow YDB's Decimal(22,9), DuckDB's
		// DECIMAL(18,3) and Informix's 16-digit DECIMAL.
		// Access has no 64-bit integer type: CREATE TABLE fails with "Syntax error in field definition".
		[Test]
		public void Int64ThroughDecimalAndText([DataSources(TestProvName.AllAccess)] string context)
		{
			var data = new[]
			{
				new Int64Value { Id = 1, L = 7                 },
				new Int64Value { Id = 2, L = 12345678901234567 },
			};

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(data);

			var query =
				from r in t
				orderby r.Id
				select new
				{
					r.Id,
					Decimal = Sql.AsSql((long)(decimal)r.L),
					Text    = Sql.AsSql(long.Parse(Sql.ConvertTo<string>.From(r.L))),
				};

			AssertQuery(query);
		}

		sealed class FoldPairRow
		{
			[PrimaryKey] public int      Id { get; set; }
			[Column    ] public float    S  { get; set; }
			[Column    ] public double   F  { get; set; }
			[Column    ] public DateTime D  { get; set; }
		}

		// A pair of casts folds only when the round trip loses nothing. The assertions read CAST text, which SQLite renders for
		// every cast (PostgreSQL writes ::, ClickHouse toInt32).
		[Test]
		public void CastPairFoldsOnlyWhenLossless([IncludeDataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable<FoldPairRow>();

			t
				.Select(r => Sql.AsSql((int)(long)r.Id))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql((float)(double)r.S))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql((int)(short)r.Id))
				.ToSqlQuery()
				.Sql
				.ShouldContain("CAST(");

			t
				.Select(r => Sql.AsSql((int)(uint)r.Id))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql(DateTime.Parse(Sql.ConvertTo<string>.From(r.D))))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql(double.Parse(Sql.ConvertTo<string>.From(r.F))))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql(float.Parse(Sql.ConvertTo<string>.From(r.S))))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql(int.Parse(Sql.ConvertTo<string>.From(r.Id))))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql((int)(decimal)r.Id))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");

			t
				.Select(r => Sql.AsSql((float)(decimal)r.S))
				.ToSqlQuery()
				.Sql
				.ShouldNotContain("CAST(");
		}

		[Test]
		public void ToInt([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.Int, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.Types.Int, t.MoneyValue));
		}

		[Test]
		public void ToInt32([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (int)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (int)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt32([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToInt32(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToSmallInt([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.SmallInt, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.Types.SmallInt, t.MoneyValue));
		}

		[Test]
		public void ToInt16([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (short)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (short)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToInt16([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToTinyInt([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.TinyInt, t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.Types.TinyInt, t.MoneyValue));
		}

		[Test]
		public void ToSByte([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (sbyte)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (sbyte)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToSByte([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToSByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region UInts

		[Test]
		public void ToUInt1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.ConvertTo<uint>.From(t.MoneyValue),
				from t in db.Types select Sql.AsSql(Sql.ConvertTo<uint>.From(t.MoneyValue)));
		}

		[Test]
		public void ToUInt2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert<uint, decimal>(t.MoneyValue),
				from t in db.Types select Sql.AsSql(Sql.Convert<uint, decimal>(t.MoneyValue)));
		}

		[Test]
		public void ToUInt64([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (ulong)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (ulong)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt64([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToUInt64(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToUInt32([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (uint)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (uint)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt32([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToUInt32(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToUInt16([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (ushort)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (ushort)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToUInt16([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToUInt16(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToByte([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (byte)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (byte)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToByte([DataSources(ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToByte(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToByte(t.MoneyValue) where p > 0 select p);
		}

		#endregion

		#region Floats

		[ActiveIssue(5592, Configuration = TestProvName.AllYdb, ErrorTypeName = "System.OverflowException", ErrorMessage = "Value does not fit into decimal",
			Details = "Ydb.Sdk UnpackDecimal throws OverflowException reading a computed Decimal result (client-side scale handling).")]
		[Test]
		public void ToDefaultDecimal([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.DefaultDecimal, t.MoneyValue * 1000),
				from t in db.Types select Sql.Convert(Sql.Types.DefaultDecimal, t.MoneyValue * 1000));
		}

		[ActiveIssue(5592, Configuration = TestProvName.AllYdb, ErrorTypeName = "System.OverflowException", ErrorMessage = "Value does not fit into decimal",
			Details = "Ydb.Sdk UnpackDecimal throws OverflowException reading a computed Decimal result (client-side scale handling).")]
		[Test]
		public void ToDecimal1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.Decimal(10), t.MoneyValue * 1000),
				from t in db.Types select Sql.Convert(Sql.Types.Decimal(10), t.MoneyValue * 1000));
		}

		[Test]
		public void ToDecimal2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.Decimal(10, 4), t.MoneyValue),
				from t in db.Types select Sql.Convert(Sql.Types.Decimal(10, 4), t.MoneyValue));
		}

		[Test]
		public void ToDecimal3([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (decimal)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (decimal)t.MoneyValue where p > 0 select p);
		}

		// providers disabled due to change in
		// https://github.com/linq2db/linq2db/pull/3690
		[Test]
		public void ConvertToDecimal([DataSources(
			ProviderName.DB2,
			TestProvName.AllFirebird,
			TestProvName.AllSqlServer,
			TestProvName.AllSybase,
			TestProvName.AllOracle,
			TestProvName.AllMySql,
			ProviderName.SqlCe
			)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p,
				from p in from t in db.Types select Convert.ToDecimal(t.MoneyValue) where p > 0 select p);
		}

		[Test]
		public void ToMoney([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select (int)Sql.Convert(Sql.Types.Money, t.MoneyValue),
				from t in db.Types select (int)Sql.Convert(Sql.Types.Money, t.MoneyValue));
		}

		[Test]
		public void ToSmallMoney([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select (decimal)Sql.Convert(Sql.Types.SmallMoney, t.MoneyValue),
				from t in db.Types select (decimal)Sql.Convert(Sql.Types.SmallMoney, t.MoneyValue));
		}

		[Test]
		public void ToSqlFloat([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select (int)Sql.Convert(Sql.Types.Float, t.MoneyValue),
				from t in db.Types select (int)Sql.Convert(Sql.Types.Float, t.MoneyValue));
		}

		[Test]
		public void ToDouble([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (int)(double)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (int)(double)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToDouble([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p,
				from p in from t in db.Types select Convert.ToDouble(t.MoneyValue) where p > 0 select (int)p);
		}

		[Test]
		public void ToSqlReal([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select (int)Sql.Convert(Sql.Types.Real, t.MoneyValue),
				from t in db.Types select (int)Sql.Convert(Sql.Types.Real, t.MoneyValue));
		}

		[Test]
		public void ToSingle([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select (Single)t.MoneyValue where p > 0 select p,
				from p in from t in db.Types select (Single)t.MoneyValue where p > 0 select p);
		}

		[Test]
		public void ConvertToSingle([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p,
				from p in from t in db.Types select Convert.ToSingle(t.MoneyValue) where p > 0 select (int)p);
		}

		#endregion

		#region DateTime

		[Test]
		public void ToSqlDateTime([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
				from t in db.Types select Sql.Convert(Sql.Types.DateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlDateTime2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"),
				from t in db.Types select Sql.Convert(Sql.Types.DateTime2, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlSmallDateTime([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"),
				from t in db.Types select Sql.Convert(Sql.Types.SmallDateTime, t.DateTimeValue.Year + "-01-01 00:20:00"));
		}

		[Test]
		public void ToSqlDate([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.Date, t.DateTimeValue.Year + "-01-01"),
				from t in db.Types select Sql.Convert(Sql.Types.Date, t.DateTimeValue.Year + "-01-01"));
		}

		[Test]
		public void ToSqlTime([DataSources(TestProvName.AllSQLite, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.Time, t.DateTimeValue.Hour + ":01:01"),
				from t in db.Types select Sql.Convert(Sql.Types.Time, t.DateTimeValue.Hour + ":01:01"));
		}

		[Test]
		public void ToSqlTimeSql([DataSources(TestProvName.AllSQLite, TestProvName.AllAccess, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.AsSql(Sql.Convert(Sql.Types.Time, t.DateTimeValue.Hour + ":01:01")),
				from t in db.Types select Sql.AsSql(Sql.Convert(Sql.Types.Time, t.DateTimeValue.Hour + ":01:01")));
		}

		DateTime ToDateTime(DateTimeOffset dto)
		{
			return new DateTime(dto.Year, dto.Month, dto.Day, dto.Hour, dto.Minute, dto.Second);
		}

		[Test]
		public void ToSqlDateTimeOffset([DataSources(ProviderName.SqlCe, TestProvName.AllDuckDB)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select ToDateTime(Sql.Convert(Sql.Types.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")),
				from t in db.Types select ToDateTime(Sql.Convert(Sql.Types.DateTimeOffset, t.DateTimeValue.Year + "-01-01 00:20:00")));
		}

		[Test]
		public void ToDateTime([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
				from p in from t in db.Types select DateTime.Parse(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		[Test]
		public void ConvertToDateTime([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p,
				from p in from t in db.Types select Convert.ToDateTime(t.DateTimeValue.Year + "-01-01 00:00:00") where p.Day > 0 select p);
		}

		#endregion

		#region String

		[Test]
		public void ToChar([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.Char(20), t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.Char(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.DefaultChar, t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.DefaultChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToVarChar([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.VarChar(20), t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.VarChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultVarChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird)]
			string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.DefaultVarChar, t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.DefaultVarChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToNChar([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.NChar(20), t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.NChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultNChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.DefaultNChar, t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.DefaultNChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToNVarChar([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.NVarChar(20), t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.NVarChar(20), t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void ToDefaultNVarChar([DataSources(TestProvName.AllOracle, TestProvName.AllFirebird)] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types select Sql.Convert(Sql.Types.DefaultNVarChar, t.MoneyValue).ToInvariantString(),
				from t in db.Types select Sql.Convert(Sql.Types.DefaultNVarChar, t.MoneyValue).ToInvariantString());
		}

		[Test]
		public void DecimalToString([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd('0', '.'),
				from p in from t in db.Types select Convert.ToString(t.MoneyValue) where p.Length > 0 select p.Replace(',', '.').TrimEnd(new char[] { '0', '.' }));
		}

		[Test]
		public void ByteToString([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select ((byte)t.ID).ToString() where p.Length > 0 select p,
				from p in from t in db.Types select ((byte)t.ID).ToString() where p.Length > 0 select p);
		}

		[Test]
		public void GuidToString([DataSources] string context)
		{
			var guid = "febe3eca-cb5f-40b2-ad39-2979d312afca";
			using var db = GetDataContext(context);
			AreEqual(
				from t in Types where Sql.ConvertTo<string>.From(t.GuidValue).ToLower() == guid select t.GuidValue,
				from t in db.Types where Sql.ConvertTo<string>.From(t.GuidValue).ToLower() == guid select t.GuidValue);
		}

		#endregion

		#region Boolean

		[Test]
		public void ToBit1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in
					from t in GetTypes(context)
					where Sql.Convert(Sql.Types.Bit, t.MoneyValue)
					select t
				select t,
				from t in
					from t in db.Types
					where Sql.Convert(Sql.Types.Bit, t.MoneyValue)
					select t
				select t);
		}

		[Test]
		public void ToBit2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from t in
					from t in GetTypes(context)
					where !Sql.Convert(Sql.Types.Bit, t.MoneyValue - 4.5m)
					select t
				select t
				,
				from t in
					from t in db.Types
					where !Sql.Convert(Sql.Types.Bit, t.MoneyValue - 4.5m)
					select t
				select t);
		}

		[Test]
		public void ConvertToBoolean1([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToBoolean(t.MoneyValue) where p == true select p,
				from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue) where p == true select p);
		}

		[Test]
		public void ConvertToBoolean2([DataSources] string context)
		{
			using var db = GetDataContext(context);
			AreEqual(
				from p in from t in Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p,
				from p in from t in db.Types select Convert.ToBoolean(t.MoneyValue - 4.5m) where !p select p);
		}

		#endregion

		[ActiveIssue(Configuration = ProviderName.DB2, ErrorTypeName = "IBM.Data.Db2.DB2Exception",
			ErrorMessage = "SQL0245N{0}The invocation of routine \"DECIMAL\" is ambiguous",
			Details = "no-issue: CI: SQL0245N  The invocation of routine DECIMAL is ambiguous. The argument in position 1 does not have a best fit. Nothing on the tracker covers it. The placeholder spans the two spaces DB2 pads after the code.")]
		[Test]
		public void ConvertFromOneToAnother([DataSources] string context)
		{
			// providers disabled due to change in
			// https://github.com/linq2db/linq2db/pull/3690
			var scaleLessDecimal = context.IsAnyOf(
				TestProvName.AllFirebird,
				TestProvName.AllSybase,
				TestProvName.AllOracle,
				TestProvName.AllMySql,
				ProviderName.SqlCe,
				TestProvName.AllSqlServer);

			using var db = GetDataContext(context);
			var decimalValue = 6579.64648m;
			var floatValue   = 6579.64648f;
			var doubleValue  = 6579.64648d;

			if (!scaleLessDecimal)
			{
				AssertConvert(db, decimalValue, decimalValue);
				AssertConvert(db, decimalValue, floatValue);
				AssertConvert(db, decimalValue, doubleValue);
			}

			AssertConvert(db, floatValue, decimalValue);
			AssertConvert(db, floatValue, floatValue);
			AssertConvert(db, floatValue, doubleValue);

			AssertConvert(db, doubleValue, decimalValue);
			AssertConvert(db, doubleValue, floatValue);
			AssertConvert(db, doubleValue, doubleValue);
		}

		static void AssertConvert<TTo, TFrom>(Model.ITestDataContext db, TTo expected, TFrom value)
		{
			var r = db.Types.Select(_ => ServerConvert<TTo, TFrom>(value)).First();

			TestContext.Out.WriteLine($"Expected {expected} result {r}");

			Assert.That(Math.Abs(Convert<TTo, decimal>.From(expected) - Convert<TTo, decimal>.From(r)), Is.LessThan(0.01m));
		}

		[ExpressionMethod(nameof(ServerConvertImp))]
		private static TTo ServerConvert<TTo, TFrom>(TFrom obj)
		{
			throw new ServerSideOnlyException(nameof(ServerConvert));
		}

		static Expression<Func<TFrom, TTo>> ServerConvertImp<TTo, TFrom>()
			=> obj => Sql.AsSql(Sql.Convert<TTo, TFrom>(obj));
		

		[Test]
		public void ConvertDataToDecimal([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var actual = (from od in db.OrderDetail
						  select
							  Sql.AsSql(od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount))).ToArray();

			var expected = (from od in db.OrderDetail
							select
								od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount)).ToArray();

			Assert.That(expected, Has.Length.EqualTo(actual.Length));

			for (var i = 0; i < actual.Length; i++)
			{
				Assert.That(Math.Abs(actual[i] - expected[i]), Is.LessThan(0.01m));
			}
		}

		[Test]
		public void ConvertDataToDecimalNoConvert([NorthwindDataContext] string context)
		{
			using var db = new NorthwindDB(context);
			var qActual =
					from od in db.OrderDetail
					select
						Sql.NoConvert(od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount));

			var qExpected =
					from od in db.OrderDetail
					select
						Sql.AsSql(od.UnitPrice * od.Quantity * (decimal)(1 - od.Discount));

			var sqlActual   = qActual.ToSqlQuery().Sql;
			var sqlExpected = qExpected.ToSqlQuery().Sql;
			using (Assert.EnterMultipleScope())
			{
				Assert.That(sqlActual, Is.Not.Contains("Convert").Or.Contains("Cast"));
				Assert.That(sqlExpected, Is.Not.Contains("Convert").Or.Contains("Cast"));
			}

			var actual   = qActual.  ToArray();
			var expected = qExpected.ToArray();

			Assert.That(expected, Has.Length.EqualTo(actual.Length));

			for (var i = 0; i < actual.Length; i++)
			{
				Assert.That(Math.Abs(actual[i] - expected[i]), Is.LessThan(0.01m));
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
		public void TestNoConvert_Byte([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Byte equals y.Byte
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Byte([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Byte == x.Byte)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_SByte([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.SByte equals y.SByte
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_SByte([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.SByte == x.SByte)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_Int16([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int16 equals y.Int16
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int16([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int16 == x.Int16)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_UInt16([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt16 equals y.UInt16
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt16([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt16 == x.UInt16)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_Int32([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int32 equals y.Int32
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int32([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int32 == x.Int32)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_UInt32([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt32 equals y.UInt32
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt32([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt32 == x.UInt32)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_Int64([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.Int64 equals y.Int64
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int64([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int64 == x.Int64)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_UInt64([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on x.UInt64 equals y.UInt64
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt64([IncludeDataSources(false, TestProvName.AllSqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt64 == x.UInt64)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_ByteN([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.ByteN } equals new { y.ByteN }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_ByteN([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.ByteN == x.ByteN)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_SByteN([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.SByteN } equals new { y.SByteN }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_SByteN([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.SByteN == x.SByteN)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_Int16N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.Int16N } equals new { y.Int16N }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int16N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int16N == x.Int16N)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain(" Convert("));
				}
			}
		}

		[Test]
		public void TestNoConvert_UInt16N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.UInt16N } equals new { y.UInt16N }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt16N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt16N == x.UInt16N)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvert_Int32N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.Int32N } equals new { y.Int32N }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int32N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int32N == x.Int32N)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvert_UInt32N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.UInt32N } equals new { y.UInt32N }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt32N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt32N == x.UInt32N)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvert_Int64N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.Int64N } equals new { y.Int64N }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_Int64N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.Int64N == x.Int64N)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvert_UInt64N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							join y in db.GetTable<IntegerConverts>() on new { x.UInt64N } equals new { y.UInt64N }
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}

		[Test]
		public void TestNoConvertWithExtension_UInt64N([IncludeDataSources(false, TestProvName.AllSqlServer)] string context)
		{
			using (var db = GetDataConnection(context))
			using (db.CreateLocalTable(IntegerConverts.Seed))
			{
				var query = from x in db.GetTable<IntegerConverts>()
							from y in db.GetTable<IntegerConverts>().InnerJoin(y => y.UInt64N == x.UInt64N)
							select x;

				var res = query.Single();
				using (Assert.EnterMultipleScope())
				{
					Assert.That(res.Id, Is.EqualTo(1));
					Assert.That(db.LastQuery!, Does.Not.Contain("CAST"));
				}
			}
		}
		#endregion

		#region TryConvert
		// NOTE:
		// class-typed overloads not tested for Oracle as it doesn't support DEFAULT CAST for strings
		// and we need custom reference type that wraps something like int for test
		[Test]
		public void TryConvertConvertedStruct([IncludeDataSources(true,
			TestProvName.AllClickHouse,
			TestProvName.AllOracle12Plus,
			TestProvName.AllSqlServer2012Plus
			)] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => Sql.TryConvert("123", (int?)0)), Is.EqualTo(123));
		}

		[Test]
		public void TryConvertNotConvertedStruct([IncludeDataSources(true,
			TestProvName.AllClickHouse,
			TestProvName.AllOracle12Plus,
			TestProvName.AllSqlServer2012Plus
			)] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => Sql.TryConvert("burp", (int?)0)), Is.Null);
		}

		[Test]
		public void TryConvertConvertedClass([IncludeDataSources(true, TestProvName.AllSqlServer2012Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => Sql.TryConvert(345, "")), Is.EqualTo("345"));
		}

		[Test]
		public void TryConvertOrDefaultConvertedStruct([IncludeDataSources(true, TestProvName.AllOracle12Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => Sql.TryConvertOrDefault("123", (int?)100500)), Is.EqualTo(123));
		}

		[Test]
		public void TryConvertOrDefaultNotConvertedStruct([IncludeDataSources(true, TestProvName.AllOracle12Plus, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			Assert.That(db.Select(() => Sql.TryConvertOrDefault("burp", (int?)-10)), Is.EqualTo(-10));
		}

		#endregion

		sealed class ToStringConvertibleTypes
		{
			public bool    Prop_bool    { get; set; }
			public byte    Prop_byte    { get; set; }
			public char    Prop_char    { get; set; }
			public decimal Prop_decimal { get; set; }
			public double  Prop_double  { get; set; }
			public short   Prop_short   { get; set; }
			public int     Prop_int     { get; set; }
			public long    Prop_long    { get; set; }
			public sbyte   Prop_sbyte   { get; set; }
			public float   Prop_float   { get; set; }
			public ushort  Prop_ushort  { get; set; }
			public uint    Prop_uint    { get; set; }
			public ulong   Prop_ulong   { get; set; }
			public Guid    Prop_Guid    { get; set; }

			public bool?    NullableProp_bool    { get; set; }
			public byte?    NullableProp_byte    { get; set; }
			public char?    NullableProp_char    { get; set; }
			public decimal? NullableProp_decimal { get; set; }
			public double?  NullableProp_double  { get; set; }
			public short?   NullableProp_short   { get; set; }
			public int?     NullableProp_int     { get; set; }
			public long?    NullableProp_long    { get; set; }
			public sbyte?   NullableProp_sbyte   { get; set; }
			public float?   NullableProp_float   { get; set; }
			public ushort?  NullableProp_ushort  { get; set; }
			public uint?    NullableProp_uint    { get; set; }
			public ulong?   NullableProp_ulong   { get; set; }
			public Guid?    NullableProp_Guid    { get; set; }

			public DateTime  Prop_DateTime         { get; set; }
			public DateTime? NullableProp_DateTime { get; set; }

			public static ToStringConvertibleTypes[] Seed()
			{
				return new ToStringConvertibleTypes[]
				{
					new ToStringConvertibleTypes
					{
						Prop_bool             = true,
						Prop_byte             = 1,
						Prop_char             = 'c',
						Prop_decimal          = 1.2m,
						Prop_double           = 1.2,
						Prop_short            = short.MaxValue,
						Prop_int              = int.MaxValue,
						Prop_long             = long.MaxValue,
						Prop_sbyte            = sbyte.MaxValue,
						Prop_float            = 1.2f,
						Prop_ushort           = ushort.MaxValue,
						Prop_uint             = uint.MaxValue,
						Prop_ulong            = uint.MaxValue,
						Prop_Guid             = Guid.Empty,
						NullableProp_bool     = true,
						NullableProp_byte     = 1,
						NullableProp_char     = 'c',
						NullableProp_decimal  = 1.2m,
						NullableProp_double   = 1.2,
						NullableProp_short    = short.MaxValue,
						NullableProp_int      = int.MaxValue,
						NullableProp_long     = long.MaxValue,
						NullableProp_sbyte    = sbyte.MaxValue,
						NullableProp_float    = 1.2f,
						NullableProp_ushort   = ushort.MaxValue,
						NullableProp_uint     = uint.MaxValue,
						NullableProp_ulong    = uint.MaxValue,
						NullableProp_Guid     = Guid.Empty,
						Prop_DateTime         = new DateTime(2022, 3, 25, 13, 40, 33),
						NullableProp_DateTime = new DateTime(2022, 3, 25, 13, 40, 33),
					},
				};
			}
		}

		[Test]
		public void TestToStringConversion([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using var db = GetDataContext(context);
			using var table = db.CreateLocalTable<ToStringConvertibleTypes>(ToStringConvertibleTypes.Seed());
			var sqlConverted = table.Select(x => new
			{
				Prop_bool             = Sql.AsSql(x.Prop_bool            .ToString(CultureInfo.InvariantCulture)),
				Prop_byte             = Sql.AsSql(x.Prop_byte            .ToString()),
				Prop_char             = Sql.AsSql(x.Prop_char            .ToString()),
				Prop_decimal          = Sql.AsSql(x.Prop_decimal         .ToString()),
				Prop_double           = Sql.AsSql(x.Prop_double          .ToString()),
				Prop_short            = Sql.AsSql(x.Prop_short           .ToString()),
				Prop_int              = Sql.AsSql(x.Prop_int             .ToString()),
				Prop_long             = Sql.AsSql(x.Prop_long            .ToString()),
				Prop_sbyte            = Sql.AsSql(x.Prop_sbyte           .ToString()),
				Prop_float            = Sql.AsSql(x.Prop_float           .ToString()),
				Prop_ushort           = Sql.AsSql(x.Prop_ushort          .ToString()),
				Prop_uint             = Sql.AsSql(x.Prop_uint            .ToString()),
				Prop_ulong            = Sql.AsSql(x.Prop_ulong           .ToString()),
				Prop_Guid             = Sql.AsSql(x.Prop_Guid            .ToString()),
				Prop_DateTime         = Sql.AsSql(x.Prop_DateTime        .ToString()),
				NullableProp_bool     = Sql.AsSql(x.NullableProp_bool    .ToString()),
				NullableProp_byte     = Sql.AsSql(x.NullableProp_byte    .ToString()),
				NullableProp_char     = Sql.AsSql(x.NullableProp_char    .ToString()),
				NullableProp_decimal  = Sql.AsSql(x.NullableProp_decimal .ToString()),
				NullableProp_double   = Sql.AsSql(x.NullableProp_double  .ToString()),
				NullableProp_short    = Sql.AsSql(x.NullableProp_short   .ToString()),
				NullableProp_int      = Sql.AsSql(x.NullableProp_int     .ToString()),
				NullableProp_long     = Sql.AsSql(x.NullableProp_long    .ToString()),
				NullableProp_sbyte    = Sql.AsSql(x.NullableProp_sbyte   .ToString()),
				NullableProp_float    = Sql.AsSql(x.NullableProp_float   .ToString()),
				NullableProp_ushort   = Sql.AsSql(x.NullableProp_ushort  .ToString()),
				NullableProp_uint     = Sql.AsSql(x.NullableProp_uint    .ToString()),
				NullableProp_ulong    = Sql.AsSql(x.NullableProp_ulong   .ToString()),
				NullableProp_Guid     = Sql.AsSql(x.NullableProp_Guid    .ToString()),
				NullableProp_DateTime = Sql.AsSql(x.NullableProp_DateTime.ToString()),
			})
					.First();

			var noSqlConverted = table.Select(x => new
			{
				Prop_bool             = x.Prop_bool ? "1" : "0",
				Prop_byte             = x.Prop_byte            .ToString(CultureInfo.InvariantCulture),
				Prop_char             = x.Prop_char            .ToString(CultureInfo.InvariantCulture),
				Prop_decimal          = x.Prop_decimal         .ToString(CultureInfo.InvariantCulture),
				Prop_double           = x.Prop_double          .ToString(CultureInfo.InvariantCulture),
				Prop_short            = x.Prop_short           .ToString(CultureInfo.InvariantCulture),
				Prop_int              = x.Prop_int             .ToString(CultureInfo.InvariantCulture),
				Prop_long             = x.Prop_long            .ToString(CultureInfo.InvariantCulture),
				Prop_sbyte            = x.Prop_sbyte           .ToString(CultureInfo.InvariantCulture),
				Prop_float            = x.Prop_float           .ToString(CultureInfo.InvariantCulture),
				Prop_ushort           = x.Prop_ushort          .ToString(CultureInfo.InvariantCulture),
				Prop_uint             = x.Prop_uint            .ToString(CultureInfo.InvariantCulture),
				Prop_ulong            = x.Prop_ulong           .ToString(CultureInfo.InvariantCulture),
				Prop_Guid             = x.Prop_Guid            .ToString(),
				Prop_DateTime         = x.Prop_DateTime        .ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
				NullableProp_bool     = x.NullableProp_bool == null ? "" : x.NullableProp_bool.Value ? "1" : "0",
				NullableProp_byte     = x.NullableProp_byte    !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_char     = x.NullableProp_char    !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_decimal  = x.NullableProp_decimal !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_double   = x.NullableProp_double  !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_short    = x.NullableProp_short   !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_int      = x.NullableProp_int     !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_long     = x.NullableProp_long    !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_sbyte    = x.NullableProp_sbyte   !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_float    = x.NullableProp_float   !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_ushort   = x.NullableProp_ushort  !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_uint     = x.NullableProp_uint    !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_ulong    = x.NullableProp_ulong   !.Value.ToString(CultureInfo.InvariantCulture),
				NullableProp_Guid     = x.NullableProp_Guid    !.Value.ToString(),
				NullableProp_DateTime = x.NullableProp_DateTime!.Value.ToString("yyyy-MM-dd HH:mm:ss.fffffff"),
			})
					.First();

			sqlConverted.ShouldBeEquivalentTo(noSqlConverted);
		}

		#region Issue #4043

		[Table("Issue4043")]
		class Issue4043TableRaw
		{
			[Column] public int     Id    { get; set; }
			[Column] public string? Value { get; set; }

			public static readonly Issue4043TableRaw[] Data = new[]
			{
				new Issue4043TableRaw() { Id = 1, Value = /*lang=json,strict*/ "{\"Field1\": 1, \"Field2\": -1 }" }
			};
		}

		[Table("Issue4043")]
		class Issue4043Table
		{
			[Column] public int          Id    { get; set; }
			[Column] public ValueObject? Value { get; set; }
		}

		[Table("Issue4043")]
		class Issue4043ScalarTable
		{
			[Column] public ValueObject? Value { get; set; }
		}

		[Table("Issue4043")]
		class Issue4043TableWithCtor
		{
			public Issue4043TableWithCtor(int Id, ValueObject? Value)
			{
				this.Id    = Id;
				this.Value = Value;
			}

			[Column] public int          Id    { get; set; }
			[Column] public ValueObject? Value { get; set; }
		}

		[Table("Issue4043")]
		class Issue4043ScalarTableWithCtor
		{
			public Issue4043ScalarTableWithCtor(ValueObject? Value)
			{
				this.Value = Value;
			}

			[Column] public ValueObject? Value { get; set; }
		}

		class ValueObject
		{
			public int Field1 { get; set; }
			public int Field2 { get; set; }
		}

		[Test]
		public void TextExecuteTypeConverter([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<string, ValueObject?>(json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null));

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043Table>("select Id, Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(1));
				Assert.That(result.Value, Is.Not.Null);
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteTypeConverterWithCtor([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<string, ValueObject?>(json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null));

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043TableWithCtor>("select Id, Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(1));
				Assert.That(result.Value, Is.Not.Null);
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteScalarEntityTypeConverter([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<string, ValueObject?>(json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null));

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043ScalarTable>("select Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Value, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteScalarEntityTypeConverterWithCtor([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			ms.SetConvertExpression<string, ValueObject?>(json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null));

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043ScalarTableWithCtor>("select Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Value, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteScalar([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			ms.SetScalarType(typeof(ValueObject));
			ms.SetConvertExpression<string, ValueObject?>(json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null));

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<ValueObject>("select Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Field1, Is.EqualTo(1));
				Assert.That(result.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteColumnConverter([IncludeDataSources( ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Issue4043Table>()
				.Property(e => e.Value)
				.HasConversion(o => JsonSerializer.Serialize(o, (JsonSerializerOptions?)null), json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null))
				.Build();

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043Table>("select Id, Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(1));
				Assert.That(result.Value, Is.Not.Null);
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteColumnConverterWithCtor([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Issue4043TableWithCtor>()
				.Property(e => e.Value)
				.HasConversion(o => JsonSerializer.Serialize(o, (JsonSerializerOptions?)null), json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null))
				.Build();

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043TableWithCtor>("select Id, Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Id, Is.EqualTo(1));
				Assert.That(result.Value, Is.Not.Null);
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteScalarEntityColumnConverter([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Issue4043ScalarTable>()
				.Property(e => e.Value)
				.HasConversion(o => JsonSerializer.Serialize(o, (JsonSerializerOptions?)null), json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null))
				.Build();

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043ScalarTable>("select Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Value, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}

		[Test]
		public void TextExecuteScalarEntityColumnConverterWithCtor([IncludeDataSources(ProviderName.SQLiteClassic)] string context)
		{
			var ms = new MappingSchema();
			new FluentMappingBuilder(ms)
				.Entity<Issue4043ScalarTableWithCtor>()
				.Property(e => e.Value)
				.HasConversion(o => JsonSerializer.Serialize(o, (JsonSerializerOptions?)null), json => JsonSerializer.Deserialize<ValueObject>(json, (JsonSerializerOptions?)null))
				.Build();

			using var db = GetDataContext(context, ms);
			using var _  = db.CreateLocalTable(Issue4043TableRaw.Data);

			var result = db.Execute<Issue4043ScalarTableWithCtor>("select Value from Issue4043");

			Assert.That(result, Is.Not.Null);
			Assert.That(result.Value, Is.Not.Null);
			using (Assert.EnterMultipleScope())
			{
				Assert.That(result.Value!.Field1, Is.EqualTo(1));
				Assert.That(result.Value!.Field2, Is.EqualTo(-1));
			}
		}
		#endregion

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5390")]
		public void TestDateDontUseTypeFacets([DataSources] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue5390Table>();

			var query = tb.GroupBy(o => new { o.CreatedOnUtc.Date })
				.Select(g => new
				{
					Date = g.Key.Date,
					Count = g.Count()
				});

			AssertQuery(query);
		}

		public class Issue5390Table
		{
			[Column(Precision = 6), PrimaryKey]
			public DateTime CreatedOnUtc { get; set; }
		}

		sealed class ScaledBoundRow
		{
			[PrimaryKey                      ] public int     Id    { get; set; }
			[Column(Precision = 6, Scale = 2)] public decimal Value { get; set; }
		}

		/// <summary>
		/// A bound compared against a widened column keeps enough scale to hold itself.
		/// </summary>
		/// <remarks>
		/// The descriptor walk lends a column's declared type to a value written down beside it, and stops where a
		/// cast changed that type - but it compared only the data type, which a cast between two decimals does not
		/// change. So a bound beside <c>CAST(x AS Decimal(22,9))</c> over a <c>Decimal(6,2)</c> column was written
		/// down as <c>Decimal(6,2)</c>: a declared scale too small for its own digits.
		/// <para>
		/// Asked in both directions because the two ways of losing the third decimal place fail on opposite rows.
		/// Rounding the bound up to <c>0.01</c> drops the row above it; truncating it down to <c>0.00</c> drops the
		/// row below. One assertion cannot see both.
		/// </para>
		/// </remarks>
		[Test]
		public void ABoundBesideAWideningCastKeepsItsOwnScale([IncludeDataSources(false, TestProvName.AllYdb)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(new[]
			{
				new ScaledBoundRow { Id = 1, Value = 0.00m },
				new ScaledBoundRow { Id = 2, Value = 0.01m },
			});

			t.Count(r => Convert.ToDecimal(r.Value) > 0.005m).ShouldBe(1);
			t.Count(r => Convert.ToDecimal(r.Value) < 0.005m).ShouldBe(1);
		}
	}
}
