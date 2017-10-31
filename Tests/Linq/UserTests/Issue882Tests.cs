using System.Linq;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;

	[TestFixture]
	public class Issue882Tests : TestBase
	{
		[Test, DataContextSource]
		public void Year(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Year % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Year % 7));
		}

		[Test, DataContextSource]
		public void Month(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Month % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Month % 7));
		}

		[Test, DataContextSource]
		public void DayOfYear(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfYear % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfYear % 7));
		}

		[Test, DataContextSource]
		public void Day(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Day % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Day % 7));
		}

		[Test, DataContextSource]
		public void Hour(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Hour % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Hour % 7));
		}

		[Test, DataContextSource]
		public void Minute(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Minute % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Minute % 7));
		}

		[Test, DataContextSource(ProviderName.Firebird)]
		public void Second(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Second % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Second % 7));
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.Firebird, ProviderName.MySql, ProviderName.Access, ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
		public void Millisecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Millisecond % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Millisecond % 7));
		}

	}
}