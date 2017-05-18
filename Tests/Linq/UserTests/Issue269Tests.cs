using System.Linq;
using LinqToDB;
using NUnit.Framework;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue269Tests : TestBase
	{
		class TestDataContextSourceAttribute : DataContextSourceAttribute
		{
			public TestDataContextSourceAttribute() : base(
				ProviderName.Access, ProviderName.SQLite, ProviderName.Oracle,
				ProviderName.MySql, ProviderName.Sybase, 
				ProviderName.OracleNative, ProviderName.OracleManaged)
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