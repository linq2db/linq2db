#if NET8_0_OR_GREATER
using System;
using System.Linq;
using System.Runtime.InteropServices;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.Tools;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class DateOnlyFunctionTests : TestBase
	{
		[Table("Transactions")]
		private sealed class Transaction
		{
			[PrimaryKey] public int      TransactionId   { get; set; }
			[Column]     public DateOnly TransactionDate { get; set; }

			public static Transaction[] AllData { get; } = new[]
			{
				new Transaction() { TransactionId = 1 , TransactionDate = TestData.DateOnly                        },
				new Transaction() { TransactionId = 2 , TransactionDate = TestData.DateOnly         .AddYears(1)   },
				new Transaction() { TransactionId = 3 , TransactionDate = TestData.DateOnly         .AddYears(-1)  },
				new Transaction() { TransactionId = 4 , TransactionDate = TestData.DateOnly         .AddMonths(1)  },
				new Transaction() { TransactionId = 5 , TransactionDate = TestData.DateOnly         .AddMonths(-1) },
				new Transaction() { TransactionId = 6 , TransactionDate = TestData.DateOnly         .AddDays(1)    },
				new Transaction() { TransactionId = 7 , TransactionDate = TestData.DateOnly         .AddDays(-1)   },
				new Transaction() { TransactionId = 8 , TransactionDate = TestData.DateOnlyAmbiguous               },
				new Transaction() { TransactionId = 9 , TransactionDate = TestData.DateOnlyAmbiguous.AddYears(1)   },
				new Transaction() { TransactionId = 10, TransactionDate = TestData.DateOnlyAmbiguous.AddYears(-1)  },
				new Transaction() { TransactionId = 11, TransactionDate = TestData.DateOnlyAmbiguous.AddMonths(1)  },
				new Transaction() { TransactionId = 12, TransactionDate = TestData.DateOnlyAmbiguous.AddMonths(-1) },
				new Transaction() { TransactionId = 13, TransactionDate = TestData.DateOnlyAmbiguous.AddDays(1)    },
				new Transaction() { TransactionId = 14, TransactionDate = TestData.DateOnlyAmbiguous.AddDays(-1)   },
			};
		}

		private const string DateOnlySkipProviders = $"{TestProvName.AllAccess},{ProviderName.SqlCe},{TestProvName.AllSqlServer2005},{ProviderName.PostgreSQL92},{ProviderName.PostgreSQL93}";

		[Test]
		public void Parse1([DataSources(DateOnlySkipProviders)] string context)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(Transaction.AllData);

			var query = from t in db.GetTable<Transaction>()
				let d = DateOnly.Parse("2010-01-" + Sql.ZeroPad(t.TransactionId, 2))
				where d.Day > 0
				select d;

			AssertQuery(query);
		}

		[Test]
		public void Parse2([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from d in from t in Transaction.AllData        select DateOnly.Parse(Sql.ConvertTo<string>.From(t.TransactionDate)) where d.Day > 0 select d,
					from d in from t in db.GetTable<Transaction>() select DateOnly.Parse(Sql.ConvertTo<string>.From(t.TransactionDate)) where d.Day > 0 select d);
		}

		#region DatePart

		[Test]
		public void DatePartYear([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DatePart(Sql.DateParts.Year, t.TransactionDate),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(Sql.DateParts.Year, t.TransactionDate)));
		}

		[Test]
		public void DatePartQuarter([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DatePart(Sql.DateParts.Quarter, t.TransactionDate),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(Sql.DateParts.Quarter, t.TransactionDate)));
		}

		[Test]
		public void DatePartMonth([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DatePart(Sql.DateParts.Month, t.TransactionDate),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(Sql.DateParts.Month, t.TransactionDate)));
		}

		[Test]
		public void DatePartDayOfYear([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DatePart(Sql.DateParts.DayOfYear, t.TransactionDate),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(Sql.DateParts.DayOfYear, t.TransactionDate)));
		}

		[Test]
		public void DatePartDay([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DatePart(Sql.DateParts.Day, t.TransactionDate),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(Sql.DateParts.Day, t.TransactionDate)));
		}

		[Test]
		public void DatePartWeek([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				(from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(Sql.DateParts.Week, t.TransactionDate))).ToList();
		}

		[Test]
		public void DatePartWeekDay([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DatePart(Sql.DateParts.WeekDay, t.TransactionDate),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(Sql.DateParts.WeekDay, t.TransactionDate)));
		}

		[Test]
		public void DatepartDynamic(
			[DataSources(TestProvName.AllInformix, DateOnlySkipProviders)] string context,
			[Values(
				Sql.DateParts.Day,
				Sql.DateParts.Month,
				Sql.DateParts.Year
				)] Sql.DateParts datepart)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
			{
				var expected =
					from t in Transaction.AllData        select           Sql.DatePart(datepart, t.TransactionDate);
				var result =
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DatePart(datepart, t.TransactionDate));

				AreEqual(expected, result);
			}
		}

		[Test]
		public void Year([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           t.TransactionDate.Year,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.Year));
		}

		[Test]
		public void Month([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           t.TransactionDate.Month,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.Month));
		}

		[Test]
		public void DayOfYear([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           t.TransactionDate.DayOfYear,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.DayOfYear));
		}

		[Test]
		public void Day([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           t.TransactionDate.Day,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.Day));
		}

		#endregion

		#region DateAdd

		[Test]
		public void DateAddYear([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DateAdd(Sql.DateParts.Year, 12, t.TransactionDate) !.Value,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 12, t.TransactionDate))!.Value);
		}

		[Test]
		public void DateAddQuarter([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate) !.Value,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate))!.Value);
		}

		[Test]
		public void DateAddMonth([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate) !.Value,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate))!.Value);
		}

		[Test]
		public void DateAddDayOfYear([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.TransactionDate) !.Value,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.TransactionDate))!.Value);
		}

		[Test]
		public void DateAddDay([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate) !.Value,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate))!.Value);
		}

		[Test]
		public void DateAddWeek([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate) !.Value,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate))!.Value);
		}

		[Test]
		public void DateAddWeekDay([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.TransactionDate) !.Value,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.TransactionDate))!.Value);
		}

		[Test]
		public void AddYears([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           t.TransactionDate.AddYears(12),
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddYears(12)));
		}

		[Test]
		public void AddMonths([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           t.TransactionDate.AddMonths(-2),
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddMonths(-2)));
		}

		[Test]
		public void AddDays([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.AllData))
				AreEqual(
					from t in Transaction.AllData        select           t.TransactionDate.AddDays(5),
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddDays(5)));
		}

		#endregion

		#region DateDiff

		[Test]
		public void SubDateDay(
			[DataSources(TestProvName.AllInformix, DateOnlySkipProviders)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays));
		}

		[Test]
		public void DateDiffDay(
			[DataSources(TestProvName.AllInformix, DateOnlySkipProviders)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		#endregion

		#region MakeDateTime

		[Test]
		public void MakeDateOnly([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateOnly(2010, p.ID, 1) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateOnly(2010, p.ID, 1) where t.Value.Year == 2010 select t);
		}

		[Test]
		public void MakeDateOnlyParameters([DataSources(DateOnlySkipProviders)] string context)
		{
			var year = 2010;
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateOnly(year, p.ID, 1) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateOnly(year, p.ID, 1) where t.Value.Year == 2010 select t);
		}

		[Test]
		public void MakeDateOnlyParametersMonth([DataSources(DateOnlySkipProviders)] string context, [Values(1, 10)] int month)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateOnly(2010 + p.ID, month, 1) select t,
					from t in from p in db.Types select Sql.MakeDateOnly(2010 + p.ID, month, 1) select t);
		}

		[Test]
		public void NewDateOnly1([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateOnly(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t,
					from t in from p in db.Types select new DateOnly(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t);
		}

		[Test]
		public void NewDateOnly2([DataSources(DateOnlySkipProviders)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types select new DateOnly(p.DateTimeValue.Year, 10, 1),
					from p in db.Types select new DateOnly(p.DateTimeValue.Year, 10, 1));
		}

#endregion
	}
}
#endif
