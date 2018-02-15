using System;
using System.Linq;
using LinqToDB;
using LinqToDB.SqlQuery;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	public class Issue998Tests : TestBase
	{
		[Test, IncludeDataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void AddDaysFromColumnPositive(string context)
		{
			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<LinqDataTypes>();

				db.Insert(new LinqDataTypes { ID = 5000, SmallIntValue = 2, DateTimeValue = new DateTime(2018, 01, 03) });

				var result = tbl
					.Count(t => t.ID == 5000 && t.DateTimeValue.AddDays(t.SmallIntValue) > new DateTime(2018, 01, 02));

				tbl.Delete(t => t.ID == 5000);

				Assert.AreEqual(1, result);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void AddDaysFromColumnNegative(string context)
		{
			using (var db = GetDataContext(context))
			{
				var tbl = db.GetTable<LinqDataTypes>();

				db.Insert(new LinqDataTypes { ID = 5000, SmallIntValue = -2, DateTimeValue = new DateTime(2018, 01, 03) });

				var result = tbl
					.Count(t => t.ID == 5000 && t.DateTimeValue.AddDays(t.SmallIntValue) < new DateTime(2018, 01, 02));

				Assert.AreEqual(1, result);

				tbl.Delete(t => t.ID == 5000);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void AddDaysFromColumn(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(Types.Select(t => t.DateTimeValue.AddDays(t.SmallIntValue)),
					Types.Select(t => t.DateTimeValue.AddDays(t.SmallIntValue)));
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void AddWeekFromColumn(string context)
		{
			using (var db = GetDataContext(context))
			{ 
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue, t.DateTimeValue).Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue, t.DateTimeValue)).Value.Date);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void AddQuarterFromColumn(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue, t.DateTimeValue).Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue, t.DateTimeValue)).Value.Date);
			}
		}

		[Test, IncludeDataContextSource(ProviderName.SQLiteClassic, ProviderName.SQLiteMS)]
		public void AddYearFromColumn(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue, t.DateTimeValue).Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue, t.DateTimeValue)).Value.Date);
			}
		}
	}
}
