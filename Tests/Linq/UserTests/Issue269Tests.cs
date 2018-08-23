using System.Linq;
using LinqToDB;
using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue269Tests : TestBase
	{
		class TestDataContextSourceAttribute : DataContextSourceAttribute
		{
			public TestDataContextSourceAttribute() : base(
				ProviderName.Access, ProviderName.SQLiteClassic, ProviderName.Oracle,
				ProviderName.MySql, ProviderName.Sybase, ProviderName.SybaseManaged,
				ProviderName.OracleNative, ProviderName.OracleManaged,
				ProviderName.DB2,
				ProviderName.SqlCe,
				TestProvName.MySql57,
				ProviderName.SQLiteMS, ProviderName.SqlServer2000, TestProvName.MariaDB)
			{
			}
		}

		[Test, TestDataContextSource]
		public void TestTake(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Take(1)
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Take(1)
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test, TestDataContextSource]
		public void TestDistinct(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Distinct()
						.Any(_ => _.Contains("with")));

				// DISTINCT should be optimized out
//				Assert.That(q.EnumQueries().All(x => !x.Select.IsDistinct), Is.True);

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Distinct()
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test, TestDataContextSource]
		public void TestSkipDistinct(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Skip(0)
						.Distinct()
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Skip(0)
						.Distinct()
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test, TestDataContextSource]
		public void TestDistinctSkip(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Distinct()
						.Skip(0)
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Distinct()
						.Skip(0)
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

		[Test, TestDataContextSource]
		public void TestSkip(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = db.Patient
					.Where(pat => db.Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Skip(0)
						.Any(_ => _.Contains("with")));

				var e = Patient
					.Where(pat => Person
						.Where(per => per.ID == pat.PersonID)
						.OrderByDescending(per => per.FirstName)
						.Select(c => c.Patient.Diagnosis)
						.Skip(0)
						.Any(_ => _.Contains("with")));

				AreEqual(e, q);
			}
		}

	}
}
