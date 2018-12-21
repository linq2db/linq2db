using System.Linq;
using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;

	[TestFixture]
	public class Issue882Tests : TestBase
	{
		[Test]
		public void Year([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Year % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Year % 7));
		}

		[Test]
		public void Month([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Month % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Month % 7));
		}

		[Test]
		public void DayOfYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfYear % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfYear % 7));
		}

		[Test]
		public void Day([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Day % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Day % 7));
		}

		[Test]
		public void Hour([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Hour % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Hour % 7));
		}

		[Test]
		public void Minute([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Minute % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Minute % 7));
		}

		[Test]
		public void Second([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Second % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Second % 7));
		}

		[Test]
		public void Millisecond([DataSources(
			ProviderName.Informix, ProviderName.MySql, ProviderName.Access,
			ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Millisecond % 7,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Millisecond % 7));
		}
	}
}
